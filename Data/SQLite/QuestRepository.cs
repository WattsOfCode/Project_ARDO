/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `QuestRepository.cs` is a concrete SQLite repository that reads quest and NPC
    data from the database. Encounter groups are built by `EncounterTable`.
 */
using System;
using Microsoft.Data.Sqlite;

namespace ARDO.Data.SQLite
{
    public class QuestRepository
    {
        private readonly DatabaseConfig _config;

        public sealed record QuestSummary(string Title, string NarrativeIntro, string TargetEnemyType, bool IsFinalNode);

        public QuestRepository(DatabaseConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        // Queries the narrative table block to pull the text string context for figureheads.
        public (string Name, string Role, string Dialogue)? GetNpcForQuest(int questId)
        {
            using var connection = _config.CreateConnection();
            const string query = "SELECT name, role_title, greeting_dialogue FROM npcs WHERE associated_quest_id = @questId LIMIT 1;";

            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@questId", questId);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return (
                    reader["name"]?.ToString() ?? "Mysterious Stranger",
                    reader["role_title"]?.ToString() ?? "Traveler",
                    reader["greeting_dialogue"]?.ToString() ?? "..."
                );
            }
            return null;
        }

        public QuestSummary? GetQuestSummary(int questId)
        {
            using var connection = _config.CreateConnection();
            const string query = @"
                SELECT title, narrative_intro, target_enemy_type, is_final_node
                FROM quest_templates
                WHERE quest_id = @questId
                LIMIT 1;";

            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@questId", questId);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new QuestSummary(
                    reader["title"]?.ToString() ?? "Unknown Depth",
                    reader["narrative_intro"]?.ToString() ?? "...",
                    reader["target_enemy_type"]?.ToString() ?? "Minion",
                    Convert.ToInt32(reader["is_final_node"]) == 1
                );
            }

            return null;
        }
    }
}
