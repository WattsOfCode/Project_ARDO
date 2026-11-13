/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `Alhuezo.cs` is the concrete final boss, the God-King. The first meeting is a
    scripted loss that the combat engine plays as the epilogue. When a finished
    hero returns with the three keys, he fights for real, sized to the hero who
    came back for him.
 */
using System;
using ARDO.Core.Abstracts;

namespace ARDO.Game.Enemies
{
    public sealed class Alhuezo : Boss
    {
        // The first throne fight: he can't be beaten, and the engine plays the ending.
        public Alhuezo() : base("Alhuezo, the God-King", 999, 80)
        {
            IsScripted = true;
        }

        // The rematch with the keys: strong, but beatable by the hero standing in front of him.
        private Alhuezo(int health, int damage) : base("Alhuezo, the God-King", health, damage)
        {
            XpReward = 600;
            GoldReward = 500;
        }

        public static Alhuezo Unbound(Hero hero)
        {
            int health = 120 + 8 * hero.BasicAttackDamage;
            int damage = hero.MaxHealth / 13 + hero.Equipment.DefenseBonus * 7 / 8;
            return new Alhuezo(health, damage);
        }

        public bool IsScripted { get; }

        public override string ArtKey => "Alhuezo";

        public override string AttackLine => "lifts the room with a king's crushing blow.";

        public override string HeavyAttackLine => "brings the turning wheel down on you.";

        public override string WindUpWarning => "Alhuezo raises one hand, and the great wheel above the throne begins to turn!";

        protected override int HeavyChancePercent => IsScripted ? 20 : 25;

        protected override int TrickChancePercent => IsScripted ? 0 : 20;

        // The wheel turns backward for him: a wound closes, as if it had never been made.
        protected override string UseTrick(Hero hero)
        {
            int healed = Math.Min(MaxHealth - CurrentHealth, MaxHealth / 14);
            CurrentHealth += healed;
            return healed > 0
                ? $"{Name} turns his palm, and the wheel runs backward. {healed} of his wounds were never made."
                : $"{Name} turns his palm, but there is nothing to undo.";
        }
    }
}
