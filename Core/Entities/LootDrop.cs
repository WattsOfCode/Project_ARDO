/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `LootDrop.cs` is a concrete value object used to describe reward 
    drops from combat. It has simple properties.
 */
namespace ARDO.Core.Entities
{
    public class LootDrop
    {
        public GameItem Item { get; set; } = null!;

        public double DropChance { get; set; }
    }
}
