/**
 * Name: John Deardorff
 * Date: 11/14/26
 * Project ARDO
 *
 * Class description:
 * `GoblinCutpurse.cs` is a veteran thief of the Robbers' Road. It steals more
    than a common goblin, and once it has your gold it makes for the dark: kill
    it within two of its turns, or it escapes with the purse.
 */
using System;
using ARDO.Core.Abstracts;

namespace ARDO.Game.Enemies
{
    public sealed class GoblinCutpurse : Minion
    {
        private const int TurnsToEscape = 2;
        private int _stolen;
        private int _turnsLeft;

        public GoblinCutpurse() : base("Goblin Cutpurse", 85, 13, 60, 30)
        {
        }

        public override string ArtKey => "goblin";
        public override string AttackLine => "darts in with a hooked knife.";
        protected override int TrickChancePercent => _stolen > 0 ? 0 : 40;

        public override string? StatusNote => _stolen > 0 ? $"has {_stolen} of your gold, escaping in {_turnsLeft}" : null;

        public override string TakeTurn(Hero hero)
        {
            if (_stolen > 0)
            {
                _turnsLeft--;
                if (_turnsLeft <= 0)
                {
                    int lost = _stolen;
                    _stolen = 0;
                    XpReward = 0;
                    GoldReward = 0;
                    CurrentHealth = 0;
                    return $"{Name} slips into a crack in the wall and is gone, with {lost} of your gold.";
                }
            }

            return base.TakeTurn(hero);
        }

        protected override string UseTrick(Hero hero)
        {
            int taken = Math.Min(hero.Gold, 20 + CombatFloor * 5);
            if (taken <= 0)
                return HitHero(hero, RollAttackDamage(), AttackLine);

            hero.Gold -= taken;
            GoldReward += taken;
            _stolen += taken;
            _turnsLeft = TurnsToEscape;
            return $"{Name} cuts your purse strings and backs toward the dark with {taken} gold. Kill it before it gets away!";
        }
    }
}
