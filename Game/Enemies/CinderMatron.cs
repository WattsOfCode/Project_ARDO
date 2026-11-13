/**
 * Name: John Deardorff
 * Date: 11/13/26
 * Project ARDO
 *
 * Class description:
 * `CinderMatron.cs` is the guardian of the Emberward Depths. She builds heat
    every turn and lets it go as a Firestorm on the third, which sets the hero
    burning. Fire only feeds her: a burn on her heals instead of hurting.
 */
using ARDO.Core.Abstracts;
using ARDO.Core.Enums;

namespace ARDO.Game.Enemies
{
    public sealed class CinderMatron : Boss
    {
        private const int HeatForFirestorm = 3;
        private int _heat;

        public CinderMatron() : base("The Cinder Matron", 240, 15)
        {
            XpReward = 220;
            GoldReward = 170;
        }

        public override string ArtKey => "cindermatron";
        public override string AttackLine => "rakes at you with fingers of live coal.";
        public override string HeavyAttackLine => "opens her arms, and the Firestorm pours out of her.";
        public override string WindUpWarning => $"{Name} draws the furnace into her arms. A Firestorm is coming!";
        protected override double HeavyMultiplier => 2.0;

        public override bool FeedsOnFire => true;

        public override string? StatusNote => NextIntent == EnemyIntent.Heavy ? "firestorm next" : $"heat {_heat}/{HeatForFirestorm}";

        // No dice for the big one: it comes every third turn, so it can be seen and planned for.
        protected override EnemyIntent PlanNextMove()
        {
            _heat++;
            if (_heat < HeatForFirestorm)
                return EnemyIntent.Attack;

            _heat = 0;
            return EnemyIntent.Heavy;
        }

        protected override string AfterHeavyBlow(Hero hero)
        {
            if (!LastHitLanded)
                return string.Empty;

            hero.Burn.Apply(3, 4 + CombatFloor);
            return "You are wreathed in fire.";
        }
    }
}
