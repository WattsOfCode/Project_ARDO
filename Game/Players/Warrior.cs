/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `Warrior.cs` is a concrete hero subclass that demonstrates polymorphic combat with
    sword and shield abilities. It uses constructors, public combat methods, and
    inherited properties.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using ARDO.Core.Abstracts;
using ARDO.Core.Entities;
using ARDO.Core.Enums;

namespace ARDO.Game.Players
{
    public class Warrior : Hero
    {
        public Warrior(string name) : base(name, maxHealth: 150, currentHealth: 150)
        {
            Level = 1;
            Gold = 100;
        }

        public int SwordStrikeDamage => 25 + Level + WeaponDamage + SpecialAttackBonus;

        public int ShieldBashDamage => 16 + Level + SpecialAttackBonus;

        private int ShieldWallBonus => HasPerk(PerkId.ShieldWall) ? 3 : 0;

        public override List<(string MoveName, string Type, string Description)> GetCombatMoves()
        {
            return new List<(string, string, string)>
            {
                ("Sword Strike", "Attack", $"Deliver a heavy blade arc. Deals {SwordStrikeDamage} physical dmg."),
                ("Shield Bash", "Attack", $"Slam the enemy with your shield. Deals {ShieldBashDamage} dmg, then +{2 + ShieldWallBonus} defense and the next hit is halved."),
                ("Defensive Guard", "Defense", $"Hunker behind your shield: +{4 + ShieldWallBonus} defense and the next hit is halved. Best against a heavy blow.")
            };
        }

        protected override void OnLevelUp()
        {
            base.OnLevelUp();
            MaxHealth += 15;
            CurrentHealth = MaxHealth;
        }

        // Bloodlust heals off the hit; Cleave carries half of it into the next foe still standing.
        public string SwordStrike(Enemy target, IEnumerable<Enemy>? others = null)
        {
            HitResult hit = Strike(target, SwordStrikeDamage, canMiss: false);
            string line = Narrate(hit, $"{Name} drives Sword Strike through {target.Name} for {hit.Damage} damage.", string.Empty);

            if (HasPerk(PerkId.Cleave) && others?.FirstOrDefault(e => !ReferenceEquals(e, target) && e.IsAlive) is Enemy second)
            {
                int carry = Math.Max(1, hit.Damage / 2);
                second.TakeDamage(carry);
                line += $" The blade carries into {second.Name} for {carry}.";
            }

            if (HasPerk(PerkId.Bloodlust))
            {
                int healed = Heal(Math.Max(1, hit.Damage / 4));
                if (healed > 0)
                    line += $" Bloodlust: +{healed} HP.";
            }

            return line;
        }

        public string ShieldBash(Enemy target)
        {
            HitResult hit = Strike(target, ShieldBashDamage, canMiss: false);
            GuardDefense += 2 + ShieldWallBonus;
            InvulnerabilityCharges += 1;
            return Narrate(hit, $"{Name} crashes Shield Bash into {target.Name} for {hit.Damage} damage and braces for the counterattack.", string.Empty);
        }

        public string DefensiveGuard()
        {
            GuardDefense += 4 + ShieldWallBonus;
            InvulnerabilityCharges += 1;
            return $"{Name} plants Defensive Guard. Defense rises and the next blow is blunted.";
        }
    }
}
