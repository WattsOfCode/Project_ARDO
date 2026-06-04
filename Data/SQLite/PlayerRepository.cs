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
            int baseHp = classType == "Warrior" ? 150 : 90;
            int baseMana = classType == "Mage" ? 100 : 0;

            using var connection = _config.CreateConnection();

            string insertQuery = @"
                INSERT INTO characters (name, class_type, level, experience, current_health, max_health, current_mana, max_mana, gold, active_story_node)
                VALUES (@name, @classType, 1, 0, @baseHp, @baseHp, @baseMana, @baseMana, 100, 1);";

            using (var command = new SqliteCommand(insertQuery, connection))
            {
                command.Parameters.AddWithValue("@name", name);
                command.Parameters.AddWithValue("@classType", classType);
                command.Parameters.AddWithValue("@baseHp", baseHp);
                command.Parameters.AddWithValue("@baseMana", baseMana);
                command.ExecuteNonQuery();
            }

            string idQuery = "SELECT last_insert_rowid();";
            object? result;

            using (var command = new SqliteCommand(idQuery, connection))
            {
                result = command.ExecuteScalar();
            }

            return result != null ? Convert.ToInt32(result) : 0;
        }

        public DataTable GetLoadSlots()
        {
            DataTable table = new DataTable();
            using var connection = _config.CreateConnection();
            string query = @"
                SELECT c.character_id, c.name, c.class_type, c.level, c.gold, q.title as current_quest
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
                string query = "SELECT * FROM characters WHERE character_id = @charId;";
                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@charId", characterId);

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    string name = reader["name"]?.ToString() ?? "Unknown Hero";
                    string classType = reader["class_type"]?.ToString() ?? "Warrior";

                    int maxHp = Convert.ToInt32(reader["max_health"]);
                    int currentHp = Convert.ToInt32(reader["current_health"]);

                    hero = classType switch
                    {
                        "Warrior" => new Warrior(name),
                        "Mage" => new Mage(name),
                        _ => throw new InvalidDataException($"Unknown hero profile variant: {classType}")
                    };

                    hero.CharacterId = Convert.ToInt32(reader["character_id"]);
                    hero.Level = Convert.ToInt32(reader["level"]);
                    hero.Experience = Convert.ToInt32(reader["experience"]);
                    hero.MaxHealth = maxHp;
                    hero.CurrentHealth = currentHp;
                    hero.Gold = Convert.ToInt32(reader["gold"]);

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

        public void SaveGameState(Hero character, int activeStoryNode)
        {
            using var connection = _config.CreateConnection();
            string query = @"
                UPDATE characters 
                SET level = @level, experience = @xp, current_health = @currentHp, max_health = @maxHp, 
                    current_mana = @currentMana, max_mana = @maxMana, gold = @gold, active_story_node = @storyNode
                WHERE character_id = @charId;";

            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@level", character.Level);
            command.Parameters.AddWithValue("@xp", character.Experience);
            command.Parameters.AddWithValue("@currentHp", character.CurrentHealth);
            command.Parameters.AddWithValue("@maxHp", character.MaxHealth);
            command.Parameters.AddWithValue("@currentMana", character is Mage m ? m.CurrentMana : 0);
            command.Parameters.AddWithValue("@maxMana", character is Mage mage ? mage.MaxMana : 0);
            command.Parameters.AddWithValue("@gold", character.Gold);
            command.Parameters.AddWithValue("@storyNode", activeStoryNode);
            command.Parameters.AddWithValue("@charId", character.CharacterId);

            command.ExecuteNonQuery();
        }

        public void DeleteProfile(int characterId)
        {
            using var connection = _config.CreateConnection();
            string query = "DELETE FROM characters WHERE character_id = @charId;";
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
    }
}
