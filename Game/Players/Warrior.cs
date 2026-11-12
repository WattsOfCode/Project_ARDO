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
using ARDO.Core.Interfaces;

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
                ("Shield Bash", "Attack", $"Slam the enemy with your shield. Deals {ShieldBashDamage} dmg and braces for the counterattack."),
                ("Defensive Guard", "Defense", "Hunker behind your shield. Greatly reduces incoming damage this turn.")
            };
        }

        protected override void OnLevelUp()
        {
            base.OnLevelUp();
            MaxHealth += 15;
            CurrentHealth = MaxHealth;
        }

        public string SwordStrike(ICombatant target)
        {
            int damage = SwordStrikeDamage;
            target.TakeDamage(damage);
            return $"{Name} drives Sword Strike through the enemy for {damage} damage.";
        }

        public string ShieldBash(ICombatant target)
        {
            int damage = ShieldBashDamage;
            target.TakeDamage(damage);
            GuardDefense += 2;
            InvulnerabilityCharges += 1;
            return $"{Name} crashes Shield Bash for {damage} damage and braces for the counterattack.";
        }

        public string DefensiveGuard()
        {
            GuardDefense += 4;
            InvulnerabilityCharges += 1;
            return $"{Name} plants Defensive Guard. Defense rises and the next blow is blunted.";
        }
    }
}
