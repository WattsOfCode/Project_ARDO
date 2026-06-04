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
            _config = config;
        }

        public void HydrateHeroInventory(Hero hero)
        {
            using var connection = _config.CreateConnection();

            string query = @"
                SELECT
                    item_id,
                    item_name,
                    item_description,
                    type_category,
                    item_value,
                    stat_modifier,
                    rarity_tier,
                    equipment_slot,
                    is_equipped
                FROM inventory_items
                WHERE character_id = @charId;
            ";

            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@charId", hero.CharacterId);

            using var reader = command.ExecuteReader();

            hero.HeroInventory = new Inventory(20);

            while (reader.Read())
            {
                GameItem item = MapItem(reader);
                hero.HeroInventory.AddItem(item);
            }
        }

        public void SaveInventoryItem(int characterId, GameItem item)
        {
            using var connection = _config.CreateConnection();

            string query = @"
                INSERT INTO inventory_items
                (
                    character_id,
                    item_name,
                    item_description,
                    type_category,
                    item_value,
                    stat_modifier,
                    rarity_tier,
                    equipment_slot,
                    is_equipped
                )
                VALUES
                (
                    @charId,
                    @name,
                    @description,
                    @type,
                    @value,
                    @modifier,
                    @rarity,
                    @slot,
                    @equipped
                );
            ";

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

            command.ExecuteNonQuery();

            using var idCommand = new SqliteCommand("SELECT last_insert_rowid();", connection);
            object? insertedId = idCommand.ExecuteScalar();
            item.ItemId = insertedId != null ? Convert.ToInt32(insertedId) : 0;
        }

        public void RemoveInventoryItem(int characterId, int itemId)
        {
            if (itemId <= 0)
            {
                return;
            }

            using var connection = _config.CreateConnection();
            string query = @"
                DELETE FROM inventory_items
                WHERE character_id = @charId
                  AND item_id = @itemId;";

            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@charId", characterId);
            command.Parameters.AddWithValue("@itemId", itemId);
            command.ExecuteNonQuery();
        }

        private static GameItem MapItem(SqliteDataReader reader)
        {
            int itemId = Convert.ToInt32(reader["item_id"]);
            string itemName = reader["item_name"]?.ToString() ?? "Unknown Item";
            string description = reader["item_description"]?.ToString() ?? string.Empty;
            string typeText = reader["type_category"]?.ToString() ?? ItemType.Miscellaneous.ToString();
            string rarityText = reader["rarity_tier"]?.ToString() ?? RarityType.Common.ToString();
            string slotText = reader["equipment_slot"]?.ToString() ?? EquipmentSlot.None.ToString();
            int value = Convert.ToInt32(reader["item_value"]);
            int statModifier = Convert.ToInt32(reader["stat_modifier"]);
            bool isEquipped = Convert.ToInt32(reader["is_equipped"]) == 1;

            Enum.TryParse(typeText, ignoreCase: true, out ItemType itemType);
            Enum.TryParse(rarityText, ignoreCase: true, out RarityType rarity);
            Enum.TryParse(slotText, ignoreCase: true, out EquipmentSlot slot);

            GameItem item = itemType switch
            {
                ItemType.Weapon => new Weapon(itemName, description, rarity, value, statModifier),
                ItemType.Armor => new Armor(itemName, description, rarity, value, statModifier, slot),
                ItemType.Consumable => new Consumable(itemName, description, rarity, value, statModifier),
                _ => new GameItem(itemName, description, itemType, rarity, value, statModifier, slot)
            };

            item.ItemId = itemId;
            item.IsEquipped = isEquipped;
            return item;
        }
    }
}
