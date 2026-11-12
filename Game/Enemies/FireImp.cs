/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `FireImp.cs` is a concrete enemy subclass with a unique combat style for the game's 
    polymorphic battle flow. It relies on inherited properties and overridden methods.
 */
using ARDO.Core.Abstracts;

namespace ARDO.Game.Enemies
{
    // Fire imps can set the hero burning for a few rounds.
    public class FireImp : Minion
    {
        public FireImp() : base("Fire Imp", 48, 10, 20, 12)
        {
        }

        public override string ArtKey => "FireImp";

        public override string AttackLine => "hurls a burst of ember flame.";

        protected override int TrickChancePercent => 35;

        protected override string UseTrick(Hero hero)
        {
            string line = HitHero(hero, RollAttackDamage() / 2, "spits a gout of clinging fire.");
            hero.Burn.Apply(3, 3 + CombatFloor);
            return line + $" You are burning! ({hero.Burn.DamagePerTurn} a round for {hero.Burn.TurnsLeft} rounds)";
        }
    }
}
