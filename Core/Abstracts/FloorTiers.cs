/**
 * Name: John Deardorff
 * Date: 11/14/26
 * Project ARDO
 *
 * Class description:
 * `FloorTiers.cs` says how strong a floor is. Story floors 1 to 4 are their own
    tier. The deeper travel floors that open on later turns of the wheel have ids
    above `DeepFloorBase`, and all count as the deepest tier for loot, damage
    ranges and burns.
 */
using System;

namespace ARDO.Core.Abstracts
{
    public static class FloorTiers
    {
        // Travel floors have ids above this, so they never collide with the story's quest ids.
        public const int DeepFloorBase = 100;

        public const int DeepestTier = 4;

        // The throne quest: where a finished hero stands in the story.
        public const int FinalStoryFloor = 4;

        public static bool IsDeep(int floorId) => floorId > DeepFloorBase;

        public static int Of(int floorId) => IsDeep(floorId) ? DeepestTier : Math.Max(1, floorId);
    }
}
