/**
 * Name: John Deardorff
 * Date: 11/13/26
 * Project ARDO
 *
 * Class description:
 * `PerkEvents.cs` builds the level-up screen: three perks to choose from,
    shown like any other event so they can be clicked or typed.
 */
using System.Linq;
using ARDO.Core.Abstracts;
using ARDO.Core.Enums;
using ARDO.Game.Progression;

namespace ARDO.Game.Events
{
    public static class PerkEvents
    {
        public static DungeonEvent Choose(Hero hero)
        {
            var description = new System.Collections.Generic.List<string>
            {
                $"Level {hero.Level}. Something in you has changed for good. Choose what it becomes."
            };

            if (hero.PendingPerkPicks > 1)
            {
                description.Add($"({hero.PendingPerkPicks} perks to choose, one at a time.)");
            }

            var choices = PerkCatalog.Offer(hero)
                .Select(perk => new EventChoice($"{perk.Name}: {perk.Description}", h => Take(h, perk)))
                .ToList();

            return new DungeonEvent("LEVEL UP: CHOOSE A PERK", description, choices);
        }

        private static EventOutcome Take(Hero hero, PerkInfo perk)
        {
            PerkCatalog.Apply(hero, perk.Id);
            hero.PendingPerkPicks = System.Math.Max(0, hero.PendingPerkPicks - 1);
            return new EventOutcome().Say($"You take {perk.Name}.", MessageTone.Gold);
        }
    }
}
