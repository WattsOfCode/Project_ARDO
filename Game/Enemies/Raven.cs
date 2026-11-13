/**
 * Name: John Deardorff
 * Date: 11/13/26
 * Project ARDO
 *
 * Class description:
 * `Raven.cs` is one of the Raven Witch's birds, called down from the rafters
    once she is badly hurt. Small and quick to kill, but it adds up.
 */
namespace ARDO.Game.Enemies
{
    public sealed class Raven : Minion
    {
        public Raven() : base("Raven", 30, 8, 5, 0)
        {
        }

        public override string ArtKey => "raven";
        public override string AttackLine => "dives and rakes at your face.";
    }
}
