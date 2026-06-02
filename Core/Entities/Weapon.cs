using ARDO.Core.Enums;

namespace ARDO.Core.Entities
{
    public class Weapon : GameItem
    {
        public int DamageBonus { get; set; }

        public Weapon(
            string name,
            string description,
            RarityType rarity,
            int value,
            int damageBonus)

            : base(
                name,
                description,
                ItemType.Weapon,
                rarity,
                value,
                damageBonus,
                EquipmentSlot.Weapon)
        {
            DamageBonus = damageBonus;
            StatModifier = damageBonus;
        }
    }
}
