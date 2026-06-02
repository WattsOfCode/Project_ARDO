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

        public int AttackBonus { get; set; }

        public int DefenseBonus { get; set; }

        public int HealAmount { get; set; }

        public bool Consumable { get; set; }

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

        public override string ToString()
        {
            string modifier = StatModifier != 0 ? $" ({StatModifier:+#;-#;0})" : string.Empty;
            return $"[{Rarity}] {Name}{modifier}";
        }
    }
}
