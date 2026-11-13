/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `DatabaseConfig.cs` is a concrete database bootstrapper that creates the
    SQLite schema and seeds default data. It exposes setup methods only,
    while CRUD support is handled by repository classes.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Data.Sqlite;

namespace ARDO.Data.SQLite
{
    public class DatabaseConfig
    {
        private const string DatabaseFileName = "ardo.db";

        private readonly string _connectionString;

        // Saves live in %LocalAppData%\ARDO so the game works from any folder without admin rights.
        public DatabaseConfig()
            : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ARDO", DatabaseFileName))
        {
        }

        public DatabaseConfig(string databasePath)
        {
            DatabasePath = databasePath ?? throw new ArgumentNullException(nameof(databasePath));
            _connectionString = new SqliteConnectionStringBuilder { DataSource = DatabasePath }.ToString();
        }

        public string DatabasePath { get; }

        public SqliteConnection CreateConnection()
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var pragmaCmd = connection.CreateCommand();
            pragmaCmd.CommandText = "PRAGMA foreign_keys = ON;";
            pragmaCmd.ExecuteNonQuery();

            return connection;
        }

        public void InitializeDatabase()
        {
            string? folder = Path.GetDirectoryName(DatabasePath);
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }

            MoveLegacyDatabase();

            using var connection = CreateConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                Execute(connection, transaction, @"
                    CREATE TABLE IF NOT EXISTS quest_templates (
                        quest_id INTEGER PRIMARY KEY,
                        title TEXT NOT NULL,
                        narrative_intro TEXT NOT NULL,
                        target_enemy_type TEXT NOT NULL DEFAULT 'Minion',
                        is_final_node INTEGER NOT NULL DEFAULT 0 CHECK(is_final_node IN (0, 1))
                    );");

                Execute(connection, transaction, @"
                    CREATE TABLE IF NOT EXISTS npcs (
                        npc_id INTEGER PRIMARY KEY AUTOINCREMENT,
                        name TEXT NOT NULL,
                        role_title TEXT NOT NULL,
                        greeting_dialogue TEXT NOT NULL,
                        associated_quest_id INTEGER NOT NULL,
                        FOREIGN KEY (associated_quest_id) REFERENCES quest_templates(quest_id)
                    );");

                Execute(connection, transaction, CharactersTableSql("characters"));

                Execute(connection, transaction, @"
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
                        item_effect TEXT NOT NULL DEFAULT 'None',
                        FOREIGN KEY (character_id) REFERENCES characters(character_id) ON DELETE CASCADE
                    );");

                // Columns added after the first release, for saves made by older versions.
                EnsureColumnExists(connection, transaction, "inventory_items", "item_description", "TEXT NOT NULL DEFAULT ''");
                EnsureColumnExists(connection, transaction, "inventory_items", "item_value", "INTEGER NOT NULL DEFAULT 0");
                EnsureColumnExists(connection, transaction, "inventory_items", "equipment_slot", "TEXT NOT NULL DEFAULT 'None'");
                EnsureColumnExists(connection, transaction, "inventory_items", "item_effect", "TEXT NOT NULL DEFAULT 'None'");
                EnsureColumnExists(connection, transaction, "characters", "story_complete", "INTEGER NOT NULL DEFAULT 0");
                EnsureColumnExists(connection, transaction, "characters", "wheel_keys", "INTEGER NOT NULL DEFAULT 0");
                EnsureColumnExists(connection, transaction, "characters", "wheel_ending", "INTEGER NOT NULL DEFAULT 0");
                EnsureColumnExists(connection, transaction, "characters", "perks", "TEXT NOT NULL DEFAULT ''");

                // Heroes from before perks get one to choose for every level they already earned.
                if (EnsureColumnExists(connection, transaction, "characters", "pending_perks", "INTEGER NOT NULL DEFAULT 0"))
                {
                    Execute(connection, transaction, "UPDATE characters SET pending_perks = MAX(0, level - 1);");
                }

                // Enemy stats live in the enemy classes; this table from older versions was never read.
                Execute(connection, transaction, "DROP TABLE IF EXISTS enemy_templates;");

                Execute(connection, transaction, @"
                    INSERT OR REPLACE INTO quest_templates (quest_id, title, narrative_intro, target_enemy_type, is_final_node) VALUES
                    (1, 'The First Descent', 'You wake in Oakhaven at the Inn Keeper''s bedside, and the town above the Maw remembers the reversal of life.', 'Minion', 0),
                    (2, 'The Emberward Depths', 'The second layer of the dungeon burns with old judgment and restless echoes.', 'Minion', 0),
                    (3, 'The Hollow Cathedral', 'Deeper still, a broken cathedral echoes with the prayers of things that should not live.', 'Minion', 0),
                    (4, 'The God-King Awaits', 'At the root of all things sits the origin of the world: Alhuezo, the God-King.', 'Boss', 1);");

                // NPC rows are fixed content: replace them on every launch so edits here reach old saves.
                Execute(connection, transaction, "DELETE FROM npcs;");
                Execute(connection, transaction, @"
                    INSERT INTO npcs (name, role_title, greeting_dialogue, associated_quest_id) VALUES
                    ('Maren', 'Inn Keeper', 'You slept through the first bell, but the road below the town is already open. Drink, rest, and then descend when you are ready.', 1),
                    ('Eldrin', 'Wandering Scout', 'The fire below does not burn rock. It burns memory. Keep your guard up.', 2),
                    ('Sera', 'Cathedral Scribe', 'The hollow bells still ring. When they do, the next descent begins.', 3),
                    ('Maren', 'Inn Keeper', 'The bells have gone quiet. Whatever sits on that throne has waited a long time for you. Rest first. Some roads only run one way.', 4);");

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Debug.WriteLine($"[ARDO][DB] Database initialization failed: {ex}");
                throw;
            }

            AllowNewClasses();
        }

        // The class list lives in a CHECK on the characters table.
        private static string CharactersTableSql(string tableName) => $@"
                    CREATE TABLE IF NOT EXISTS {tableName} (
                        character_id INTEGER PRIMARY KEY AUTOINCREMENT,
                        name TEXT NOT NULL,
                        class_type TEXT NOT NULL CHECK(class_type IN ('Warrior', 'Mage', 'Archer', 'Rogue')),
                        level INTEGER NOT NULL DEFAULT 1,
                        experience INTEGER NOT NULL DEFAULT 0,
                        current_health INTEGER NOT NULL,
                        max_health INTEGER NOT NULL,
                        current_mana INTEGER NOT NULL DEFAULT 0,
                        max_mana INTEGER NOT NULL DEFAULT 0,
                        gold INTEGER NOT NULL DEFAULT 100,
                        active_story_node INTEGER NOT NULL DEFAULT 1,
                        story_complete INTEGER NOT NULL DEFAULT 0,
                        wheel_keys INTEGER NOT NULL DEFAULT 0,
                        wheel_ending INTEGER NOT NULL DEFAULT 0,
                        perks TEXT NOT NULL DEFAULT '',
                        pending_perks INTEGER NOT NULL DEFAULT 0,
                        FOREIGN KEY (active_story_node) REFERENCES quest_templates(quest_id)
                    );";

        // Saves from before the Archer and Rogue only allow 'Warrior' and 'Mage'. SQLite can't
        // change a CHECK in place, so the table is rebuilt once with every hero copied across.
        // Foreign keys are off while it happens, so dropping the old table can't take the packs with it.
        private void AllowNewClasses()
        {
            using var connection = CreateConnection();

            using (var check = connection.CreateCommand())
            {
                check.CommandText = "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'characters';";
                string sql = check.ExecuteScalar()?.ToString() ?? string.Empty;
                if (sql.Contains("'Rogue'"))
                    return;
            }

            const string columns = "character_id, name, class_type, level, experience, current_health, max_health, " +
                                   "current_mana, max_mana, gold, active_story_node, story_complete, wheel_keys, wheel_ending, perks, pending_perks";

            using (var pragma = connection.CreateCommand())
            {
                pragma.CommandText = "PRAGMA foreign_keys = OFF;";
                pragma.ExecuteNonQuery();
            }

            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    Execute(connection, transaction, "DROP TABLE IF EXISTS characters_rebuild;");
                    Execute(connection, transaction, CharactersTableSql("characters_rebuild"));
                    Execute(connection, transaction, $"INSERT INTO characters_rebuild ({columns}) SELECT {columns} FROM characters;");
                    Execute(connection, transaction, "DROP TABLE characters;");
                    Execute(connection, transaction, "ALTER TABLE characters_rebuild RENAME TO characters;");
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Debug.WriteLine($"[ARDO][DB] Could not open the save to the new classes: {ex}");
                    throw;
                }
            }

            using (var pragma = connection.CreateCommand())
            {
                pragma.CommandText = "PRAGMA foreign_keys = ON;";
                pragma.ExecuteNonQuery();
            }
        }

        // Older builds kept ardo.db in whatever folder the game was started from. Bring it along once.
        private void MoveLegacyDatabase()
        {
            if (File.Exists(DatabasePath))
                return;

            foreach (string folder in new[] { AppContext.BaseDirectory, Environment.CurrentDirectory })
            {
                string legacyPath = Path.Combine(folder, DatabaseFileName);
                if (!File.Exists(legacyPath))
                    continue;

                try
                {
                    File.Copy(legacyPath, DatabasePath);
                    return;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ARDO][DB] Could not copy the old save file '{legacyPath}': {ex.Message}");
                }
            }
        }

        private static void Execute(SqliteConnection connection, SqliteTransaction transaction, string sql)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        // Returns true when the column was missing and has just been added.
        private static bool EnsureColumnExists(SqliteConnection conn, SqliteTransaction trans, string tableName, string columnName, string columnDefinition)
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

            if (columns.Contains(columnName))
                return false;

            Execute(conn, trans, $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition};");
            return true;
        }
    }
}
