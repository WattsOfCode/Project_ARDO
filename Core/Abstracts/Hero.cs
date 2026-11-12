/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `Hero.cs` is an abstract base class that defines the shared combat,
    leveling, inventory, and progression behavior for player characters.
    It includes common properties, constructors, and virtual methods,
    while subclasses add polymorphic special attacks and CRUD remains
    in the repository layer.
 */
using System;
using System.Collections.Generic;
using ARDO.Core.Entities;
using ARDO.Core.Enums;
using ARDO.Core.Interfaces;
using ARDO.Game.Inventory;

namespace ARDO.Core.Abstracts
{
    public abstract class Hero : ICombatant
    {
        public const int BaseFleeChancePercent = 60;
        public const int BaseCritChancePercent = 10;
        public const int MissChancePercent = 5;

        public int CharacterId { get; set; }
        public string Name { get; set; }
        public int Level { get; set; }
        public int Experience { get; set; }
        public int CurrentHealth { get; set; }
        public int MaxHealth { get; set; }
        public int Gold { get; set; }
        public Inventory HeroInventory { get; set; }
        public EquipmentManager Equipment { get; set; }
        public int ActiveStoryNode { get; set; } = 1;

        // True once the hero has reached the epilogue; their story is over.
        public bool StoryComplete { get; set; }

        // Defense from guard moves (Defensive Guard, Shield Bash, Arcane Ward). Lasts until the enemies' turn ends.
        public int GuardDefense { get; set; }
        // Each charge halves the next hit taken (and lets it drop to 0). Also cleared after the enemies' turn.
        public int InvulnerabilityCharges { get; set; }

        // Shrine blessings and the like: extra attack until the hero is back in town.
        public int RunAttackBonus { get; set; }

        public BurnEffect Burn { get; } = new();

        // The last attack this hero made, so the combat log can color crits and misses.
        public HitResult LastHit { get; private set; }

        public int ExperienceToNextLevel => 100 + ((Level - 1) * 75);
        public int LevelAttackBonus => Math.Max(0, (Level - 1) / 3);
        public int SpecialAttackBonus => Math.Max(0, (Level - 1) / 3) * 2;

        public int WeaponDamage => Equipment.DamageBonus + RunAttackBonus;
        public int Defense => GuardDefense + Equipment.DefenseBonus;

        // A keen weapon and a keen ring each add 10%.
        public int CritChancePercent => BaseCritChancePercent + Equipment.CountEffect(ItemEffect.Keen) * 10;

        public int FleeChancePercent => BaseFleeChancePercent + (Equipment.HasEffect(ItemEffect.Swift) ? 15 : 0);

        // Set at the start of a fight when a Bulwark shield is worn; the first heavy blow uses it up.
        public bool BulwarkReady { get; set; }

        public bool IsAlive => CurrentHealth > 0;
        public bool IsDead => !IsAlive;

        protected Hero(string name, int maxHealth, int currentHealth)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Nameless Hero" : name.Trim();
            MaxHealth = maxHealth;
            CurrentHealth = currentHealth;
            HeroInventory = new Inventory();
            Equipment = new EquipmentManager();
        }

        public virtual int BasicAttackDamage => 10 + Level + WeaponDamage + LevelAttackBonus;

        public virtual void Attack(ICombatant target)
        {
            if (target is Enemy enemy)
            {
                Strike(enemy, BasicAttackDamage, canMiss: true);
            }
            else
            {
                target?.TakeDamage(BasicAttackDamage);
            }
        }

        // Every hero attack goes through here: miss and crit rolls, then the weapon's effect.
        public HitResult Strike(Enemy target, int baseDamage, bool canMiss)
        {
            if (canMiss && Random.Shared.Next(100) < MissChancePercent)
            {
                LastHit = HitResult.Miss;
                return LastHit;
            }

            bool critical = Random.Shared.Next(100) < CritChancePercent;
            int damage = critical ? (int)Math.Round(baseDamage * 1.5) : baseDamage;
            target.TakeDamage(damage);

            string notes = string.Empty;
            switch (Equipment.EquippedWeapon?.Effect)
            {
                case ItemEffect.Burning when target.IsAlive:
                    target.Burn.Apply(3, 3 + ActiveStoryNode);
                    notes = $"{target.Name} catches fire.";
                    break;

                case ItemEffect.Vampiric:
                    int healed = Heal(Math.Max(1, damage / 5));
                    if (healed > 0)
                    {
                        notes = $"You drain {healed} HP.";
                    }
                    break;
            }

            LastHit = new HitResult(damage, critical, false, notes);
            return LastHit;
        }

        // Builds the log line for an attack: "Critical hit!" in front, effect notes after.
        protected static string Narrate(HitResult hit, string hitLine, string missLine)
        {
            if (hit.Missed)
                return missLine;

            string line = hit.Critical ? "Critical hit! " + hitLine : hitLine;
            return string.IsNullOrEmpty(hit.Notes) ? line : $"{line} {hit.Notes}";
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
                damage = Math.Max(0, damage / 2 - Defense);
            }
            else
            {
                damage = Math.Max(1, damage - Defense);
            }

            CurrentHealth = Math.Max(0, CurrentHealth - damage);
        }

        // Burn damage ignores armor.
        public int TakeBurnDamage()
        {
            int damage = Burn.Tick();
            CurrentHealth = Math.Max(0, CurrentHealth - damage);
            return damage;
        }

        public int Heal(int amount)
        {
            int before = CurrentHealth;
            CurrentHealth = Math.Min(MaxHealth, CurrentHealth + Math.Max(0, amount));
            return CurrentHealth - before;
        }

        public void ClearGuard()
        {
            GuardDefense = 0;
            InvulnerabilityCharges = 0;
        }

        // Halves a heavy blow if the shield is still ready. Returns the damage to apply.
        public int BraceForHeavyBlow(int damage, out bool braced)
        {
            braced = BulwarkReady;
            BulwarkReady = false;
            return braced ? damage / 2 : damage;
        }

        public void ClearCombatEffects()
        {
            ClearGuard();
            BulwarkReady = false;
            Burn.Clear();
        }

        public void EndDungeonRun()
        {
            RunAttackBonus = 0;
            ClearCombatEffects();
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

        // Full recovery at the inn or after being revived.
        public virtual void RestoreForTown()
        {
            CurrentHealth = MaxHealth;
            ClearCombatEffects();
        }

        public virtual bool EscapeFromCombat()
        {
            return Random.Shared.Next(100) < FleeChancePercent;
        }

        public abstract List<(string MoveName, string Type, string Description)> GetCombatMoves();
    }
}
