/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `ItemFactory.cs` is a static factory file that creates common game items
     for loot drops. It exposes creation methods.
 */
using ARDO.Core.Entities;
using ARDO.Core.Enums;

namespace ARDO.Core.Factories
{
    public static class ItemFactory
    {
        public static Consumable CreateHealthPotion()
        {
            return new Consumable(
                "Health Potion",
                "Restores 50 HP.",
                RarityType.Common,
                25,
                50
            );
        }

        public static Weapon CreateIronSword()
        {
            return new Weapon(
                "Iron Sword",
                "A sturdy iron sword.",
                RarityType.Common,
                50,
                5
            );
        }
    }
}
