/**
 * Name: John Deardorff
 * Date: 11/13/26
 * Project ARDO
 *
 * Class description:
 * `PerkCatalog.cs` is a static list of every perk: its name, what it does, and
    which class may take it. On a level-up the hero is offered three at random
    from the shared perks and their own class's, and keeps the one they pick.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using ARDO.Core.Abstracts;
using ARDO.Core.Enums;

namespace ARDO.Game.Progression
{
    public sealed record PerkInfo(PerkId Id, string Name, string Description, string? ClassName = null, bool Repeatable = false);

    public static class PerkCatalog
    {
        public const int ChoicesOffered = 3;
        public const int ToughnessHealth = 20;

        public static readonly IReadOnlyList<PerkInfo> All = new[]
        {
            new PerkInfo(PerkId.Toughness, "Toughness", $"+{ToughnessHealth} max HP. Can be taken more than once.", Repeatable: true),
            new PerkInfo(PerkId.KeenEye, "Keen Eye", "+5% critical hit chance."),
            new PerkInfo(PerkId.SecondWind, "Second Wind", "After every won fight, recover a tenth of your max HP."),
            new PerkInfo(PerkId.Scavenger, "Scavenger", "A quarter more gold from fights and chests."),
            new PerkInfo(PerkId.IronSkin, "Iron Skin", "+2 defense, always."),
            new PerkInfo(PerkId.Executioner, "Executioner", "Your attacks deal 30% more to a foe below a third of its HP."),
            new PerkInfo(PerkId.BrutalCrits, "Brutal Criticals", "Critical hits deal double damage instead of half again."),
            new PerkInfo(PerkId.FleetFooted, "Fleet-Footed", "+15% chance to flee."),
            new PerkInfo(PerkId.QuickStudy, "Quick Study", "A fifth more XP from fights."),

            new PerkInfo(PerkId.Bloodlust, "Bloodlust", "Sword Strike heals you for a quarter of the damage it deals.", "Warrior"),
            new PerkInfo(PerkId.Cleave, "Cleave", "Sword Strike carries through to a second foe for half damage.", "Warrior"),
            new PerkInfo(PerkId.ShieldWall, "Shield Wall", "Shield Bash and Defensive Guard each give +3 more defense.", "Warrior"),
            new PerkInfo(PerkId.LastStand, "Last Stand", "Once a fight, a blow that would kill you leaves you at 1 HP.", "Warrior"),

            new PerkInfo(PerkId.ManaFlow, "Mana Flow", "Recover 6 mana at the end of every round.", "Mage"),
            new PerkInfo(PerkId.Pyromancer, "Pyromancer", "Firebolt sets its target burning.", "Mage"),
            new PerkInfo(PerkId.Overcharge, "Overcharge", "Lightning Cascade arcs for three quarters of its damage, not half.", "Mage"),
            new PerkInfo(PerkId.EfficientCasting, "Efficient Casting", "Every spell costs 5 less mana.", "Mage"),
            new PerkInfo(PerkId.MirrorWard, "Mirror Ward", "Arcane Ward also heals you for a tenth of your max HP.", "Mage"),

            new PerkInfo(PerkId.SteadyAim, "Steady Aim", "Aimed Shot has +25% critical hit chance.", "Archer"),
            new PerkInfo(PerkId.Barrage, "Barrage", "Volley deals 40% more to every foe.", "Archer"),
            new PerkInfo(PerkId.BarbedArrows, "Barbed Arrows", "Pinning Shot leaves the target bleeding for 3 rounds.", "Archer"),
            new PerkInfo(PerkId.QuickDraw, "Quick Draw", "Your first special attack in each fight has no cooldown.", "Archer"),

            new PerkInfo(PerkId.Ambusher, "Ambusher", "Backstab on an unhurt foe deals double, not half again.", "Rogue"),
            new PerkInfo(PerkId.VirulentPoison, "Virulent Poison", "Poison Blade's poison is half again as strong and lasts 6 rounds.", "Rogue"),
            new PerkInfo(PerkId.ShadowDance, "Shadow Dance", "After you dodge a hit, your next attack is a critical hit.", "Rogue"),
            new PerkInfo(PerkId.Cutpurse, "Cutpurse", "Basic attacks that land lift 3 to 6 gold.", "Rogue"),
            new PerkInfo(PerkId.Evasive, "Evasive", "+8% chance to dodge a hit outright.", "Rogue")
        };

        private static readonly Dictionary<PerkId, PerkInfo> ById = All.ToDictionary(perk => perk.Id);

        public static PerkInfo Get(PerkId id) => ById[id];

        // Every perk this hero could still take: shared or their class's, and not already held unless it stacks.
        public static List<PerkInfo> Available(Hero hero) =>
            All.Where(perk => (perk.ClassName == null || perk.ClassName == hero.ClassName)
                              && (perk.Repeatable || !hero.HasPerk(perk.Id)))
               .ToList();

        // Three different perks at random. Toughness always stacks, so there is always at least one.
        public static List<PerkInfo> Offer(Hero hero, Random? random = null)
        {
            random ??= Random.Shared;
            return Available(hero).OrderBy(_ => random.Next()).Take(ChoicesOffered).ToList();
        }

        public static void Apply(Hero hero, PerkId id)
        {
            hero.Perks.Add(id);
            if (id == PerkId.Toughness)
            {
                hero.MaxHealth += ToughnessHealth;
                hero.CurrentHealth += ToughnessHealth;
            }
        }
    }
}
