using ARDO.Core.Enums;

namespace ARDO.Core.Entities
{
    public class Consumable : GameItem
    {
        public Consumable(
            string name,
            string description,
            RarityType rarity,
            int value,
            int healAmount)

            : base(
                name,
                description,
                ItemType.Consumable,
                rarity,
                value,
                healAmount)
        {
            HealAmount = healAmount;
            Consumable = true;
            StatModifier = healAmount;
        }
    }
}
