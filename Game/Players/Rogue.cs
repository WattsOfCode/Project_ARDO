/**
 * Name: John Deardorff
 * Date: 11/12/26
 * Project ARDO
 *
 * Class description:
 * `Rogue.cs` is a concrete hero subclass built on getting in first and getting
    out clean: a backstab that hits hardest on an unhurt foe, a poisoned blade,
    and a smoke step that makes the next hit miss. Rogues dodge now and then,
    flee more easily, and can pick the sealed lock at the throne.
 */
using System;
using System.Collections.Generic;
using ARDO.Core.Abstracts;
using ARDO.Core.Entities;
using ARDO.Core.Enums;

namespace ARDO.Game.Players
{
    public class Rogue : Hero
    {
        public Rogue(string name) : base(name, maxHealth: 100, currentHealth: 100)
        {
            Level = 1;
            Gold = 100;
        }

        protected override int ClassCritBonus => 5;
        protected override int ClassFleeBonus => 15;
        protected override int ClassDodgeBonus => 12;
        public override bool CanPickLocks => true;

        public int BackstabDamage => 22 + Level + WeaponDamage + SpecialAttackBonus;

        public int PoisonBladeDamage => 12 + Level + WeaponDamage + SpecialAttackBonus;

        public int PoisonPerTurn => HasPerk(PerkId.VirulentPoison) ? (4 + Level / 2) * 3 / 2 : 4 + Level / 2;

        public int PoisonRounds => HasPerk(PerkId.VirulentPoison) ? 6 : 4;

        public override List<(string MoveName, string Type, string Description)> GetCombatMoves()
        {
            return new List<(string, string, string)>
            {
                ("Backstab", "Attack", $"Strike where it hurts. Deals {BackstabDamage} dmg, {(HasPerk(PerkId.Ambusher) ? "double" : "half again")} on a foe that is still unhurt."),
                ("Poison Blade", "Attack", $"Cut for {PoisonBladeDamage} dmg and poison the wound: {PoisonPerTurn} a round for {PoisonRounds} rounds."),
                ("Smoke Step", "Defense", "Vanish in a puff of smoke: the next hit against you misses completely.")
            };
        }

        protected override void OnLevelUp()
        {
            base.OnLevelUp();
            MaxHealth += 5;
            CurrentHealth = MaxHealth;
        }

        public string Backstab(Enemy target)
        {
            bool unhurt = target.CurrentHealth >= target.MaxHealth;
            int damage = !unhurt ? BackstabDamage : HasPerk(PerkId.Ambusher) ? BackstabDamage * 2 : BackstabDamage * 3 / 2;
            HitResult hit = Strike(target, damage, canMiss: false);
            string opener = unhurt ? $"{Name} slips behind {target.Name} before it ever sees you" : $"{Name} finds a gap in {target.Name}'s guard";
            return Narrate(hit, $"{opener}: Backstab for {hit.Damage} damage.", string.Empty);
        }

        public string PoisonBlade(Enemy target)
        {
            HitResult hit = Strike(target, PoisonBladeDamage, canMiss: false);
            string line = Narrate(hit, $"{Name} opens {target.Name} with a Poison Blade for {hit.Damage} damage.", string.Empty);
            if (target.IsAlive)
            {
                target.Poison.Apply(PoisonRounds, PoisonPerTurn);
                line += $" The poison takes hold ({PoisonPerTurn} a round).";
            }

            return line;
        }

        public string SmokeStep()
        {
            EvadeNext = true;
            return $"{Name} cracks a smoke pellet and is somewhere else. The next blow will miss.";
        }
    }
}
