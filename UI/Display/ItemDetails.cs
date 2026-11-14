/**
 * Name: John Deardorff
 * Date: 11/14/26
 * Project ARDO
 *
 * Class description:
 * `ItemDetails.cs` writes the details box the shop and the pack show when an item
    is clicked, in three lines: the item's name (with its price, if any), its
    description, and what it does for this hero, compared with what they wear in
    that slot now.
 */
using ARDO.Core.Abstracts;
using ARDO.Core.Entities;
using ARDO.Game.Inventory;

namespace ARDO.UI.Display
{
    public static class ItemDetails
    {
        public static string Describe(Hero hero, GameItem item, string? price = null, string? action = null)
        {
            string kind = item switch
            {
                Weapon => "weapon",
                Armor armor => armor.EquippedSlot.ToString().ToLowerInvariant(),
                Consumable => "consumable",
                _ => "item"
            };

            string title = $"{item.Name} ({item.Rarity} {kind})";
            if (!string.IsNullOrEmpty(price))
                title += $" - {price}";

            string effects = $"{item.Summary}.{Compare(hero, item)}";
            if (!string.IsNullOrEmpty(action))
                effects += $" {action}";

            return string.IsNullOrWhiteSpace(item.Description)
                ? $"{title}\n{effects}"
                : $"{title}\n{item.Description}\n{effects}";
        }

        // How the item stacks up against what is worn in its slot.
        private static string Compare(Hero hero, GameItem item)
        {
            if (!EquipmentManager.CanEquip(item))
                return string.Empty;

            string stat = item is Weapon ? "ATK" : "DEF";
            GameItem? worn = hero.Equipment.ItemIn(EquipmentManager.SlotFor(item));
            if (worn == null)
                return $" Nothing worn there now: +{item.StatModifier} {stat}.";

            if (ReferenceEquals(worn, item))
                return " You are wearing it.";

            int change = item.StatModifier - worn.StatModifier;
            string verdict = change > 0 ? "an upgrade" : change < 0 ? "a downgrade" : "the same";
            return $" Against your {worn.Name}: {(change >= 0 ? "+" : "")}{change} {stat}, {verdict}.";
        }
    }
}
