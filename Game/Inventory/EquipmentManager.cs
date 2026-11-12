/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `EquipmentManager.cs` is a concrete composition helper that equips and unequips
    items for the hero. It holds one weapon plus one armor piece in each of the
    five armor slots (helmet, chest, shield, boots, ring) and exposes the
    bonuses and effects they give, which the hero uses in combat.
 */
using System.Collections.Generic;
using System.Linq;
using ARDO.Core.Entities;
using ARDO.Core.Enums;

namespace ARDO.Game.Inventory
{
    public class EquipmentManager
    {
        // The armor slots in the order the gear screen and stats list them.
        public static readonly EquipmentSlot[] ArmorSlots =
        {
            EquipmentSlot.Helmet,
            EquipmentSlot.Chest,
            EquipmentSlot.Shield,
            EquipmentSlot.Boots,
            EquipmentSlot.Ring
        };

        private readonly Dictionary<EquipmentSlot, Armor> _armor = new();

        public Weapon? EquippedWeapon { get; private set; }

        // The chest piece, the slot older saves and most armor use.
        public Armor? EquippedArmor => ArmorIn(EquipmentSlot.Chest);

        public int DamageBonus => EquippedWeapon?.DamageBonus ?? 0;

        public int DefenseBonus => _armor.Values.Sum(armor => armor.DefenseBonus);

        public int ArmorPiecesWorn => _armor.Count;

        // Everything being worn, weapon first.
        public IEnumerable<GameItem> Worn =>
            (EquippedWeapon == null ? Enumerable.Empty<GameItem>() : new GameItem[] { EquippedWeapon })
            .Concat(ArmorSlots.Where(_armor.ContainsKey).Select(slot => _armor[slot]));

        public static bool CanEquip(GameItem item) => item is Weapon or Armor;

        // Armor saved before there were several slots has no slot set; it is chest armor.
        public static EquipmentSlot SlotFor(GameItem item)
        {
            return item switch
            {
                Weapon => EquipmentSlot.Weapon,
                Armor armor when ArmorSlots.Contains(armor.Slot) => armor.Slot,
                Armor => EquipmentSlot.Chest,
                _ => EquipmentSlot.None
            };
        }

        public Armor? ArmorIn(EquipmentSlot slot) => _armor.TryGetValue(slot, out Armor? armor) ? armor : null;

        public GameItem? ItemIn(EquipmentSlot slot) => slot == EquipmentSlot.Weapon ? EquippedWeapon : ArmorIn(slot);

        public GameItem? FirstWithEffect(ItemEffect effect) => Worn.FirstOrDefault(item => item.Effect == effect);

        public bool HasEffect(ItemEffect effect) => FirstWithEffect(effect) != null;

        public int CountEffect(ItemEffect effect) => Worn.Count(item => item.Effect == effect);

        // True when the item would raise the stat its slot gives (or the slot is empty).
        public bool IsUpgrade(GameItem item)
        {
            return item switch
            {
                Weapon weapon => EquippedWeapon == null || weapon.DamageBonus > EquippedWeapon.DamageBonus,
                Armor armor => ArmorIn(SlotFor(armor)) is not Armor worn || armor.DefenseBonus > worn.DefenseBonus,
                _ => false
            };
        }

        // Equips a weapon or armor piece in its slot. Returns the item it replaced, or null.
        public GameItem? Equip(GameItem item)
        {
            GameItem? replaced;

            switch (item)
            {
                case Weapon weapon:
                    replaced = EquippedWeapon;
                    EquippedWeapon = weapon;
                    break;

                case Armor armor:
                    EquipmentSlot slot = SlotFor(armor);
                    replaced = ArmorIn(slot);
                    _armor[slot] = armor;
                    break;

                default:
                    return null;
            }

            if (replaced != null && !ReferenceEquals(replaced, item))
            {
                replaced.IsEquipped = false;
            }

            item.IsEquipped = true;
            return ReferenceEquals(replaced, item) ? null : replaced;
        }

        public bool Unequip(GameItem item)
        {
            if (ReferenceEquals(item, EquippedWeapon))
            {
                EquippedWeapon = null;
            }
            else if (item is Armor armor && ReferenceEquals(ArmorIn(SlotFor(armor)), armor))
            {
                _armor.Remove(SlotFor(armor));
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
