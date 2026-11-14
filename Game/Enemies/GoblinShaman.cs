/**
 * Name: John Deardorff
 * Date: 11/14/26
 * Project ARDO
 *
 * Class description:
 * `GoblinShaman.cs` is a bone-rattling healer of the king's court. Instead of
    attacking it often mends the most hurt goblin in the fight (itself included),
    so it is worth killing first.
 */
using System;
using System.Linq;
using ARDO.Core.Abstracts;

namespace ARDO.Game.Enemies
{
    public sealed class GoblinShaman : Minion
    {
        public GoblinShaman() : base("Goblin Shaman", 80, 10, 70, 30)
        {
        }

        public override string ArtKey => "goblinshaman";
        public override string AttackLine => "cracks you across the knuckles with a staff of bones.";
        protected override int TrickChancePercent => 50;

        protected override string UseTrick(Hero hero)
        {
            Enemy? patient = Allies
                .Where(ally => ally.IsAlive && ally.CurrentHealth < ally.MaxHealth)
                .OrderBy(ally => (double)ally.CurrentHealth / ally.MaxHealth)
                .FirstOrDefault();

            if (patient == null)
                return HitHero(hero, RollAttackDamage(), AttackLine);

            int mended = Math.Min(patient.MaxHealth - patient.CurrentHealth, 20 + patient.MaxHealth / 12);
            patient.CurrentHealth += mended;
            string who = ReferenceEquals(patient, this) ? "itself" : $"the {patient.Name}";
            return $"{Name} shakes its bone rattle over {who}. Green light knits {mended} HP back.";
        }
    }
}
