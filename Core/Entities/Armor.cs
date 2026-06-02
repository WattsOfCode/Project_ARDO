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
