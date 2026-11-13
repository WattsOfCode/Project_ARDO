/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `Mage.cs` is a concrete hero subclass that demonstrates polymorphic spell
    combat and mana-based abilities. It uses constructors, public combat
    methods, and inherited properties.
 */
using System;
using System.Collections.Generic;
using ARDO.Core.Abstracts;
using ARDO.Core.Entities;
using ARDO.Core.Enums;

namespace ARDO.Game.Players
{
    public class Mage : Hero
    {
        public const int FireboltCost = 15;
        public const int LightningCascadeCost = 30;
        public const int ArcaneWardCost = 10;

        private const int LightningMinRoll = 22;
        private const int LightningMaxRoll = 34;

        public int CurrentMana { get; set; }
        public int MaxMana { get; set; }

        public Mage(string name) : base(name, maxHealth: 90, currentHealth: 90)
        {
            Level = 1;
            MaxMana = 100;
            CurrentMana = 100;
            Gold = 100;
        }

        public override int BasicAttackDamage => 12 + Level + WeaponDamage + LevelAttackBonus + SpecialAttackBonus;

        public int FireboltDamage => 24 + Level + WeaponDamage + SpecialAttackBonus;

        private int LightningBonus => Level + WeaponDamage + SpecialAttackBonus;

        public override List<(string MoveName, string Type, string Description)> GetCombatMoves()
        {
            return new List<(string, string, string)>
            {
                ("Firebolt", "Attack", $"Launch a blazing projectile. Deals {FireboltDamage} magic dmg{(HasPerk(PerkId.Pyromancer) ? " and sets it burning" : "")}. (Cost: {CostOf(1)} Mana)"),
                ("Lightning Cascade", "Attack", $"Shock the target for {LightningMinRoll + LightningBonus} to {LightningMaxRoll + LightningBonus} dmg; arcs to the others for {(HasPerk(PerkId.Overcharge) ? "three quarters" : "half")}. (Cost: {CostOf(2)} Mana)"),
                ("Arcane Ward", "Defense", $"Raise a shield: +3 defense and the next hit is halved{(HasPerk(PerkId.MirrorWard) ? ", and heal a tenth of your HP" : "")}. (Cost: {CostOf(3)} Mana)")
            };
        }

        // Mana each special move needs, by its menu number (1 to 3).
        public static int ManaCostFor(int moveNumber)
        {
            return moveNumber switch
            {
                1 => FireboltCost,
                2 => LightningCascadeCost,
                3 => ArcaneWardCost,
                _ => 0
            };
        }

        // What a move costs this mage, after Efficient Casting.
        public int CostOf(int moveNumber) =>
            Math.Max(0, ManaCostFor(moveNumber) - (HasPerk(PerkId.EfficientCasting) ? 5 : 0));

        public int BurnPerTurn => 4 + Level / 2;

        protected override void OnLevelUp()
        {
            base.OnLevelUp();
            MaxMana += 15;
            CurrentMana = MaxMana;
        }

        public override void RestoreForTown()
        {
            base.RestoreForTown();
            CurrentMana = MaxMana;
        }

        public string CastFirebolt(Enemy target)
        {
            if (CurrentMana < CostOf(1))
            {
                return $"{Name} tries to cast Firebolt but lacks mana.";
            }

            CurrentMana -= CostOf(1);
            HitResult hit = Strike(target, FireboltDamage, canMiss: false);
            string line = Narrate(hit, $"{Name} hurls Firebolt at {target.Name} for {hit.Damage} damage.", string.Empty);
            if (HasPerk(PerkId.Pyromancer) && target.IsAlive)
            {
                target.Burn.Apply(3, BurnPerTurn);
                line += $" {target.Name} catches fire.";
            }

            return line + $" Mana {CurrentMana}/{MaxMana}.";
        }

        // Hits the target, then arcs to every other standing enemy for half damage.
        public string CastLightningCascade(Enemy target, IEnumerable<Enemy> otherEnemies)
        {
            if (CurrentMana < CostOf(2))
            {
                return $"{Name} tries to call Lightning Cascade but lacks mana.";
            }

            CurrentMana -= CostOf(2);
            int roll = Random.Shared.Next(LightningMinRoll, LightningMaxRoll + 1) + LightningBonus;
            HitResult hit = Strike(target, roll, canMiss: false);
            string line = Narrate(hit, $"{Name} unleashes Lightning Cascade on {target.Name} for {hit.Damage} damage.", string.Empty);

            int arcDamage = Math.Max(1, HasPerk(PerkId.Overcharge) ? roll * 3 / 4 : roll / 2);
            var arcedTo = new List<string>();
            foreach (Enemy other in otherEnemies)
            {
                if (ReferenceEquals(other, target) || other.IsDead)
                    continue;

                other.TakeDamage(arcDamage);
                arcedTo.Add(other.Name);
            }

            if (arcedTo.Count > 0)
            {
                line += $" It arcs to {string.Join(", ", arcedTo)} for {arcDamage} each.";
            }

            return line + $" Mana {CurrentMana}/{MaxMana}.";
        }

        public string CastArcaneWard()
        {
            if (CurrentMana < CostOf(3))
            {
                return $"{Name} tries to raise Arcane Ward but lacks mana.";
            }

            CurrentMana -= CostOf(3);
            GuardDefense += 3;
            InvulnerabilityCharges += 1;
            string mirror = string.Empty;
            if (HasPerk(PerkId.MirrorWard))
            {
                int healed = Heal(MaxHealth / 10);
                if (healed > 0)
                    mirror = $" The ward gives back {healed} HP.";
            }

            return $"{Name} weaves Arcane Ward. Defense rises and the next strike will be halved.{mirror} Mana {CurrentMana}/{MaxMana}.";
        }
    }
}
