/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `Weapon.cs` is a concrete item subclass that adds attack bonuses to the 
    shared game item model. It uses constructors and inherited properties.
 */
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
