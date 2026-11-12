/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `GameItem.cs` is a concrete base item model used by inventory, combat, 
    and shop systems. It exposes item properties and constructors.
 */
using ARDO.Core.Enums;

namespace ARDO.Core.Entities
{
    public class GameItem
    {
        public int ItemId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public ItemType ItemType { get; set; }

        public RarityType Rarity { get; set; }

        public int Value { get; set; }

        public int StatModifier { get; set; }

        public EquipmentSlot EquippedSlot { get; set; }

        public bool IsEquipped { get; set; }

        public int DefenseBonus { get; set; }

        public int HealAmount { get; set; }

        public ItemEffect Effect { get; set; }

        // Short description of the item's special effect, for shop and loot lists.
        public string EffectText => Effect switch
        {
            ItemEffect.Burning => "sets foes burning",
            ItemEffect.Vampiric => "heals you on hit",
            ItemEffect.Keen => "+10% crit",
            ItemEffect.Warded => "halves the first hit each fight",
            ItemEffect.RestoresMana => $"restores {StatModifier} mana",
            _ => string.Empty
        };

        public GameItem(
            string name,
            string description,
            ItemType itemType,
            RarityType rarity,
            int value,
            int statModifier = 0,
            EquipmentSlot equippedSlot = EquipmentSlot.None)
        {
            Name = name;
            Description = description;
            ItemType = itemType;
            Rarity = rarity;
            Value = value;
            StatModifier = statModifier;
            EquippedSlot = equippedSlot;
        }

        // One-line stats for lists and loot messages, e.g. "+12 ATK, sets foes burning".
        public virtual string Summary => EffectText;

        public override string ToString()
        {
            return $"[{Rarity}] {Name}";
        }
    }
}
