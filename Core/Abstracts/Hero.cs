using System;
using System.Collections.Generic;
using ARDO.Core.Interfaces;
using ARDO.Core.Entities;
using ARDO.Game.Inventory;

namespace ARDO.Core.Abstracts
{
    public abstract class Hero : ICombatant
    {
        public int CharacterId { get; set; }
        public string Name { get; set; }
        public int Level { get; set; }
        public int Experience { get; set; }
        public int CurrentHealth { get; set; }
        public int MaxHealth { get; set; }
        public int Gold { get; set; }
        public Inventory HeroInventory { get; set; }
        public int ActiveStoryNode { get; set; } = 1;

        public int WeaponBonusDamage { get; set; } = 0;
        public int ShieldBonusDefense { get; set; } = 0;
        public int InvulnerabilityCharges { get; set; } = 0;
        public bool IsRestingInSafety { get; set; } = false;
        public EquipmentManager Equipment { get; set; }
        public int ExperienceToNextLevel => 100 + ((Level - 1) * 75);
        public int LevelAttackBonus => Math.Max(0, (Level - 1) / 3);
        public int SpecialAttackBonus => Math.Max(0, (Level - 1) / 3) * 2;

        public int TotalAttackPower =>
            10 + WeaponBonusDamage +
            (Equipment.EquippedWeapon?.DamageBonus ?? 0);

        public int TotalDefense =>
            ShieldBonusDefense +
            (Equipment.EquippedArmor?.DefenseBonus ?? 0);

        public bool IsAlive => CurrentHealth > 0;
        public bool IsDead => !IsAlive;

        private static readonly Random _random = new Random();

        protected Hero(string name, int maxHealth, int currentHealth)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Nameless Hero" : name;
            MaxHealth = maxHealth;
            CurrentHealth = currentHealth;
            HeroInventory = new Inventory(20);
            Equipment = new EquipmentManager();
        }

        public virtual void Attack(ICombatant target)
        {
            int damage = 10 + Level + WeaponBonusDamage + LevelAttackBonus;
            target.TakeDamage(damage);
        }

        public virtual void TakeDamage(int damage)
        {
            if (damage <= 0)
            {
                return;
            }

            if (InvulnerabilityCharges > 0)
            {
                InvulnerabilityCharges--;
                damage = Math.Max(0, damage - ShieldBonusDefense);
            }
            else
            {
                damage = Math.Max(1, damage - ShieldBonusDefense);
            }

            CurrentHealth -= damage;

            if (CurrentHealth < 0)
            {
                CurrentHealth = 0;
            }
        }

        public virtual bool GainExperience(int amount)
        {
            if (amount <= 0)
                return false;

            Experience += amount;
            bool leveledUp = false;

            while (Experience >= ExperienceToNextLevel)
            {
                Experience -= ExperienceToNextLevel;
                Level++;
                OnLevelUp();
                leveledUp = true;
            }

            return leveledUp;
        }

        protected virtual void OnLevelUp()
        {
            MaxHealth += 10;
            CurrentHealth = MaxHealth;
        }

        public virtual void RestoreForTown()
        {
            CurrentHealth = MaxHealth;
        }

        public virtual bool EscapeFromCombat()
        {
            int roll = _random.Next(1, 101);
            bool success = roll > 40;
            IsRestingInSafety = success;
            return success;
        }

        public abstract List<(string MoveName, string Type, string Description)> GetCombatMoves();
        public abstract void ExecuteSpecial(ICombatant target);

        public virtual void DisplayStatusCard()
        {
            Console.WriteLine("========================");
            Console.WriteLine($"Name: {Name}");
            Console.WriteLine($"Level: {Level}");
            Console.WriteLine($"HP: {CurrentHealth}/{MaxHealth}");
            Console.WriteLine($"Gold: {Gold}g");
            Console.WriteLine($"Weapon Bonus: +{WeaponBonusDamage} ATK");
            Console.WriteLine($"Shield Bonus: +{ShieldBonusDefense} DEF");
            if (InvulnerabilityCharges > 0)
            {
                Console.WriteLine($"✨ INVULNERABILITY CHARGES: {InvulnerabilityCharges} ✨");
            }
            Console.WriteLine("========================");
        }
    }
}
