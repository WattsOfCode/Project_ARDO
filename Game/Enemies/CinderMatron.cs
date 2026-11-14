/**
 * Name: John Deardorff
 * Date: 11/13/26
 * Project ARDO
 *
 * Class description:
 * `CinderMatron.cs` is the guardian of the Emberward Depths. She builds heat
    every turn and lets it go as a Firestorm on the third, which sets the hero
    burning. Fire only feeds her: a burn on her heals instead of hurting. On a
    later turn of the wheel, at half health she breaks the floor open, and the
    magma burns the hero a little more every round until she falls.
 */
using ARDO.Core.Abstracts;
using ARDO.Core.Enums;

namespace ARDO.Game.Enemies
{
    public sealed class CinderMatron : Boss
    {
        private const int HeatForFirestorm = 3;
        private int _heat;

        // Second phase: the floor burns for this much each round, and more each round after.
        private const int FloorHeatStart = 4;
        private const int FloorHeatRise = 3;
        public int FloorHeat { get; private set; }

        public CinderMatron() : base("The Cinder Matron", 240, 15)
        {
            XpReward = 220;
            GoldReward = 170;
        }

        public override string ArtKey => "cindermatron";
        public override string AttackLine => "rakes at you with four clawed hands of cracked, burning stone.";
        public override string HeavyAttackLine => "opens her arms, and the Firestorm pours out of her.";
        public override string WindUpWarning => $"{Name} spreads all four arms and the fire in her cracks blazes white. A Firestorm is coming!";
        protected override double HeavyMultiplier => 2.0;

        public override bool FeedsOnFire => true;

        public override string? StatusNote
        {
            get
            {
                string heat = NextIntent == EnemyIntent.Heavy ? "firestorm next" : $"heat {_heat}/{HeatForFirestorm}";
                return FloorHeat > 0 ? $"{heat}, floor burns {FloorHeat}" : heat;
            }
        }

        protected override bool HasSecondPhase => true;

        protected override void BeginSecondPhase()
        {
            FloorHeat = FloorHeatStart + CombatFloor;
            Announce($"{Name} drives all four fists into the ground. The floor splits, and magma wells up around your feet. " +
                     "It will burn hotter every round until she falls.");
        }

        public override string TakeTurn(Hero hero)
        {
            string line = base.TakeTurn(hero);
            if (FloorHeat <= 0 || !hero.IsAlive)
                return line;

            int scorched = hero.TakeUnblockableDamage(FloorHeat);
            line += $" The molten floor sears you for {scorched}.";
            FloorHeat += FloorHeatRise;
            return line;
        }

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
