/**
 * Name: John Deardorff
 * Date: 11/12/26
 * Project ARDO
 *
 * Class description:
 * `PassageEvents.cs` is a static catalogue of the dungeon's forks and rooms.
    Between fights the passage splits left and right, each way with an honest
    hint. Most lead on to the next fight; some to a door, and behind a door a
    room that may hold a treasure chest.
 */
using System;
using System.Collections.Generic;
using ARDO.Core.Abstracts;
using ARDO.Core.Enums;
using ARDO.Game.Inventory;

namespace ARDO.Game.Events
{
    public static class PassageEvents
    {
        // Out of 100, for each side of a fork: a door, one of the regular events, or (the rest) a fight.
        private const int DoorChancePercent = 20;
        private const int EventChancePercent = 20;

        // Behind a door, out of 100: a chest, a chest with guards, or (the rest) an empty room.
        private const int ChestChancePercent = 45;
        private const int GuardedChestChancePercent = 25;

        private const int ChestTrapChancePercent = 25;

        // An empty room often has a journal left in it.
        private const int JournalChancePercent = 60;

        private enum Way { Fight, Door, Event }

        private static readonly string[] FightHints =
        {
            "something shuffles and scrapes in the dark.",
            "you hear breathing that isn't yours.",
            "fresh claw marks score the walls.",
            "bones crunch under something heavy."
        };

        private static readonly string[] DoorHints =
        {
            "torchlight catches an iron-banded door.",
            "the passage ends at a heavy, carved door.",
            "a doorframe, old and crooked, waits at the end."
        };

        private static readonly string[] EventHints =
        {
            "a faint light flickers somewhere ahead.",
            "you smell smoke, and something else.",
            "the air turns strangely still."
        };

        // Where the run goes after a fight: left or right, each side rolled on its own.
        public static DungeonEvent Fork()
        {
            Way left = RollWay();
            Way right = RollWay();

            return new DungeonEvent(
                "A FORK IN THE PASSAGE",
                new[] { "The passage splits in two." },
                new[]
                {
                    new EventChoice($"Left: {Hint(left)}", _ => Follow(left, "left")),
                    new EventChoice($"Right: {Hint(right)}", _ => Follow(right, "right"))
                });
        }

        private static Way RollWay()
        {
            int roll = Random.Shared.Next(100);
            if (roll < DoorChancePercent)
                return Way.Door;

            return roll < DoorChancePercent + EventChancePercent ? Way.Event : Way.Fight;
        }

        private static string Hint(Way way) => way switch
        {
            Way.Door => Pick(DoorHints),
            Way.Event => Pick(EventHints),
            _ => Pick(FightHints)
        };

        private static EventOutcome Follow(Way way, string side)
        {
            var outcome = new EventOutcome().Say($"You take the {side} passage.", MessageTone.Dim);

            switch (way)
            {
                case Way.Door:
                    outcome.RoomInsteadOfFight = true;
                    outcome.FollowUp = Door();
                    break;

                case Way.Event:
                    outcome.RoomInsteadOfFight = true;
                    outcome.FollowUp = DungeonEvents.RollRandom();
                    break;

                default:
                    outcome.FightNow = true;
                    break;
            }

            return outcome;
        }

        private static DungeonEvent Door()
        {
            return new DungeonEvent(
                "A DOOR",
                new[] { "An old door stands closed across the passage. No light shows under it." },
                new[]
                {
                    new EventChoice("Open the door", hero => OpenDoor(hero)),
                    new EventChoice("Leave it shut and go around", _ =>
                        new EventOutcome().Say("Some doors are shut for a reason. You find another way.", MessageTone.Dim))
                });
        }

        private static EventOutcome OpenDoor(Hero hero)
        {
            var outcome = new EventOutcome();
            int roll = Random.Shared.Next(100);

            if (roll < ChestChancePercent)
            {
                outcome.Say("The door groans open on a small, dusty room. In the middle of it sits a chest.");
                outcome.FollowUp = Chest(hero);
                return outcome;
            }

            if (roll < ChestChancePercent + GuardedChestChancePercent)
            {
                outcome.Say("There is a chest in the room, and things standing guard over it. They turn as one.", MessageTone.Warning);
                outcome.StartsAmbush = true;
                outcome.FollowUp = Chest(hero, guarded: true);
                return outcome;
            }

            int healed = hero.Heal(hero.MaxHealth / 8);
            if (LoreEvents.HasPage(hero.ActiveStoryNode) && Random.Shared.Next(100) < JournalChancePercent)
                outcome.FollowUp = LoreEvents.Journal(hero.ActiveStoryNode);

            return outcome.Say(healed > 0
                ? $"An empty room, dry and quiet. You catch your breath and recover {healed} HP."
                : "An empty room, dry and quiet. You catch your breath.", MessageTone.Heal);
        }

        // A chest: often worth it, sometimes trapped. A rogue can pick it without setting anything off.
        public static DungeonEvent Chest(Hero hero, bool guarded = false)
        {
            var description = new List<string>
            {
                guarded
                    ? "With its keepers down, the chest is yours to open."
                    : "An iron-bound chest, heavy, its lock old but whole."
            };

            var choices = new List<EventChoice>();

            if (hero.CanPickLocks)
            {
                choices.Add(new EventChoice("Pick the lock and feel for traps", h => Loot(h, trapped: false,
                    new EventOutcome().Say("Your picks find the trap's catch before the lock. Both give.", MessageTone.Dim))));
            }

            choices.Add(new EventChoice("Open it", h =>
            {
                bool trapped = Random.Shared.Next(100) < ChestTrapChancePercent;
                return Loot(h, trapped, new EventOutcome());
            }));

            choices.Add(new EventChoice("Leave it", _ =>
                new EventOutcome().Say("You leave the chest to whoever comes next.", MessageTone.Dim)));

            return new DungeonEvent("A TREASURE CHEST", description, choices);
        }

        private static EventOutcome Loot(Hero hero, bool trapped, EventOutcome outcome)
        {
            if (trapped)
            {
                int lost = DungeonEvents.LoseHealth(hero, hero.MaxHealth * 12 / 100);
                outcome.Say($"A needle snaps out of the lock. You lose {lost} HP, but the lid comes up.", MessageTone.Damage);
            }

            int floor = hero.FloorTier;
            int gold = hero.WithGoldBonus(Random.Shared.Next(15, 31) + floor * 10);
            hero.Gold += gold;
            outcome.Say($"Coins spill out under your hands: +{gold} gold.", MessageTone.Gold);
            outcome.Reward = LootGenerator.RollGear(floor, RarityType.Uncommon);
            return outcome;
        }

        private static string Pick(string[] options) => options[Random.Shared.Next(options.Length)];
    }
}
