/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `Goblin.cs` is a concrete enemy subclass that provides a simple polymorphic enemy type for 
    early encounters. It uses inherited combat members and constructor-driven stats.
 */
using System;
using ARDO.Core.Abstracts;

namespace ARDO.Game.Enemies
{
    // Goblins grab gold from the hero's pouch. Killing the thief gets it back.
    public class Goblin : Minion
    {
        public Goblin() : base("Goblin", 40, 6, 15, 10)
        {
        }

        public override string ArtKey => "goblin";

        public override string AttackLine => "lunges with a jagged blade.";

        protected override int TrickChancePercent => 30;

        protected override string UseTrick(Hero hero)
        {
            int stolen = Math.Min(hero.Gold, 5 + CombatFloor * 3);
            if (stolen <= 0)
            {
                return HitHero(hero, RollAttackDamage(), AttackLine);
            }

            hero.Gold -= stolen;
            GoldReward += stolen;
            return $"{Name} snatches {stolen} gold from your pouch and cackles. Kill it to get it back!";
        }
    }
}
