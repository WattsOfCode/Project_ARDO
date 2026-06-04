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
    public class HollowCaptain : Boss
    {
        public HollowCaptain() : base("Hollow Captain", 120, 20)
        {
        }
    }
}
