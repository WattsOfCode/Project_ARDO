/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `ItemFactory.cs` is a static factory file that creates the fixed consumables
    sold in the shop and dropped as loot. It exposes creation methods.
 */
using ARDO.Core.Entities;
using ARDO.Core.Enums;

namespace ARDO.Core.Factories
{
    public static class ItemFactory
    {
        public static Consumable CreateHealthPotion()
        {
            return new Consumable("Health Potion", "Restores 50 HP.", RarityType.Common, 30, 50);
        }

        public static Consumable CreateGreaterHealthPotion()
        {
            return new Consumable("Greater Health Potion", "Restores 90 HP.", RarityType.Uncommon, 55, 90);
        }

        public static Consumable CreateManaDraught()
        {
            return new Consumable("Mana Draught", "Restores 40 mana.", RarityType.Common, 30, 40, ItemEffect.RestoresMana);
        }

        public static Consumable CreateTravelRation()
        {
            return new Consumable("Travel Ration", "A simple ration for desperate moments.", RarityType.Common, 12, 20);
        }
    }
}
