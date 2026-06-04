/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `Skeleton.cs` is a concrete enemy subclass that supplies another polymorphic 
    combat variant for dungeon encounters. It uses inherited members and 
    constructor-based stats.
 */
namespace ARDO.Game.Enemies
{
    public class Skeleton : Minion
    {
        public Skeleton() : base("Skeleton", 55, 8, 25, 15)
        {
        }
    }
}
