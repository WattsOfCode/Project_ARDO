/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `HollowCaptain.cs` is a concrete enemy subclass that serves as a stronger polymorphic 
 combat opponent. It uses inherited combat members and constructors for stats.
 */
namespace ARDO.Game.Enemies
{
    // The Hollow Captain winds up heavy blows often, and they land hard. Guard against them.
    public class HollowCaptain : Boss
    {
        public HollowCaptain() : base("Hollow Captain", 120, 20)
        {
        }

        public override string ArtKey => "HollowCaptain";

        public override string AttackLine => "drives a brutal captain's cut toward you.";

        public override string HeavyAttackLine => "brings his greatblade down in a cleaving arc.";

        public override string WindUpWarning => $"{Name} lifts his greatblade high overhead!";

        protected override int HeavyChancePercent => 35;

        protected override double HeavyMultiplier => 2.2;
    }
}
