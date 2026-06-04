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
namespace ARDO.Game.Enemies
{
    public class WraithHerald : Boss
    {
        public WraithHerald() : base("Wraith Herald", 110, 18)
        {
        }
    }
}
