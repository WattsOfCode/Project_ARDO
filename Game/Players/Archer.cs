/**
 * Name: John Deardorff
 * Date: 11/12/26
 * Project ARDO
 *
 * Class description:
 * `Archer.cs` is a concrete hero subclass that fights from range: a sharper eye
    for critical hits, a volley that reaches every foe, and a pinning shot that
    stops a heavy blow before it lands. It uses constructors, public combat
    methods, and inherited properties.
 */
using System;
using System.Collections.Generic;
using ARDO.Core.Abstracts;
using ARDO.Core.Entities;
using ARDO.Core.Enums;

namespace ARDO.Game.Players
{
    public class Archer : Hero
    {
        public Archer(string name) : base(name, maxHealth: 110, currentHealth: 110)
        {
            Level = 1;
            Gold = 100;
        }

        protected override int ClassCritBonus => 10;

        public int AimedShotDamage => 27 + Level + WeaponDamage + SpecialAttackBonus;

        public int VolleyDamage => Math.Max(1, (14 + Level + WeaponDamage + SpecialAttackBonus) * 2 / 3 * (HasPerk(PerkId.Barrage) ? 7 : 5) / 5);

        public int BleedPerTurn => 3 + Level / 2;

        public int PinningShotDamage => 8 + Level + SpecialAttackBonus;

        public override List<(string MoveName, string Type, string Description)> GetCombatMoves()
        {
            return new List<(string, string, string)>
            {
                ("Aimed Shot", "Attack", $"Take your time and loose one true arrow. Deals {AimedShotDamage} dmg and never misses."),
                ("Volley", "Attack", $"Rain arrows on every standing foe for {VolleyDamage} dmg each."),
                ("Pinning Shot", "Defense", HasPerk(PerkId.BarbedArrows)
                    ? $"Pin the target for {PinningShotDamage} dmg, break any heavy blow, and leave it bleeding {BleedPerTurn} a round."
                    : $"Pin the target for {PinningShotDamage} dmg and break any heavy blow it is winding up.")
            };
        }

        protected override void OnLevelUp()
        {
            base.OnLevelUp();
            MaxHealth += 5;
            CurrentHealth = MaxHealth;
        }

        public string AimedShot(Enemy target)
        {
            HitResult hit = Strike(target, AimedShotDamage, canMiss: false, extraCritChance: HasPerk(PerkId.SteadyAim) ? 25 : 0);
            return Narrate(hit, $"{Name} draws, holds, and looses an Aimed Shot into {target.Name} for {hit.Damage} damage.", string.Empty);
        }

        // Every standing enemy takes an arrow; each one rolls its own crit.
        public string Volley(IEnumerable<Enemy> enemies)
        {
            var hits = new List<string>();
            foreach (Enemy enemy in enemies)
            {
                if (enemy.IsDead)
                    continue;

                HitResult hit = Strike(enemy, VolleyDamage, canMiss: false);
                hits.Add(hit.Critical ? $"{enemy.Name} {hit.Damage} (crit)" : $"{enemy.Name} {hit.Damage}");
            }

            return $"{Name} looses a Volley. Arrows fall on {string.Join(", ", hits)}.";
        }

        public string PinningShot(Enemy target)
        {
            HitResult hit = Strike(target, PinningShotDamage, canMiss: false);
            string line = $"{Name} pins {target.Name} with a quick shot for {hit.Damage} damage.";
            if (HasPerk(PerkId.BarbedArrows) && target.IsAlive)
            {
                target.Poison.Apply(3, BleedPerTurn);
                line += $" The barbs stay in: it bleeds {BleedPerTurn} a round.";
            }

            return target.IsAlive && target.Interrupt()
                ? $"{line} Its heavy blow is broken before it can fall."
                : line;
        }
    }
}
