/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `PlayerRepository.cs` is a concrete SQLite repository that creates, reads,
    updates, and deletes player profiles and game state. It uses constructors
    and public CRUD methods to manage character progression, inventory,
    and story persistence.
 */
using System;
using System.Data;
using System.IO;
using Microsoft.Data.Sqlite;
using ARDO.Core.Abstracts;
using ARDO.Core.Entities;
using ARDO.Core.Enums;
using ARDO.Core.Factories;
using ARDO.Game.Players;

namespace ARDO.Data.SQLite
{
    public class PlayerRepository : IPlayerRepository
    {
        private readonly DatabaseConfig _config;
        private readonly InventoryRepository _inventoryRepo;

        public PlayerRepository(DatabaseConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _inventoryRepo = new InventoryRepository(config);
        }

        public int CreatePlayer(string name, string classType)
        {
            // The class constructors own the starting stats, so they are written in one place.
            Hero hero = HeroFactory.Create(name, classType);
            int mana = hero is Mage mage ? mage.MaxMana : 0;

            using var connection = _config.CreateConnection();

            const string insertQuery = @"
                INSERT INTO characters (name, class_type, level, experience, current_health, max_health, current_mana, max_mana, gold, active_story_node)
                VALUES (@name, @classType, @level, 0, @hp, @hp, @mana, @mana, @gold, 1);
                SELECT last_insert_rowid();";

            using var command = new SqliteCommand(insertQuery, connection);
            command.Parameters.AddWithValue("@name", hero.Name);
            command.Parameters.AddWithValue("@classType", classType);
            command.Parameters.AddWithValue("@level", hero.Level);
            command.Parameters.AddWithValue("@hp", hero.MaxHealth);
            command.Parameters.AddWithValue("@mana", mana);
            command.Parameters.AddWithValue("@gold", hero.Gold);

            object? result = command.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        public DataTable GetLoadSlots()
        {
            DataTable table = new DataTable();
            using var connection = _config.CreateConnection();
            const string query = @"
                SELECT c.character_id, c.name, c.class_type, c.level, c.gold, c.story_complete, c.wheel_ending, q.title as current_quest
                FROM characters c
                JOIN quest_templates q ON c.active_story_node = q.quest_id";

            using var command = new SqliteCommand(query, connection);
            using var reader = command.ExecuteReader();
            table.Load(reader);
            return table;
        }

        public Hero? LoadFullProfile(int characterId)
        {
            Hero? hero = null;

            using (var connection = _config.CreateConnection())
            {
                const string query = "SELECT * FROM characters WHERE character_id = @charId;";
                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@charId", characterId);

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    string name = reader["name"]?.ToString() ?? "Unknown Hero";
                    string classType = reader["class_type"]?.ToString() ?? "Warrior";

                    hero = HeroFactory.Create(name, classType);
                    hero.CharacterId = Convert.ToInt32(reader["character_id"]);
                    hero.Level = Convert.ToInt32(reader["level"]);
                    hero.Experience = Convert.ToInt32(reader["experience"]);
                    hero.MaxHealth = Convert.ToInt32(reader["max_health"]);
                    hero.CurrentHealth = Convert.ToInt32(reader["current_health"]);
                    hero.Gold = Convert.ToInt32(reader["gold"]);
                    hero.ActiveStoryNode = Convert.ToInt32(reader["active_story_node"]);
                    hero.StoryComplete = Convert.ToInt32(reader["story_complete"]) == 1;
                    hero.WheelKeys = Convert.ToInt32(reader["wheel_keys"]);
                    hero.WheelEnding = (WheelEnding)Convert.ToInt32(reader["wheel_ending"]);
                    hero.PendingPerkPicks = Convert.ToInt32(reader["pending_perks"]);
                    hero.GuardiansSlain = Convert.ToInt32(reader["guardians_slain"]);

                    // Perks are saved by name, comma-separated; a name this build doesn't know is skipped.
                    foreach (string perkName in (reader["perks"]?.ToString() ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (Enum.TryParse(perkName.Trim(), out PerkId perk))
                            hero.Perks.Add(perk);
                    }

                    if (hero is Mage mage)
                    {
                        mage.CurrentMana = Convert.ToInt32(reader["current_mana"]);
                        mage.MaxMana = Convert.ToInt32(reader["max_mana"]);
                    }
                }
            }

            if (hero != null)
            {
                _inventoryRepo.HydrateHeroInventory(hero);
            }

            return hero;
        }

        public void SaveGameState(Hero character)
        {
            using var connection = _config.CreateConnection();
            const string query = @"
                UPDATE characters
                SET level = @level, experience = @xp, current_health = @currentHp, max_health = @maxHp,
                    current_mana = @currentMana, max_mana = @maxMana, gold = @gold, active_story_node = @storyNode,
                    story_complete = @storyComplete, wheel_keys = @wheelKeys, wheel_ending = @wheelEnding,
                    perks = @perks, pending_perks = @pendingPerks, guardians_slain = @guardians
                WHERE character_id = @charId;";

            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@level", character.Level);
            command.Parameters.AddWithValue("@xp", character.Experience);
            command.Parameters.AddWithValue("@currentHp", character.CurrentHealth);
            command.Parameters.AddWithValue("@maxHp", character.MaxHealth);
            command.Parameters.AddWithValue("@currentMana", character is Mage m ? m.CurrentMana : 0);
            command.Parameters.AddWithValue("@maxMana", character is Mage mage ? mage.MaxMana : 0);
            command.Parameters.AddWithValue("@gold", character.Gold);
            command.Parameters.AddWithValue("@storyNode", character.ActiveStoryNode);
            command.Parameters.AddWithValue("@storyComplete", character.StoryComplete ? 1 : 0);
            command.Parameters.AddWithValue("@wheelKeys", character.WheelKeys);
            command.Parameters.AddWithValue("@wheelEnding", (int)character.WheelEnding);
            command.Parameters.AddWithValue("@perks", string.Join(",", character.Perks));
            command.Parameters.AddWithValue("@pendingPerks", character.PendingPerkPicks);
            command.Parameters.AddWithValue("@guardians", character.GuardiansSlain);
            command.Parameters.AddWithValue("@charId", character.CharacterId);

            command.ExecuteNonQuery();
        }

        public void DeleteProfile(int characterId)
        {
            using var connection = _config.CreateConnection();
            const string query = "DELETE FROM characters WHERE character_id = @charId;";
            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@charId", characterId);
            command.ExecuteNonQuery();
        }

        public void SaveInventoryItem(int characterId, GameItem item)
        {
            _inventoryRepo.SaveInventoryItem(characterId, item);
        }

        public void RemoveInventoryItem(int characterId, int itemId)
        {
            _inventoryRepo.RemoveInventoryItem(characterId, itemId);
        }

        public void SaveEquipment(Hero character)
        {
            _inventoryRepo.SaveEquippedFlags(character);
        }
    }
}
