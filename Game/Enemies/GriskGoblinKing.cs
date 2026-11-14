/**
 * Name: John Deardorff
 * Date: 11/13/26
 * Project ARDO
 *
 * Class description:
 * `GriskGoblinKing.cs` is the guardian of the first floor. He shouts lackeys
    into the fight a few at a time and taxes the hero's pouch when his court is
    full. Kill the king and his lackeys scatter. On a later turn of the wheel,
    at half health he puts on his hoard: a gilded ward that soaks blows and grows
    with every coin he takes from you.
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

        // Second phase: gold taxed this fight, and the ward it buys.
        private const int WardPerGold = 3;
        private int _taxedThisFight;
        public int Ward { get; private set; }

        public GriskGoblinKing() : base("Grisk, the Goblin King", 210, 14)
        {
            XpReward = 150;
            GoldReward = 120;
        }

        public override string ArtKey => "grisk";
        public override string AttackLine => "slashes with a curved blade wreathed in green fire.";
        public override string HeavyAttackLine => "brings the burning blade down with both hands, grinning all the while.";
        public override string WindUpWarning => $"{Name} raises his green-burning blade high over his hood!";
        protected override int HeavyChancePercent => 20;
        protected override int TrickChancePercent => InSecondPhase ? 55 : 35;
        protected override double HeavyMultiplier => 1.8;

        public override string? StatusNote
        {
            get
            {
                int court = _court.Count(goblin => goblin.IsAlive);
                string? ward = Ward > 0 ? $"gilded ward {Ward}" : null;
                string? courtNote = court > 0 ? $"court of {court}" : null;
                return ward != null && courtNote != null ? $"{ward}, {courtNote}" : ward ?? courtNote;
            }
        }

        protected override bool HasSecondPhase => true;

        protected override void BeginSecondPhase()
        {
            Ward = MaxHealth / 4 + _taxedThisFight * WardPerGold;
            Announce($"{Name} tears open his treasure sack and pulls on a crown and a coat of stolen coin. " +
                     $"A gilded ward of {Ward} covers him, and every coin he takes from you will thicken it.");
        }

        protected override string UseTrick(Hero hero)
        {
            if (!InSecondPhase && _court.Count(goblin => goblin.IsAlive) < MaxCourtAtOnce && _court.Count < MaxCalledPerFight)
            {
                var lackey = new Goblin { Name = "Goblin Lackey", XpReward = 5, GoldReward = 0 };
                _court.Add(lackey);
                Summon(lackey);
                return $"{Name} lifts the burning skull and shrieks \"Destroy them!\" A Goblin Lackey scrambles out of the dark.";
            }

            int taxed = Math.Min(hero.Gold, 8 + CombatFloor * 2);
            if (taxed <= 0)
                return HitHero(hero, RollAttackDamage(), AttackLine);

            hero.Gold -= taxed;
            GoldReward += taxed;
            _taxedThisFight += taxed;
            if (InSecondPhase)
            {
                Ward += taxed * WardPerGold;
                return $"{Name} points his blade at your pouch. \"Consider this a royal tax.\" He presses {taxed} of your gold into his coat, and the gilded ward grows to {Ward}.";
            }

            return $"{Name} points his blade at your pouch. \"Consider this a royal tax.\" A lackey's quick hands take {taxed} gold.";
        }

        public override void TakeDamage(int amount)
        {
            if (Ward > 0 && amount > 0)
            {
                int soaked = Math.Min(Ward, amount);
                Ward -= soaked;
                amount -= soaked;
                if (Ward == 0)
                    Announce($"The gilded ward bursts, and coins scatter across the cave floor.");
                if (amount <= 0)
                    return;
            }

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
