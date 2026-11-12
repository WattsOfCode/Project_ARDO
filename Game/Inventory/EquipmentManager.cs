/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `EquipmentManager.cs` is a concrete composition helper that equips and unequips
    items for the hero. It holds one weapon and one armor piece and exposes the
    bonuses they give, which the hero uses in combat.
 */
using ARDO.Core.Entities;

namespace ARDO.Game.Inventory
{
    public class EquipmentManager
    {
        public Weapon? EquippedWeapon { get; private set; }

        public Armor? EquippedArmor { get; private set; }

        public int DamageBonus => EquippedWeapon?.DamageBonus ?? 0;

        public int DefenseBonus => EquippedArmor?.DefenseBonus ?? 0;

        public static bool CanEquip(GameItem item) => item is Weapon or Armor;

        // True when the item would raise the stat its slot gives (or the slot is empty).
        public bool IsUpgrade(GameItem item)
        {
            return item switch
            {
                Weapon weapon => weapon.DamageBonus > DamageBonus || EquippedWeapon == null,
                Armor armor => armor.DefenseBonus > DefenseBonus || EquippedArmor == null,
                _ => false
            };
        }

        // Equips a weapon or armor piece. Returns the item it replaced, or null.
        public GameItem? Equip(GameItem item)
        {
            GameItem? replaced = null;

            switch (item)
            {
                case Weapon weapon:
                    replaced = EquippedWeapon;
                    EquippedWeapon = weapon;
                    break;

                case Armor armor:
                    replaced = EquippedArmor;
                    EquippedArmor = armor;
                    break;

                default:
                    return null;
            }

            if (replaced != null)
            {
                replaced.IsEquipped = false;
            }

            item.IsEquipped = true;
            return replaced;
        }

        public bool Unequip(GameItem item)
        {
            if (ReferenceEquals(item, EquippedWeapon))
            {
                EquippedWeapon = null;
            }
            else if (ReferenceEquals(item, EquippedArmor))
            {
                EquippedArmor = null;
            }
            else
            {
                return false;
            }

            item.IsEquipped = false;
            return true;
        }
    }
}
