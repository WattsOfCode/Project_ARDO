/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `DungeonEvents.cs` is a static catalogue of the events that can happen between
    fights in the dungeon, and the side passage offered once a floor is cleared.
 */
using System;
using System.Linq;
using ARDO.Core.Abstracts;
using ARDO.Core.Entities;
using ARDO.Core.Enums;
using ARDO.Game.Inventory;

namespace ARDO.Game.Events
{
    public static class DungeonEvents
    {
        private static readonly Func<DungeonEvent>[] RandomEvents =
        {
            CrackedShrine,
            WoundedTraveler,
            OldCampfire,
            LockedChest
        };

        public static DungeonEvent RollRandom()
        {
            return RandomEvents[Random.Shared.Next(RandomEvents.Length)]();
        }

        private static DungeonEvent CrackedShrine()
        {
            return new DungeonEvent(
                "A CRACKED SHRINE",
                new[]
                {
                    "A shrine to the Progenitor leans out of the wall, split down the middle.",
                    "Something still flickers in the bowl at its feet."
                },
                new[]
                {
                    new EventChoice("Kneel and pray", hero =>
                    {
                        var outcome = new EventOutcome();

                        if (Random.Shared.Next(100) < 60)
                        {
                            if (Random.Shared.Next(2) == 0)
                            {
                                hero.RunAttackBonus += 4;
                                return outcome.Say("Warmth floods your arms. +4 attack until you return to town.", MessageTone.Gold);
                            }

                            int healed = hero.Heal(hero.MaxHealth * 2 / 5);
                            return outcome.Say($"The flame mends you. You recover {healed} HP.", MessageTone.Heal);
                        }

                        int lost = LoseHealth(hero, hero.MaxHealth * 15 / 100);
                        return outcome.Say($"The crack widens and something drinks from you. You lose {lost} HP.", MessageTone.Damage);
                    }),
                    new EventChoice("Leave it alone", _ =>
                        new EventOutcome().Say("You step around the shrine. The flicker follows you a while, then fades.", MessageTone.Dim))
                });
        }

        private static DungeonEvent WoundedTraveler()
        {
            return new DungeonEvent(
                "A WOUNDED TRAVELER",
                new[]
                {
                    "A scout sits against the wall, pressing a rag to a deep cut.",
                    "\"Please,\" she says. \"Anything for the pain. I can pay.\""
                },
                new[]
                {
                    new EventChoice("Give her a healing potion", hero =>
                    {
                        var outcome = new EventOutcome();
                        Consumable? potion = hero.HeroInventory.GetItems()
                            .OfType<Consumable>()
                            .Where(item => !item.RestoresMana)
                            .OrderBy(item => item.HealAmount)
                            .FirstOrDefault();

                        if (potion == null)
                        {
                            return outcome.Say("You have no healing potion to give. You bind the wound as best you can, and she thanks you anyway.", MessageTone.Dim);
                        }

                        int floor = hero.ActiveStoryNode;
                        int gold = 25 + floor * 15;
                        int xp = 20 * floor;

                        outcome.ItemUsedUp = potion;
                        hero.Gold += gold;
                        outcome.Say($"She drinks the {potion.Name} and presses a heavy purse into your hand. +{gold} gold, +{xp} XP.", MessageTone.Gold);
                        if (hero.GainExperience(xp))
                        {
                            outcome.Say($"You reach level {hero.Level}!", MessageTone.Gold);
                        }

                        return outcome;
                    }),
                    new EventChoice("Take her pack", hero =>
                    {
                        var outcome = new EventOutcome();
                        int gold = 15 + hero.ActiveStoryNode * 10;
                        hero.Gold += gold;
                        outcome.Say($"You tear the pack from her shoulders. +{gold} gold.", MessageTone.Gold);

                        if (Random.Shared.Next(2) == 0)
                        {
                            hero.RunAttackBonus -= 2;
                            outcome.Say("She spits a curse after you. Your hands won't stop shaking: -2 attack until you return to town.", MessageTone.Damage);
                        }

                        return outcome;
                    }),
                    new EventChoice("Walk on", _ =>
                        new EventOutcome().Say("You leave her to the dark. Her breathing follows you down the tunnel.", MessageTone.Dim))
                });
        }

        private static DungeonEvent OldCampfire()
        {
            return new DungeonEvent(
                "AN OLD CAMPFIRE",
                new[]
                {
                    "Someone camped here not long ago. The embers are still warm.",
                    "Resting would help. Then again, whoever lit it might come back."
                },
                new[]
                {
                    new EventChoice("Rest by the fire", hero =>
                    {
                        var outcome = new EventOutcome();
                        int healed = hero.Heal(hero.MaxHealth * 3 / 10);
                        outcome.Say($"You doze by the embers and recover {healed} HP.", MessageTone.Heal);

                        if (Random.Shared.Next(100) < 35)
                        {
                            outcome.StartsAmbush = true;
                            outcome.Say("You wake to footsteps. The campers are back, and they are not friendly!", MessageTone.Warning);
                        }

                        return outcome;
                    }),
                    new EventChoice("Keep moving", _ =>
                        new EventOutcome().Say("You kick dirt over the embers and press on.", MessageTone.Dim))
                });
        }

        private static DungeonEvent LockedChest()
        {
            return new DungeonEvent(
                "A LOCKED CHEST",
                new[]
                {
                    "An iron-bound chest sits in an alcove, its lock rusted half through.",
                    "Scratches on the floor around it look a lot like a trap's trigger plate."
                },
                new[]
                {
                    new EventChoice("Force it open", hero =>
                    {
                        var outcome = new EventOutcome();

                        if (Random.Shared.Next(100) < 25)
                        {
                            int lost = LoseHealth(hero, hero.MaxHealth * 12 / 100);
                            return outcome.Say($"A blade springs from the lid! You lose {lost} HP, and the chest is empty.", MessageTone.Damage);
                        }

                        outcome.Reward = LootGenerator.RollGear(hero.ActiveStoryNode, RarityType.Uncommon);
                        return outcome.Say("The lock gives way with a crack.");
                    }),
                    new EventChoice("Leave it", _ =>
                        new EventOutcome().Say("Not worth the risk. You move on.", MessageTone.Dim))
                });
        }

        // Offered when a floor's fights are done: go home now, or risk one more fight for a better haul.
        public static DungeonEvent SidePassage()
        {
            return new DungeonEvent(
                "A SIDE PASSAGE",
                new[]
                {
                    "The way back to Oakhaven is clear. But a narrow passage branches off,",
                    "and you can hear something large breathing at the end of it."
                },
                new[]
                {
                    new EventChoice("Return to Oakhaven", _ =>
                        new EventOutcome { ReturnToTown = true }.Say("You take the known road home.", MessageTone.Dim)),
                    new EventChoice("Press on: one more fight for double gold and a rare find", _ =>
                        new EventOutcome { PressOn = true }.Say("You squeeze into the passage. The breathing stops.", MessageTone.Warning))
                });
        }

        // Event damage never kills: it leaves the hero on at least 1 HP.
        private static int LoseHealth(Hero hero, int amount)
        {
            int lost = Math.Min(Math.Max(1, amount), hero.CurrentHealth - 1);
            hero.CurrentHealth -= Math.Max(0, lost);
            return Math.Max(0, lost);
        }
    }
}
