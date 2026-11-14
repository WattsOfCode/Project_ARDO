/**
 * Name: John Deardorff
 * Date: 11/14/26
 * Project ARDO
 *
 * Class description:
 * `GoblinBrute.cs` is a big goblin of the king's road, slow and heavy. It often
    winds up a crushing blow a turn ahead, so a guard or a pinning shot pays off.
 */
namespace ARDO.Game.Enemies
{
    public sealed class GoblinBrute : Minion
    {
        public GoblinBrute() : base("Goblin Brute", 150, 16, 80, 35)
        {
        }

        public override string ArtKey => "goblinbrute";
        public override string AttackLine => "swings a cart axle like a club.";
        public override string HeavyAttackLine => "brings the axle down with everything it has.";
        public override string WindUpWarning => $"{Name} hoists its cart axle over its head!";
        protected override int HeavyChancePercent => 40;
        protected override double HeavyMultiplier => 2.3;
    }
}
