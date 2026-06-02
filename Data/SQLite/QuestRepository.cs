using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using ARDO.Core.Abstracts;
using ARDO.Core.Enums;
using ARDO.Core.Factories;
using ARDO.Game.Enemies;
using ARDO.Data.SQLite;

namespace ARDO.Data.SQLite
{
    public class QuestRepository
    {
        private readonly DatabaseConfig _config;
        private static readonly Random _random = new Random();

        public sealed record QuestSummary(string Title, string NarrativeIntro, string TargetEnemyType, bool IsFinalNode);

        public QuestRepository(DatabaseConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        // Queries the narrative table block to pull the text string context for figureheads.
        public (string Name, string Role, string Dialogue)? GetNpcForQuest(int questId)
        {
            using var connection = _config.CreateConnection();
            string query = "SELECT name, role_title, greeting_dialogue FROM npcs WHERE associated_quest_id = @questId LIMIT 1;";
            
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
            string query = @"
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

        // Instantiates a unique concrete Minion or Boss based on the story node context.
        public Enemy? GetEnemyForQuest(int questId)
        {
            using var connection = _config.CreateConnection();
            string query = "SELECT * FROM enemy_templates WHERE associated_quest_id = @questId LIMIT 1;";

            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@questId", questId);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                string name = reader["name"]?.ToString() ?? "Feral Beast";
                int hp = Convert.ToInt32(reader["base_health"]);
                int attack = Convert.ToInt32(reader["base_attack"]);
                string type = reader["enemy_type"]?.ToString() ?? "Minion";
                int templateId = Convert.ToInt32(reader["enemy_template_id"]);

                Enemy enemy = type.Equals("Boss", StringComparison.OrdinalIgnoreCase)
                    ? new Boss(name, hp, attack)
                    : EnemyFactory.Create(GetEnemyTypeForQuest(questId));

                enemy.EnemyTemplateId = templateId;
                return enemy;
            }
            return null;
        }

        public IReadOnlyList<Enemy> GetEncounterGroupForQuest(int questId)
        {
            return questId switch
            {
                1 => BuildRandomGroup(
                    new[]
                    {
                        new Func<List<Enemy>>(() => new List<Enemy> { new Goblin() }),
                        new Func<List<Enemy>>(() => new List<Enemy> { new Goblin(), new Goblin() }),
                        new Func<List<Enemy>>(() => new List<Enemy> { new Skeleton(), new Skeleton(), new Skeleton() }),
                        new Func<List<Enemy>>(() => new List<Enemy> { new Skeleton(), new Goblin(), new Goblin() })
                    }),
                2 => BuildRandomGroup(
                    new[]
                    {
                        new Func<List<Enemy>>(() => new List<Enemy> { new FireImp() }),
                        new Func<List<Enemy>>(() => new List<Enemy> { new FireImp(), new FireImp() }),
                        new Func<List<Enemy>>(() => new List<Enemy> { new Goblin(), new FireImp(), new Goblin() }),
                        new Func<List<Enemy>>(() => new List<Enemy> { new WraithHerald(), new FireImp() })
                    }),
                3 => BuildRandomGroup(
                    new[]
                    {
                        new Func<List<Enemy>>(() => new List<Enemy> { new HollowCaptain() }),
                        new Func<List<Enemy>>(() => new List<Enemy> { new Skeleton(), new Skeleton(), new Skeleton() }),
                        new Func<List<Enemy>>(() => new List<Enemy> { new HollowCaptain(), new Skeleton(), new Skeleton() }),
                        new Func<List<Enemy>>(() => new List<Enemy> { new Goblin(), new FireImp(), new Skeleton() })
                    }),
                4 => new List<Enemy> { new Boss("Alhuezo, the First King", 999, 80) },
                _ => new List<Enemy> { GetEnemyForQuest(questId) ?? new Goblin() }
            };
        }

        private static EnemyType GetEnemyTypeForQuest(int questId)
        {
            return questId switch
            {
                1 => EnemyType.Goblin,
                2 => EnemyType.FireImp,
                3 => EnemyType.HollowCaptain,
                4 => EnemyType.Alhuezo,
                _ => EnemyType.Goblin
            };
        }

        private static IReadOnlyList<Enemy> BuildRandomGroup(Func<List<Enemy>>[] patterns)
        {
            int index = _random.Next(patterns.Length);
            return patterns[index]();
        }
    }
}
