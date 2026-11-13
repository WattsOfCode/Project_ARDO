/**
 * Name: John Deardorff
 * Date: 11/13/26
 * Project ARDO
 *
 * Class description:
 * `GriskGoblinKing.cs` is the guardian of the first floor. He shouts lackeys
    into the fight a few at a time and taxes the hero's pouch when his court is
    full. Kill the king and his lackeys scatter.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using ARDO.Core.Abstracts;

namespace ARDO.Game.Enemies
{
    public sealed class GriskGoblinKing : Boss
    {
        private const int MaxCourtAtOnce = 2;
        private const int MaxCalledPerFight = 3;

        private readonly List<Enemy> _court = new();
        private bool _scattered;

        public GriskGoblinKing() : base("Grisk, the Goblin King", 210, 14)
        {
            XpReward = 150;
            GoldReward = 120;
        }

        public override string ArtKey => "grisk";
        public override string AttackLine => "swings a notched royal cleaver.";
        public override string HeavyAttackLine => "brings the royal cleaver down with both hands.";
        public override string WindUpWarning => $"{Name} hoists his cleaver high over his pot-lid crown!";
        protected override int HeavyChancePercent => 20;
        protected override int TrickChancePercent => 35;
        protected override double HeavyMultiplier => 1.8;

        public override string? StatusNote => _court.Count(goblin => goblin.IsAlive) > 0
            ? $"court of {_court.Count(goblin => goblin.IsAlive)}"
            : null;

        protected override string UseTrick(Hero hero)
        {
            if (_court.Count(goblin => goblin.IsAlive) < MaxCourtAtOnce && _court.Count < MaxCalledPerFight)
            {
                var lackey = new Goblin { Name = "Goblin Lackey", XpReward = 5, GoldReward = 0 };
                _court.Add(lackey);
                Summon(lackey);
                return $"{Name} bangs on his crown and bellows \"GET 'IM!\" A Goblin Lackey scrambles out of the dark.";
            }

            int taxed = Math.Min(hero.Gold, 8 + CombatFloor * 2);
            if (taxed <= 0)
                return HitHero(hero, RollAttackDamage(), AttackLine);

            hero.Gold -= taxed;
            GoldReward += taxed;
            return $"{Name} points his cleaver at your pouch. \"Royal tax!\" A lackey's quick hands take {taxed} gold.";
        }

        public override void TakeDamage(int amount)
        {
            base.TakeDamage(amount);
            if (IsAlive || _scattered)
                return;

            _scattered = true;
            if (_court.Any(goblin => goblin.IsAlive))
            {
                foreach (Enemy goblin in _court.Where(goblin => goblin.IsAlive))
                    goblin.CurrentHealth = 0;

                Announce("With their king down, the lackeys shriek and scatter into the dark.");
            }
        }
    }
}
