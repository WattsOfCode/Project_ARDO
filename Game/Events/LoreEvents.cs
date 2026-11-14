/**
 * Name: John Deardorff
 * Date: 11/14/26
 * Project ARDO
 *
 * Class description:
 * `LoreEvents.cs` holds the journals left behind in the dungeon's empty rooms.
    Each floor has its own page, written by someone who came down before the
    hero and did not climb back. The story floors hint at what lives below; the
    deeper roads hint at Alhuezo, the keys and the door to his throne.
 */
using System;
using System.Collections.Generic;
using ARDO.Core.Abstracts;
using ARDO.Core.Enums;

namespace ARDO.Game.Events
{
    public static class LoreEvents
    {
        public const string JournalTitle = "A FORGOTTEN JOURNAL";

        // Floor id to the page found there.
        public static readonly IReadOnlyDictionary<int, string> Pages = new Dictionary<int, string>
        {
            [1] = "Day nine below. The goblins took Harl's boots, then they took Harl. The others say a king sits deeper down, " +
                  "fat on everything that falls from above. I used to think I'd see the bottom. I don't think anyone does.",
            [2] = "The fire down here doesn't burn the stone. It burns what you remember. This morning I couldn't find my " +
                  "daughter's face. I'm writing her name here so the fire can't have that too: Wren. Wren. Wren.",
            [3] = "They built this place to pray to him, the one who made the wheel. Every hymn on these walls runs backward, " +
                  "end to start. I counted the bells until they stopped. I'm still counting.",
            [FloorTiers.DeepFloorBase + 1] = "Whoever reaches the bottom comes back up. Not changed. Older, behind the eyes. I watched the same hero " +
                  "walk past my hiding place three times. The goblins don't even chase them anymore.",
            [FloorTiers.DeepFloorBase + 2] = "The king keeps a ledger of everything his court has stolen. On the last page, in a hand that isn't a " +
                  "goblin's: three keys, three floors, one door. Paid in full. I tore the page out. It didn't help."
        };

        public static bool HasPage(int floorId) => Pages.ContainsKey(floorId);

        // A journal lying in an empty room on this floor.
        public static DungeonEvent Journal(int floorId)
        {
            string page = Pages[floorId];
            return new DungeonEvent(
                JournalTitle,
                new[]
                {
                    "On the floor lies a journal, swollen with damp. Most of it has rotted, but the last page can still be read."
                },
                new[]
                {
                    new EventChoice("Read the last page", hero =>
                    {
                        int xp = 10 * hero.FloorTier;
                        var outcome = new EventOutcome()
                            .Say($"\"{page}\"", MessageTone.Dim)
                            .Say($"You close the journal. +{xp} XP.", MessageTone.Gold);
                        if (hero.GainExperience(xp))
                            outcome.Say($"You reach level {hero.Level}!", MessageTone.Gold);
                        return outcome;
                    }),
                    new EventChoice("Leave it", _ =>
                        new EventOutcome().Say("You leave the journal where it fell.", MessageTone.Dim))
                });
        }
    }
}
