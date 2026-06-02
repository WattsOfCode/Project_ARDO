using System;
using System.Collections.Generic;
using ARDO.Core.Abstracts;
using ARDO.Core.Interfaces;

namespace ARDO.Game.Players
{
    public class Mage : Hero
    {
        private static readonly Random _random = new Random();

        public int CurrentMana { get; set; }
        public int MaxMana { get; set; }

        public Mage(string name) : base(name, maxHealth: 90, currentHealth: 90)
        {
            Level = 1;
            MaxMana = 100;
            CurrentMana = 100;
            Gold = 100;
        }

        public override List<(string MoveName, string Type, string Description)> GetCombatMoves()
        {
            return new List<(string, string, string)>
            {
                ("Firebolt", "Attack", $"Launch a blazing projectile. Deals {20 + Level + WeaponBonusDamage + SpecialAttackBonus} magic dmg. (Cost: 15 Mana)"),
                ("Lightning Cascade", "Attack", $"Shock target dynamically. Deals 10 to {25 + WeaponBonusDamage + SpecialAttackBonus} random dmg. (Cost: 30 Mana)"),
                ("Arcane Ward", "Defense", "Erect a magical shield that mitigates incoming damage. (Cost: 10 Mana)")
            };
        }

        public override void DisplayStatusCard()
        {
            Console.WriteLine("========================");
            Console.WriteLine($"Name: {Name} (Mage)");
            Console.WriteLine($"Level: {Level}");
            Console.WriteLine($"HP: {CurrentHealth}/{MaxHealth}");
            Console.WriteLine($"Mana: {CurrentMana}/{MaxMana}");
            Console.WriteLine($"Gold: {Gold}g");
            Console.WriteLine($"Weapon Bonus: +{WeaponBonusDamage} ATK");
            Console.WriteLine($"Shield Bonus: +{ShieldBonusDefense} DEF");
            if (InvulnerabilityCharges > 0)
            {
                Console.WriteLine($"INVULNERABILITY CHARGES: {InvulnerabilityCharges}");
            }
            Console.WriteLine("========================");
        }

        public override void ExecuteSpecial(ICombatant target)
        {
            CastFirebolt(target);
        }

        public override void Attack(ICombatant target)
        {
            if (target == null)
            {
                return;
            }

            int damage = 12 + Level + WeaponBonusDamage + LevelAttackBonus + SpecialAttackBonus;
            target.TakeDamage(damage);
        }

        protected override void OnLevelUp()
        {
            base.OnLevelUp();
            MaxMana += 15;
            CurrentMana = MaxMana;
        }

        public string CastFirebolt(ICombatant target)
        {
            if (target == null)
            {
                return $"{Name} has no target for Firebolt.";
            }

            const int manaCost = 15;
            if (CurrentMana < manaCost)
            {
                return $"{Name} tries to cast Firebolt but lacks mana.";
            }

            CurrentMana -= manaCost;
            int damage = 24 + Level + WeaponBonusDamage + SpecialAttackBonus;
            target.TakeDamage(damage);
            return $"{Name} hurls Firebolt for {damage} damage. Mana {CurrentMana}/{MaxMana}.";
        }

        public string CastLightningCascade(ICombatant target)
        {
            if (target == null)
            {
                return $"{Name} has no target for Lightning Cascade.";
            }

            const int manaCost = 30;
            if (CurrentMana < manaCost)
            {
                return $"{Name} tries to call Lightning Cascade but lacks mana.";
            }

            CurrentMana -= manaCost;
            int damage = _random.Next(22, 35) + Level + WeaponBonusDamage + SpecialAttackBonus;
            target.TakeDamage(damage);
            return $"{Name} unleashes Lightning Cascade for {damage} damage. Mana {CurrentMana}/{MaxMana}.";
        }

        public string CastArcaneWard()
        {
            const int manaCost = 10;
            if (CurrentMana < manaCost)
            {
                return $"{Name} tries to raise Arcane Ward but lacks mana.";
            }

            CurrentMana -= manaCost;
            ShieldBonusDefense += 3;
            InvulnerabilityCharges += 1;
            return $"{Name} weaves Arcane Ward. Defense rises and one strike can be turned aside. Mana {CurrentMana}/{MaxMana}.";
        }
    }
}
