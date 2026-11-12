/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `WraithHerald.cs` is a concrete enemy subclass that adds a spectral combat style 
    to the polymorphic enemy roster. It uses inherited properties and overridden 
    methods.
 */
using System;
using ARDO.Core.Abstracts;
using ARDO.Game.Players;

namespace ARDO.Game.Enemies
{
    // The Wraith Herald feeds on its prey: mana from a mage, life from anyone else.
    public class WraithHerald : Boss
    {
        public WraithHerald() : base("Wraith Herald", 110, 18)
        {
        }

        public override string ArtKey => "WraithHerald";

        public override string AttackLine => "lashes out with a cold phantom swipe.";

        protected override int TrickChancePercent => 30;

        protected override string UseTrick(Hero hero)
        {
            if (hero is Mage mage && mage.CurrentMana > 0)
            {
                int drained = Math.Min(mage.CurrentMana, 20);
                mage.CurrentMana -= drained;
                CurrentHealth = Math.Min(MaxHealth, CurrentHealth + drained);
                return $"{Name} siphons {drained} mana from you and its form knits tighter.";
            }

            int before = hero.CurrentHealth;
            string line = HitHero(hero, RollAttackDamage(), "presses a freezing hand to your chest.");
            int stolen = (before - hero.CurrentHealth) / 2;
            if (stolen > 0)
            {
                CurrentHealth = Math.Min(MaxHealth, CurrentHealth + stolen);
                line += $" It drinks {stolen} HP of your warmth.";
            }

            return line;
        }
    }
}
