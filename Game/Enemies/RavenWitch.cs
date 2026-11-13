/**
 * Name: John Deardorff
 * Date: 11/13/26
 * Project ARDO
 *
 * Class description:
 * `RavenWitch.cs` is the guardian of the Hollow Cathedral: something like a
    vampire, in white lace under a pale parasol. She drinks back half of every
    wound she gives, her open parasol turns aside the next blow completely, and
    once she is badly hurt her ravens come down from the rafters. They scatter
    when she falls.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using ARDO.Core.Abstracts;

namespace ARDO.Game.Enemies
{
    public sealed class RavenWitch : Boss
    {
        private readonly List<Enemy> _ravens = new();
        private bool _ravensCalled;
        private bool _ravensScattered;

        public RavenWitch() : base("The Raven Witch", 245, 15)
        {
            XpReward = 300;
            GoldReward = 230;
        }

        public override string ArtKey => "ravenwitch";
        public override string AttackLine => "flicks the parasol's steel tip across your throat.";
        public override string HeavyAttackLine => "is suddenly close, and her teeth find your neck.";
        public override string WindUpWarning => $"{Name} lowers her parasol and smiles with too many teeth!";
        protected override int HeavyChancePercent => 25;
        protected override int TrickChancePercent => 30;
        protected override double HeavyMultiplier => 2.0;

        // While open, the parasol turns aside the next blow, whatever it is.
        public bool ParasolOpen { get; private set; }

        public override string? StatusNote => ParasolOpen ? "parasol open" : null;

        public override string TakeTurn(Hero hero)
        {
            int before = hero.CurrentHealth;
            string line = base.TakeTurn(hero);
            int drunk = (before - hero.CurrentHealth) / 2;
            if (drunk > 0 && IsAlive)
            {
                int healed = Math.Min(MaxHealth - CurrentHealth, drunk);
                CurrentHealth += healed;
                if (healed > 0)
                    line += $" She drinks {healed} HP from the wound.";
            }

            return line;
        }

        protected override string UseTrick(Hero hero)
        {
            if (ParasolOpen)
                return HitHero(hero, RollAttackDamage(), AttackLine);

            ParasolOpen = true;
            return $"{Name} snaps her pale parasol open. The next blow will glance off it.";
        }

        public override void TakeDamage(int amount)
        {
            if (ParasolOpen && amount > 0)
            {
                ParasolOpen = false;
                Announce($"The blow skids off {Name}'s parasol. It folds shut with a snap.");
                return;
            }

            base.TakeDamage(amount);

            if (IsAlive && !_ravensCalled && CurrentHealth < MaxHealth / 2)
            {
                _ravensCalled = true;
                for (int i = 0; i < 2; i++)
                {
                    var raven = new Raven();
                    _ravens.Add(raven);
                    Summon(raven);
                }

                Announce("She lifts one gloved hand. The rafters empty, and two ravens drop out of the dark.");
            }

            if (IsDead && !_ravensScattered)
            {
                _ravensScattered = true;
                if (_ravens.Any(raven => raven.IsAlive))
                {
                    foreach (Enemy raven in _ravens.Where(raven => raven.IsAlive))
                        raven.CurrentHealth = 0;

                    Announce("Her ravens burst up into the dark and do not come back.");
                }
            }
        }
    }
}
