/**
 * Name: John Deardorff
 * Date: 11/12/26
 * Project ARDO
 *
 * Class description:
 * `WheelEvents.cs` is a static catalogue of the events that only a finished hero
    meets: one hidden key of the wheel on each of the first three floors, and the
    sealed door to the throne that the keys open (or a rogue's picks).
 */
using System;
using System.Collections.Generic;
using ARDO.Core.Abstracts;
using ARDO.Core.Enums;

namespace ARDO.Game.Events
{
    public static class WheelEvents
    {
        public static readonly string[] KeyNames = { "Bone Key", "Ember Key", "Cathedral Key" };

        // The hidden key for a floor (1 to 3).
        public static DungeonEvent KeyFor(int floor)
        {
            return floor switch
            {
                1 => BoneKey(),
                2 => EmberKey(),
                _ => CathedralKey()
            };
        }

        private static EventOutcome GiveKey(Hero hero, int floor, EventOutcome outcome)
        {
            hero.WheelKeys |= 1 << (floor - 1);
            outcome.Say($"You take the {KeyNames[floor - 1]}. Keys of the wheel: {hero.WheelKeyCount} of 3.", MessageTone.Gold);
            if (hero.HasAllWheelKeys)
            {
                outcome.Say("All three keys hum together at your belt. Somewhere far below, a lock is waiting.", MessageTone.Gold);
            }

            return outcome;
        }

        private static DungeonEvent BoneKey()
        {
            return new DungeonEvent(
                "A KEY IN THE BONES",
                new[]
                {
                    "Behind a fallen slab, a skeleton sits with its back to the wall.",
                    "Its fingers are closed around a key ringed in bone. It hums, faintly, like the shrines do."
                },
                new[]
                {
                    new EventChoice("Pry the key loose", hero =>
                    {
                        var outcome = GiveKey(hero, 1, new EventOutcome().Say("The finger bones crack one by one."));
                        if (Random.Shared.Next(100) < 35)
                        {
                            outcome.StartsAmbush = true;
                            outcome.Say("Somewhere in the dark, the rest of the crypt stands up.", MessageTone.Warning);
                        }

                        return outcome;
                    }),
                    new EventChoice("Leave the dead their key", _ =>
                        new EventOutcome().Say("You leave it in its keeper's hand. For now.", MessageTone.Dim))
                });
        }

        private static DungeonEvent EmberKey()
        {
            return new DungeonEvent(
                "A KEY IN THE COALS",
                new[]
                {
                    "A brazier still burns in a forgotten guard post.",
                    "Deep in the coals, a key of scorched brass glows red."
                },
                new[]
                {
                    new EventChoice("Reach into the coals", hero =>
                    {
                        int lost = DungeonEvents.LoseHealth(hero, hero.MaxHealth * 15 / 100);
                        return GiveKey(hero, 2, new EventOutcome().Say($"The heat bites to the bone. You lose {lost} HP.", MessageTone.Damage));
                    }),
                    new EventChoice("Tip the brazier over and wait for it to cool", hero =>
                    {
                        if (Random.Shared.Next(100) < 50)
                            return GiveKey(hero, 2, new EventOutcome().Say("The coals hiss and dim. The key cools in the ash."));

                        return new EventOutcome().Say("The key skitters through a grate in the floor and is gone. Perhaps it will turn up again.", MessageTone.Dim);
                    }),
                    new EventChoice("Leave it burning", _ =>
                        new EventOutcome().Say("You leave the key to its fire.", MessageTone.Dim))
                });
        }

        private static DungeonEvent CathedralKey()
        {
            return new DungeonEvent(
                "A KEY ON THE ALTAR",
                new[]
                {
                    "On the Hollow Cathedral's high altar, under a prayer nobody finished,",
                    "lies a key of tarnished silver."
                },
                new[]
                {
                    new EventChoice("Take it from the altar", hero =>
                    {
                        var outcome = GiveKey(hero, 3, new EventOutcome().Say("The silver is warm, as if someone had only just set it down."));
                        if (Random.Shared.Next(100) < 50)
                        {
                            outcome.StartsAmbush = true;
                            outcome.Say("The cathedral's bells ring once. Its keepers come for what you took.", MessageTone.Warning);
                        }

                        return outcome;
                    }),
                    new EventChoice("Finish the prayer first", hero =>
                    {
                        if (Random.Shared.Next(100) < 50)
                        {
                            int healed = hero.Heal(hero.MaxHealth / 5);
                            return GiveKey(hero, 3, new EventOutcome().Say($"The words come to you as if you had always known them. You recover {healed} HP.", MessageTone.Heal));
                        }

                        return new EventOutcome().Say("The words slip away from you. When you look up, the key is gone from the altar.", MessageTone.Dim);
                    }),
                    new EventChoice("Leave it on the altar", _ =>
                        new EventOutcome().Say("Some things are better left where they were laid.", MessageTone.Dim))
                });
        }

        // The door to the throne for a finished hero: three keyholes, and a fourth only a rogue would notice.
        public static DungeonEvent SealedDoor(Hero hero)
        {
            var description = new List<string>
            {
                "The last stair below Oakhaven ends at a door of black iron.",
                "Three keyholes are set in it: one ringed in bone, one in scorched brass, one in cathedral silver.",
                $"You carry {hero.WheelKeyCount} of the 3 keys."
            };

            var choices = new List<EventChoice>();

            if (hero.HasAllWheelKeys)
            {
                choices.Add(new EventChoice("Turn the three keys", _ =>
                    new EventOutcome { ThroneFight = WheelEnding.TurnedTheWheel }
                        .Say("Bone, then ember, then silver. The door swings inward without a sound.")
                        .Say("The throne is not empty.", MessageTone.Warning)));
            }

            if (hero.CanPickLocks)
            {
                description.Add("Low on the frame, half hidden by soot, there is a fourth lock: small, plain, a servant's door.");
                choices.Add(new EventChoice("Pick the servant's lock", _ =>
                    new EventOutcome { ThroneFight = WheelEnding.SteppedOff }
                        .Say("It gives in under a minute. The little door opens onto a passage that does not lead to the throne.")
                        .Say("Something is already standing in it, waiting for you. It is wearing your face.", MessageTone.Warning)));
            }

            if (choices.Count == 0)
            {
                description.Add("The door will not move for you. Not yet.");
            }

            choices.Add(new EventChoice("Turn back to Oakhaven", _ =>
                new EventOutcome { GoHome = true }.Say("You climb back up the long stair.", MessageTone.Dim)));

            return new DungeonEvent("THE SEALED THRONE", description, choices);
        }
    }
}
