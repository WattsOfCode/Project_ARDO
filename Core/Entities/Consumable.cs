/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `Consumable.cs` is a concrete item subclass that stores healing (or mana) values
    for inventory use. It uses constructors and inherited properties.
 */
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
            int amount,
            ItemEffect effect = ItemEffect.None)

            : base(
                name,
                description,
                ItemType.Consumable,
                rarity,
                value,
                amount)
        {
            Effect = effect;
            StatModifier = amount;
            HealAmount = RestoresMana ? 0 : amount;
        }

        public bool RestoresMana => Effect == ItemEffect.RestoresMana;

        public string UseText => RestoresMana ? $"restores {StatModifier} mana" : $"heals {HealAmount} HP";

        public override string Summary => UseText;
    }
}
