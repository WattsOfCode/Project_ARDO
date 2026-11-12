/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `Enemy.cs` is an abstract base class that defines shared enemy health, rewards,
    and combat behavior. It provides common properties, constructors, and virtual methods,
    while concrete enemies add polymorphic AI, art, and attack text.
 */
using System;
using ARDO.Core.Interfaces;

namespace ARDO.Core.Abstracts
{
    public abstract class Enemy : ICombatant
    {
        public string Name { get; set; }
        public int CurrentHealth { get; set; }
        public int MaxHealth { get; set; }
        public int BaseDamage { get; set; }
        public int XpReward { get; set; }
        public int GoldReward { get; set; }
        public int CombatFloor { get; set; } = 1;

        // File name (without .txt) of this enemy's picture in Content/ASCII/Enemies.
        public virtual string ArtKey => "boss";

        // How this enemy's attack reads in the combat log, after its name.
        public virtual string AttackLine => "attacks from the shadows.";

        public bool IsAlive => CurrentHealth > 0;
        public bool IsDead => !IsAlive;

        protected Enemy(string name, int maxHealth, int baseDamage, int xpReward, int goldReward)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
            BaseDamage = baseDamage;
            XpReward = xpReward;
            GoldReward = goldReward;
        }

        public virtual void TakeDamage(int amount)
        {
            CurrentHealth = Math.Max(0, CurrentHealth - amount);
        }

        public virtual void Attack(ICombatant target)
        {
            if (target == null || IsDead) return;
            target.TakeDamage(RollAttackDamage());
        }

        public abstract void ExecuteAI(ICombatant target);

        protected virtual int RollAttackDamage()
        {
            int floorMinimum = Math.Max(1, CombatFloor);
            int lowerBound = Math.Max(floorMinimum, BaseDamage - Math.Max(0, CombatFloor / 2));
            int upperBound = Math.Max(lowerBound, BaseDamage + Math.Max(2, CombatFloor));
            return Random.Shared.Next(lowerBound, upperBound + 1);
        }
    }
}
