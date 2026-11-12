/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `Alhuezo.cs` is the concrete final boss, the First King. Its stats are set here
    and nowhere else; the combat engine plays its encounter as a scripted ending.
 */
namespace ARDO.Game.Enemies
{
    public sealed class Alhuezo : Boss
    {
        public Alhuezo() : base("Alhuezo, the First King", 999, 80)
        {
        }

        public override string ArtKey => "Alhuezo";

        public override string AttackLine => "lifts the room with a king's crushing blow.";
    }
}
