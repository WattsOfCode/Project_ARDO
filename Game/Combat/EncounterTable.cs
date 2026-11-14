/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `EncounterTable.cs` is a static lookup that says which enemy groups can appear
    on each quest node and how many fights a dungeon run has. Enemies are built
    through `EnemyFactory`.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using ARDO.Core.Abstracts;
using ARDO.Core.Enums;
using ARDO.Core.Factories;

namespace ARDO.Game.Combat
{
    public static class EncounterTable
    {
        public const int FinalQuestId = 4;

        // For each quest node, the groups a fight can roll. One group is picked at random.
        private static readonly Dictionary<int, EnemyType[][]> Groups = new()
        {
            [1] = new[]
            {
                new[] { EnemyType.Goblin },
                new[] { EnemyType.Goblin, EnemyType.Goblin },
                new[] { EnemyType.Skeleton, EnemyType.Skeleton, EnemyType.Skeleton },
                new[] { EnemyType.Skeleton, EnemyType.Goblin, EnemyType.Goblin }
            },
            [2] = new[]
            {
                new[] { EnemyType.FireImp },
                new[] { EnemyType.FireImp, EnemyType.FireImp },
                new[] { EnemyType.Goblin, EnemyType.FireImp, EnemyType.Goblin },
                new[] { EnemyType.WraithHerald, EnemyType.FireImp }
            },
            [3] = new[]
            {
                new[] { EnemyType.HollowCaptain },
                new[] { EnemyType.Skeleton, EnemyType.Skeleton, EnemyType.Skeleton },
                new[] { EnemyType.HollowCaptain, EnemyType.Skeleton, EnemyType.Skeleton },
                new[] { EnemyType.Goblin, EnemyType.FireImp, EnemyType.Skeleton }
            },
            [FinalQuestId] = new[]
            {
                new[] { EnemyType.Alhuezo }
            }
        };

        public static IReadOnlyList<Enemy> RollGroup(int questId)
        {
            EnemyType[][]? patterns = DeepFloors.Get(questId)?.Groups;
            if (patterns == null && !Groups.TryGetValue(questId, out patterns))
            {
                patterns = Groups[1];
            }

            EnemyType[] pattern = patterns[Random.Shared.Next(patterns.Length)];
            return pattern.Select(EnemyFactory.Create).ToList();
        }

        public static int FightsPerRun(int questId)
        {
            if (DeepFloors.Get(questId) is DeepFloor deep)
                return deep.Fights;

            return questId switch
            {
                1 => 2,
                2 => 3,
                3 => 3,
                FinalQuestId => 1,
                _ => 2
            };
        }
    }
}
