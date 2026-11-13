/**
 * Name: John Deardorff
 * Date: 11/12/26
 * Project ARDO
 *
 * Class description:
 * `ShadowSelf.cs` is a concrete enemy met only by a rogue who picks the sealed
    lock instead of using the three keys. Beyond the door waits the part of the
    hero the wheel has been turning backward, sized to the hero who found it.
 */
using ARDO.Core.Abstracts;

namespace ARDO.Game.Enemies
{
    public sealed class ShadowSelf : Boss
    {
        private readonly string _heroArt;

        private ShadowSelf(string heroName, string heroArt, int health, int damage) : base($"Shadow of {heroName}", health, damage)
        {
            _heroArt = heroArt;
            XpReward = 450;
            GoldReward = 300;
        }

        public static ShadowSelf Of(Hero hero)
        {
            int health = 60 + 7 * hero.BasicAttackDamage;
            int damage = hero.MaxHealth / 12 + hero.Equipment.DefenseBonus * 7 / 8;
            return new ShadowSelf(hero.Name, hero.ArtKey, health, damage);
        }

        // The hero's own portrait, a clone in another colour.
        public override string ArtKey => _heroArt;

        public override string ArtFolder => "Heroes";

        public override bool ShadowTint => true;

        public override string AttackLine => "answers with your own blade, a beat behind you.";

        public override string HeavyAttackLine => "comes at you with every trick you know.";

        public override string WindUpWarning => $"{Name} mirrors your stance, its grip reversed!";

        protected override int HeavyChancePercent => 30;

        protected override int TrickChancePercent => 20;

        protected override double HeavyMultiplier => 1.8;

        protected override string UseTrick(Hero hero)
        {
            return HitHero(hero, RollAttackDamage() * 3 / 2, "steps out of your blind spot, the way you would have.");
        }
    }
}
