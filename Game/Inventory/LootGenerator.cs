/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `LootGenerator.cs` is a static utility class that rolls random loot rewards 
    for combat outcomes. It exposes helper methods.
 */
using ARDO.Core.Entities;
using ARDO.Core.Enums;
using ARDO.Core.Factories;

namespace ARDO.Game.Inventory
{
    public static class LootGenerator
    {
        private static readonly Random _random = new();

        public static GameItem? GenerateLoot()
        {
            int roll = _random.Next(1, 101);

            // 50% Health Potion
            if (roll <= 50)
            {
                return ItemFactory.CreateHealthPotion();
            }

            // 30% Iron Sword
            if (roll <= 80)
            {
                return ItemFactory.CreateIronSword();
            }

            // 20% Nothing
            return null;
        }
    }
}
