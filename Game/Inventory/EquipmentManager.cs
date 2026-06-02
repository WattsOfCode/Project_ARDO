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