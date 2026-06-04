/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `EquipmentManager.cs` is a concrete composition helper that equips and unequips 
    items for the hero. It uses public methods to modify combat stats through 
    composition.
 */
using ARDO.Core.Entities;

namespace ARDO.Game.Inventory
{
    public class EquipmentManager
    {
        public Weapon? EquippedWeapon { get; private set; }

        public Armor? EquippedArmor { get; private set; }

        public void EquipWeapon(Weapon weapon)
        {
            EquippedWeapon = weapon;
        }

        public void EquipArmor(Armor armor)
        {
            EquippedArmor = armor;
        }

        public void UnequipWeapon()
        {
            EquippedWeapon = null;
        }

        public void UnequipArmor()
        {
            EquippedArmor = null;
        }
    }
}
