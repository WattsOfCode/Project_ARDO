using Microsoft.Data.Sqlite;
using System;

namespace ARDO.Data.SQLite
{
    public class DatabaseConfig
    {
        private const string ConnectionString = "Data Source=ardo.db";

        public SqliteConnection CreateConnection()
        {
            var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var pragmaCmd = connection.CreateCommand();
            pragmaCmd.CommandText = "PRAGMA foreign_keys = ON;";
            pragmaCmd.ExecuteNonQuery();

            return connection;
        }

        public void InitializeDatabase()
        {
            using var connection = CreateConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Defining tables Schema
                string createQuestsTable = @"
                    CREATE TABLE IF NOT EXISTS quest_templates (
                        quest_id INTEGER PRIMARY KEY,
                        title TEXT NOT NULL,
                        narrative_intro TEXT NOT NULL,
                        target_enemy_type TEXT NOT NULL DEFAULT 'Minion',
                        is_final_node INTEGER NOT NULL DEFAULT 0 CHECK(is_final_node IN (0, 1))
                    );";

                string createNPCsTable = @"
                    CREATE TABLE IF NOT EXISTS npcs (
                        npc_id INTEGER PRIMARY KEY AUTOINCREMENT,
                        name TEXT NOT NULL,
                        role_title TEXT NOT NULL,
                        greeting_dialogue TEXT NOT NULL,
                        associated_quest_id INTEGER NOT NULL,
                        FOREIGN KEY (associated_quest_id) REFERENCES quest_templates(quest_id)
                    );";
                    
                string createCharactersTable = @"
                    CREATE TABLE IF NOT EXISTS characters (
                        character_id INTEGER PRIMARY KEY AUTOINCREMENT,
                        name TEXT NOT NULL,
                        class_type TEXT NOT NULL CHECK(class_type IN ('Warrior', 'Mage')),
                        level INTEGER NOT NULL DEFAULT 1,
                        experience INTEGER NOT NULL DEFAULT 0,
                        current_health INTEGER NOT NULL,
                        max_health INTEGER NOT NULL,
                        current_mana INTEGER NOT NULL DEFAULT 0,
                        max_mana INTEGER NOT NULL DEFAULT 0,
                        gold INTEGER NOT NULL DEFAULT 100,
                        active_story_node INTEGER NOT NULL DEFAULT 1,
                        FOREIGN KEY (active_story_node) REFERENCES quest_templates(quest_id)
                    );";

                string createInventoryTable = @"
                    CREATE TABLE IF NOT EXISTS inventory_items (
                        item_id INTEGER PRIMARY KEY AUTOINCREMENT,
                        character_id INTEGER NOT NULL,
                        item_name TEXT NOT NULL,
                        item_description TEXT NOT NULL DEFAULT '',
                        type_category TEXT NOT NULL CHECK(type_category IN ('Weapon', 'Armor', 'Consumable')),
                        item_value INTEGER NOT NULL DEFAULT 0,
                        stat_modifier INTEGER NOT NULL DEFAULT 0,
                        rarity_tier TEXT NOT NULL DEFAULT 'Common',
                        equipment_slot TEXT NOT NULL DEFAULT 'None',
                        is_equipped INTEGER NOT NULL DEFAULT 0 CHECK(is_equipped IN (0, 1)),
                        FOREIGN KEY (character_id) REFERENCES characters(character_id) ON DELETE CASCADE
                    );";

                string createEnemiesTable = @"
                    CREATE TABLE IF NOT EXISTS enemy_templates (
                        enemy_template_id INTEGER PRIMARY KEY AUTOINCREMENT,
                        name TEXT NOT NULL,
                        base_health INTEGER NOT NULL,
                        base_attack INTEGER NOT NULL,
                        enemy_type TEXT NOT NULL CHECK(enemy_type IN ('Minion', 'Boss')),
                        associated_quest_id INTEGER,
                        FOREIGN KEY (associated_quest_id) REFERENCES quest_templates(quest_id)
                    );";

                // Execute Schema Implementations
                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = createQuestsTable;
                    command.ExecuteNonQuery();
                }
                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = createNPCsTable; 
                    command.ExecuteNonQuery();
                }
                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = createCharactersTable;
                    command.ExecuteNonQuery();
                }
                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = createInventoryTable;
                    command.ExecuteNonQuery();
                }
                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = createEnemiesTable; 
                    command.ExecuteNonQuery();
                }

                // additional Schema migration catch rules 
                EnsureColumnExists(connection, transaction, "enemy_templates", "xp_reward", "INTEGER NOT NULL DEFAULT 10");
                EnsureColumnExists(connection, transaction, "enemy_templates", "gold_reward", "INTEGER NOT NULL DEFAULT 5");
                EnsureColumnExists(connection, transaction, "inventory_items", "item_description", "TEXT NOT NULL DEFAULT ''");
                EnsureColumnExists(connection, transaction, "inventory_items", "item_value", "INTEGER NOT NULL DEFAULT 0");
                EnsureColumnExists(connection, transaction, "inventory_items", "equipment_slot", "TEXT NOT NULL DEFAULT 'None'");

                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = "DELETE FROM enemy_templates WHERE associated_quest_id IN (1, 2, 3, 4);";
                    command.ExecuteNonQuery();
                }

                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = "DELETE FROM npcs WHERE associated_quest_id IN (1, 2, 3, 4);";
                    command.ExecuteNonQuery();
                }

                string seedQuests = @"
                    INSERT OR REPLACE INTO quest_templates (quest_id, title, narrative_intro, target_enemy_type, is_final_node) VALUES 
                    (1, 'The First Descent', 'You wake in Oakhaven at the Inn Keeper''s bedside, and the town above the Maw remembers the reversal of life.', 'Minion', 0),
                    (2, 'The Emberward Depths', 'The second layer of the dungeon burns with old judgment and restless echoes.', 'Minion', 0),
                    (3, 'The Hollow Cathedral', 'Deeper still, a broken cathedral echoes with the prayers of things that should not live.', 'Minion', 0),
                    (4, 'The First King Awaits', 'At the root of all things sits the origin of the world: Alhuezo, the First King.', 'Boss', 1);";

                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = seedQuests;
                    command.ExecuteNonQuery();
                }

                string seedEnemies = @"
                    INSERT INTO enemy_templates (name, base_health, base_attack, enemy_type, associated_quest_id, xp_reward, gold_reward) VALUES 
                    ('Skeletal Guard', 35, 8, 'Minion', 1, 15, 10),
                    ('Fire Imp', 55, 14, 'Minion', 2, 30, 25),
                    ('Wraith Knight', 80, 18, 'Minion', 3, 55, 35),
                    ('Alhuezo, the First King', 999, 80, 'Boss', 4, 500, 250);";
                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = seedEnemies;
                    command.ExecuteNonQuery();
                }

                string seedNPCs = @"
                    INSERT INTO npcs (name, role_title, greeting_dialogue, associated_quest_id) VALUES 
                    ('Maren', 'Inn Keeper', 'You slept through the first bell, but the road below the town is already open. Drink, rest, and then descend when you are ready.', 1),
                    ('Eldrin', 'Wandering Scout', 'The fire below does not burn rock. It burns memory. Keep your blade close.', 2),
                    ('Sera', 'Cathedral Scribe', 'The hollow bells still ring. When they do, the next descent begins.', 3);";

                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = seedNPCs;
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
                Console.WriteLine("[DATA ENGINE] SQLite setup initialized successfully with adaptive schema verification checks.");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"[CRITICAL ERROR] Database initialization failed: {ex.Message}");
                throw;
            }
        }

        //safety helper
        private void EnsureColumnExists(SqliteConnection conn, SqliteTransaction trans, string tableName, string columnName, string columnDefinition)
        {
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            using (var command = conn.CreateCommand())
            {
                command.Transaction = trans;
                command.CommandText = $"PRAGMA table_info({tableName});";
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    string? existingColumn = reader["name"]?.ToString();
                    if (existingColumn != null)
                    {
                        columns.Add(existingColumn);
                    }
                }
            }

            if (!columns.Contains(columnName))
            {
                using (var command = conn.CreateCommand())
                {
                    command.Transaction = trans;
                    command.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition};";
                    command.ExecuteNonQuery();
                }
                Console.WriteLine($"[DATA ENGINE] Automatically migrated structural layout: Added missing column '{columnName}' to table '{tableName}'.");
            }
        }
    }
}
