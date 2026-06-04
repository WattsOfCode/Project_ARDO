/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `FireImp.cs` is a concrete enemy subclass with a unique combat style for the game's 
    polymorphic battle flow. It relies on inherited properties and overridden methods.
 */
namespace ARDO.Game.Enemies
{
    public class FireImp : Minion
    {
        public FireImp() : base("Fire Imp", 48, 10, 20, 12)
        {
        }
    }
}
