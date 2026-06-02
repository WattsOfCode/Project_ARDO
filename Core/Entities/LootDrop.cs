namespace ARDO.Core.Entities
{
    public class LootDrop
    {
        public GameItem Item { get; set; } = null!;

        public double DropChance { get; set; }
    }
}