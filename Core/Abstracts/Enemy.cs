/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `Enemy.cs` is an abstract base class that defines shared enemy health, rewards,
    and combat behavior. Each turn an enemy carries out the move it planned the turn
    before, so heavy blows can be announced in advance. Concrete enemies add their
    own tricks, art, and attack text.
 */
using System;
using ARDO.Core.Entities;
using ARDO.Core.Enums;
using ARDO.Core.Interfaces;

namespace ARDO.Core.Abstracts
{
    public abstract class Enemy : ICombatant
    {
        protected const int MissChancePercent = 8;

        private string? _announcement;

        public string Name { get; set; }
        public int CurrentHealth { get; set; }
        public int MaxHealth { get; set; }
        public int BaseDamage { get; set; }
        public int XpReward { get; set; }
        public int GoldReward { get; set; }
        public int CombatFloor { get; set; } = 1;

        // What this enemy will do on its next turn. The first turn is always a normal attack.
        public EnemyIntent NextIntent { get; protected set; } = EnemyIntent.Attack;

        public BurnEffect Burn { get; } = new();

        // Guardians (the tougher named foes) always drop gear.
        public virtual bool IsGuardian => false;

        // File name (without .txt) of this enemy's picture in Content/ASCII/Enemies.
        public virtual string ArtKey => "boss";

        // How this enemy's attack reads in the combat log, after its name.
        public virtual string AttackLine => "attacks from the shadows.";

        public virtual string HeavyAttackLine => "brings down the blow it was winding up.";

        public virtual string WindUpWarning => $"{Name} draws back for a heavy blow!";

        protected virtual int HeavyChancePercent => 20;

        protected virtual double HeavyMultiplier => 2.0;

        protected virtual int TrickChancePercent => 0;

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

        // Carries out the planned move, then plans the next one. Returns the combat log line.
        public virtual string TakeTurn(Hero hero)
        {
            string line = NextIntent switch
            {
                EnemyIntent.Heavy => HeavyBlow(hero),
                EnemyIntent.Trick => UseTrick(hero),
                _ => HitHero(hero, RollAttackDamage(), AttackLine)
            };

            NextIntent = PlanNextMove();
            return line;
        }

        private string HeavyBlow(Hero hero)
        {
            int damage = hero.BraceForHeavyBlow((int)Math.Round(RollAttackDamage() * HeavyMultiplier), out bool braced);
            string line = HitHero(hero, damage, HeavyAttackLine);
            return braced ? $"{line} Your shield takes the worst of it." : line;
        }

        protected virtual EnemyIntent PlanNextMove()
        {
            int roll = Random.Shared.Next(100);

            if (roll < HeavyChancePercent)
                return EnemyIntent.Heavy;

            if (roll < HeavyChancePercent + TrickChancePercent)
                return EnemyIntent.Trick;

            return EnemyIntent.Attack;
        }

        // Enemies with a special move override this. By default a trick is a normal attack.
        protected virtual string UseTrick(Hero hero)
        {
            return HitHero(hero, RollAttackDamage(), AttackLine);
        }

        protected string HitHero(Hero hero, int damage, string action)
        {
            if (Random.Shared.Next(100) < MissChancePercent)
            {
                return $"{Name} {action} It misses.";
            }

            int before = hero.CurrentHealth;
            hero.TakeDamage(damage);
            int dealt = before - hero.CurrentHealth;

            return dealt > 0
                ? $"{Name} {action} It deals {dealt} damage."
                : $"{Name} {action} You absorb or deflect the blow.";
        }

        protected virtual int RollAttackDamage()
        {
            int floorMinimum = Math.Max(1, CombatFloor);
            int lowerBound = Math.Max(floorMinimum, BaseDamage - Math.Max(0, CombatFloor / 2));
            int upperBound = Math.Max(lowerBound, BaseDamage + Math.Max(2, CombatFloor));
            return Random.Shared.Next(lowerBound, upperBound + 1);
        }

        // Things that happen outside the enemy's turn (getting back up, flying into a rage)
        // are queued here and written to the log by the combat engine.
        protected void Announce(string message)
        {
            _announcement = _announcement == null ? message : $"{_announcement} {message}";
        }

        public string? TakeAnnouncement()
        {
            string? message = _announcement;
            _announcement = null;
            return message;
        }
    }
}
