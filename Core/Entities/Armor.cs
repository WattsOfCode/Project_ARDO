/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `Armor.cs` is a concrete item subclass that adds defense bonuses and an 
    equipment slot. It uses constructors and inherited properties.
 */
using ARDO.Core.Enums;

namespace ARDO.Core.Entities
{
    public class Armor : GameItem
    {
        public EquipmentSlot Slot { get; set; }

        public Armor(
            string name,
            string description,
            RarityType rarity,
            int value,
            int defenseBonus,
            EquipmentSlot slot)

            : base(
                name,
                description,
                ItemType.Armor,
                rarity,
                value,
                defenseBonus,
                slot)
        {
            DefenseBonus = defenseBonus;
            Slot = slot;
            StatModifier = defenseBonus;
        }
    }
}
