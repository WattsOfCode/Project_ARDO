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

        public static Armor CreateWoodShield()
        {
            return new Armor(
                "Wood Shield",
                "Simple defensive shield.",
                RarityType.Common,
                40,
                3,
                EquipmentSlot.Shield
            );
        }
    }
}