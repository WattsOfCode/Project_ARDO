/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `InventoryRepository.cs` is a concrete SQLite repository that synchronizes
    inventory items for a character. It uses public CRUD-style methods to
    save, read, update, and remove item records from the database.
 */
using System;
using Microsoft.Data.Sqlite;
using ARDO.Core.Abstracts;
using ARDO.Core.Entities;
using ARDO.Core.Enums;
using ARDO.Game.Inventory;

namespace ARDO.Data.SQLite
{
    public class InventoryRepository
    {
        private readonly DatabaseConfig _config;

        public InventoryRepository(DatabaseConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void HydrateHeroInventory(Hero hero)
        {
            using var connection = _config.CreateConnection();

            const string query = @"
                SELECT item_id, item_name, item_description, type_category, item_value,
                       stat_modifier, rarity_tier, equipment_slot, is_equipped, item_effect
                FROM inventory_items
                WHERE character_id = @charId
                ORDER BY item_id;";

            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@charId", hero.CharacterId);

            using var reader = command.ExecuteReader();

            hero.HeroInventory = new Inventory(Inventory.DefaultCapacity);
            hero.Equipment = new EquipmentManager();

            while (reader.Read())
            {
                GameItem item = MapItem(reader, out bool wasEquipped);
                hero.HeroInventory.AddItem(item);

                // Put saved gear back on, so its bonuses apply after loading.
                if (wasEquipped && EquipmentManager.CanEquip(item))
                {
                    hero.Equipment.Equip(item);
                }
            }
        }

        public void SaveInventoryItem(int characterId, GameItem item)
        {
            using var connection = _config.CreateConnection();

            const string query = @"
                INSERT INTO inventory_items
                    (character_id, item_name, item_description, type_category, item_value,
                     stat_modifier, rarity_tier, equipment_slot, is_equipped, item_effect)
                VALUES
                    (@charId, @name, @description, @type, @value, @modifier, @rarity, @slot, @equipped, @effect);
                SELECT last_insert_rowid();";

            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@charId", characterId);
            command.Parameters.AddWithValue("@name", item.Name);
            command.Parameters.AddWithValue("@description", item.Description);
            command.Parameters.AddWithValue("@type", item.ItemType.ToString());
            command.Parameters.AddWithValue("@value", item.Value);
            command.Parameters.AddWithValue("@modifier", item.StatModifier);
            command.Parameters.AddWithValue("@rarity", item.Rarity.ToString());
            command.Parameters.AddWithValue("@slot", item.EquippedSlot.ToString());
            command.Parameters.AddWithValue("@equipped", item.IsEquipped ? 1 : 0);
            command.Parameters.AddWithValue("@effect", item.Effect.ToString());

            object? insertedId = command.ExecuteScalar();
            item.ItemId = insertedId != null ? Convert.ToInt32(insertedId) : 0;
        }

        public void RemoveInventoryItem(int characterId, int itemId)
        {
            if (itemId <= 0)
            {
                return;
            }

            using var connection = _config.CreateConnection();
            const string query = @"
                DELETE FROM inventory_items
                WHERE character_id = @charId
                  AND item_id = @itemId;";

            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@charId", characterId);
            command.Parameters.AddWithValue("@itemId", itemId);
            command.ExecuteNonQuery();
        }

        // Writes which items are worn: the equipped weapon and armor get 1, everything else 0.
        public void SaveEquippedFlags(Hero hero)
        {
            using var connection = _config.CreateConnection();
            const string query = @"
                UPDATE inventory_items
                SET is_equipped = CASE WHEN item_id IN (@weaponId, @armorId) THEN 1 ELSE 0 END
                WHERE character_id = @charId;";

            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@weaponId", hero.Equipment.EquippedWeapon?.ItemId ?? -1);
            command.Parameters.AddWithValue("@armorId", hero.Equipment.EquippedArmor?.ItemId ?? -1);
            command.Parameters.AddWithValue("@charId", hero.CharacterId);
            command.ExecuteNonQuery();
        }

        private static GameItem MapItem(SqliteDataReader reader, out bool wasEquipped)
        {
            int itemId = Convert.ToInt32(reader["item_id"]);
            string itemName = reader["item_name"]?.ToString() ?? "Unknown Item";
            string description = reader["item_description"]?.ToString() ?? string.Empty;
            string typeText = reader["type_category"]?.ToString() ?? ItemType.Miscellaneous.ToString();
            string rarityText = reader["rarity_tier"]?.ToString() ?? RarityType.Common.ToString();
            string slotText = reader["equipment_slot"]?.ToString() ?? EquipmentSlot.None.ToString();
            int value = Convert.ToInt32(reader["item_value"]);
            int statModifier = Convert.ToInt32(reader["stat_modifier"]);
            wasEquipped = Convert.ToInt32(reader["is_equipped"]) == 1;
            string effectText = reader["item_effect"]?.ToString() ?? ItemEffect.None.ToString();

            Enum.TryParse(typeText, ignoreCase: true, out ItemType itemType);
            Enum.TryParse(rarityText, ignoreCase: true, out RarityType rarity);
            Enum.TryParse(slotText, ignoreCase: true, out EquipmentSlot slot);
            Enum.TryParse(effectText, ignoreCase: true, out ItemEffect effect);

            GameItem item = itemType switch
            {
                ItemType.Weapon => new Weapon(itemName, description, rarity, value, statModifier, effect),
                ItemType.Armor => new Armor(itemName, description, rarity, value, statModifier, slot, effect),
                ItemType.Consumable => new Consumable(itemName, description, rarity, value, statModifier, effect),
                _ => new GameItem(itemName, description, itemType, rarity, value, statModifier, slot)
            };

            item.ItemId = itemId;
            return item;
        }
    }
}
