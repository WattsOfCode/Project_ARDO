using System;
using System.Collections.Generic;
using ARDO.Core.Abstracts;
using ARDO.Core.Interfaces;

namespace ARDO.Game.Players
{
    public class Warrior : Hero
    {
        public int ShieldBlockRating { get; set; } = 5;

        public Warrior(string name) : base(name, maxHealth: 150, currentHealth: 150)
        {
            Level = 1;
            Gold = 100;
        }

        public override List<(string MoveName, string Type, string Description)> GetCombatMoves()
        {
            return new List<(string, string, string)>
            {
                ("Sword Strike", "Attack", $"Deliver a heavy blade arc. Deals {15 + Level + WeaponBonusDamage + SpecialAttackBonus} physical dmg."),
                ("Shield Bash", "Attack", $"Slam enemy with your shield. Deals {8 + WeaponBonusDamage + SpecialAttackBonus} dmg & sets up defense."),
                ("Defensive Guard", "Defense", "Hunker behind your shield. Greatly reduces incoming damage this turn.")
            };
        }

        public override void ExecuteSpecial(ICombatant target)
        {
            SwordStrike(target);
        }

        protected override void OnLevelUp()
        {
            base.OnLevelUp();
            MaxHealth += 15;
            CurrentHealth = MaxHealth;
        }

        public string SwordStrike(ICombatant target)
        {
            if (target == null)
            {
                return $"{Name} has no target for Sword Strike.";
            }

            int damage = 25 + Level + WeaponBonusDamage + SpecialAttackBonus;
            target.TakeDamage(damage);
            return $"{Name} drives Sword Strike through the enemy for {damage} damage.";
        }

        public string ShieldBash(ICombatant target)
        {
            if (target == null)
            {
                return $"{Name} has no target for Shield Bash.";
            }

            int damage = 16 + Level + SpecialAttackBonus;
            target.TakeDamage(damage);
            ShieldBonusDefense += 2;
            InvulnerabilityCharges += 1;
            return $"{Name} crashes Shield Bash for {damage} damage and braces for the counterattack.";
        }

        public string DefensiveGuard()
        {
            ShieldBonusDefense += 4;
            InvulnerabilityCharges += 1;
            return $"{Name} plants Defensive Guard. Defense rises and the next blow is blunted.";
        }
    }
}
