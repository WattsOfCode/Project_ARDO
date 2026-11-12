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
using System.Collections.Generic;
using ARDO.Core.Abstracts;
using ARDO.Core.Entities;

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

        public override List<(string MoveName, string Type, string Description)> GetCombatMoves()
        {
            return new List<(string, string, string)>
            {
                ("Sword Strike", "Attack", $"Deliver a heavy blade arc. Deals {SwordStrikeDamage} physical dmg."),
                ("Shield Bash", "Attack", $"Slam the enemy with your shield. Deals {ShieldBashDamage} dmg, then +2 defense and the next hit is halved."),
                ("Defensive Guard", "Defense", "Hunker behind your shield: +4 defense and the next hit is halved. Best against a heavy blow.")
            };
        }

        protected override void OnLevelUp()
        {
            base.OnLevelUp();
            MaxHealth += 15;
            CurrentHealth = MaxHealth;
        }

        public string SwordStrike(Enemy target)
        {
            HitResult hit = Strike(target, SwordStrikeDamage, canMiss: false);
            return Narrate(hit, $"{Name} drives Sword Strike through {target.Name} for {hit.Damage} damage.", string.Empty);
        }

        public string ShieldBash(Enemy target)
        {
            HitResult hit = Strike(target, ShieldBashDamage, canMiss: false);
            GuardDefense += 2;
            InvulnerabilityCharges += 1;
            return Narrate(hit, $"{Name} crashes Shield Bash into {target.Name} for {hit.Damage} damage and braces for the counterattack.", string.Empty);
        }

        public string DefensiveGuard()
        {
            GuardDefense += 4;
            InvulnerabilityCharges += 1;
            return $"{Name} plants Defensive Guard. Defense rises and the next blow is blunted.";
        }
    }
}
