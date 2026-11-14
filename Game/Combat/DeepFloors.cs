/**
 * Name: John Deardorff
 * Date: 11/14/26
 * Project ARDO
 *
 * Class description:
 * `DeepFloors.cs` lists the travel floors that open as the wheel turns. Each turn
    opens two on the road to one boss, and their scenery hints at what waits at the
    end. They have no guardian: a run is fights, forks and events. They are built
    for heroes who have finished the story, so their enemies are veterans.
 */
using System.Collections.Generic;
using System.Linq;
using ARDO.Core.Abstracts;
using ARDO.Core.Enums;

namespace ARDO.Game.Combat
{
    public sealed record DeepFloor(
        int Id,
        string Label,
        string Title,
        int OpensAtTurn,
        int LeadsTo,
        string LeadsToName,
        int Fights,
        IReadOnlyList<string> Intro,
        EnemyType[][] Groups);

    public static class DeepFloors
    {
        public static readonly IReadOnlyList<DeepFloor> All = new[]
        {
            new DeepFloor(FloorTiers.DeepFloorBase + 1, "1a", "The Robbers' Road", 1, 1, "Grisk",
                Fights: 3,
                Intro: new[]
                {
                    "Below the old mine the tunnels narrow into a smugglers' road, choked with stolen carts and broken toll gates.",
                    "Goblin chalk marks on every wall point deeper, toward the king."
                },
                Groups: new[]
                {
                    new[] { EnemyType.GoblinCutpurse, EnemyType.GoblinCutpurse },
                    new[] { EnemyType.GoblinBrute },
                    new[] { EnemyType.GoblinBrute, EnemyType.GoblinCutpurse },
                    new[] { EnemyType.Skeleton, EnemyType.Skeleton, EnemyType.GoblinCutpurse }
                }),

            new DeepFloor(FloorTiers.DeepFloorBase + 2, "1b", "The Banner Halls", 1, 1, "Grisk",
                Fights: 3,
                Intro: new[]
                {
                    "The road opens into the barracks of the king's court. Stolen banners hang from every beam, " +
                    "and tribute is piled in the corners: boots, pots, a church bell.",
                    "Somewhere ahead, someone is laughing on a throne of crates."
                },
                Groups: new[]
                {
                    new[] { EnemyType.GoblinBrute, EnemyType.GoblinShaman },
                    new[] { EnemyType.GoblinCutpurse, EnemyType.GoblinCutpurse, EnemyType.GoblinShaman },
                    new[] { EnemyType.GoblinBrute, EnemyType.GoblinBrute },
                    new[] { EnemyType.GoblinBrute, EnemyType.GoblinCutpurse, EnemyType.GoblinShaman }
                })
        };

        public static DeepFloor? Get(int id) => All.FirstOrDefault(floor => floor.Id == id);

        public static DeepFloor? ByLabel(string label) => All.FirstOrDefault(floor => floor.Label == label);

        // Only a hero who has finished the story walks them, and only once the wheel has turned far enough.
        public static IEnumerable<DeepFloor> OpenFor(Hero hero) =>
            hero.StoryComplete ? All.Where(floor => hero.WheelTurn >= floor.OpensAtTurn) : Enumerable.Empty<DeepFloor>();

        public static IEnumerable<DeepFloor> OpenedAt(int turn) => All.Where(floor => floor.OpensAtTurn == turn);

        // The floors on the road to a story floor, in the order they are walked.
        public static IEnumerable<DeepFloor> Before(Hero hero, int storyFloor) => OpenFor(hero).Where(floor => floor.LeadsTo == storyFloor);
    }
}
