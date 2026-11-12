/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `Goblin.cs` is a concrete enemy subclass that provides a simple polymorphic enemy type for 
    early encounters. It uses inherited combat members and constructor-driven stats.
 */
namespace ARDO.Game.Enemies
{
    public class Goblin : Minion
    {
        public Goblin() : base("Goblin", 40, 6, 15, 10)
        {
        }

        public override string ArtKey => "goblin";

        public override string AttackLine => "lunges with a jagged blade.";
    }
}
