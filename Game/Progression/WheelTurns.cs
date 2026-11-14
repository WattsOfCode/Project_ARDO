/**
 * Name: John Deardorff
 * Date: 11/13/26
 * Project ARDO
 *
 * Class description:
 * `WheelTurns.cs` is a static list of what each turn of the wheel adds. A hero
    who has reached an ending may turn the wheel again: the keys scatter, the
    guardians rise, and the floors come back harder. Each turn keeps every twist
    before it and adds one more, and pays more XP and gold for the trouble.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using ARDO.Core.Abstracts;
using ARDO.Game.Enemies;

namespace ARDO.Game.Progression
{
    public sealed record WheelTwist(int Turn, string Name, string Description);

    public static class WheelTurns
    {
        public static readonly IReadOnlyList<WheelTwist> Twists = new[]
        {
            new WheelTwist(1, "Bloodied Stone", "Enemies have a quarter more health."),
            new WheelTwist(2, "Sharper Teeth", "Enemies hit a fifth harder, and guardians rise into a second phase at half health."),
            new WheelTwist(3, "The Crowded Dark", "Fights often bring one more foe."),
            new WheelTwist(4, "Wrathful Guardians", "Guardians and the foes at the throne grow a quarter stronger again."),
            new WheelTwist(5, "Cold Hearths", $"The inn charges {ColdHearthRestCost} gold a night."),
        };

        public const int NormalRestCost = 10;
        public const int ColdHearthRestCost = 30;

        // Every turn past the last twist: enemies gain this much more health and damage.
        public const int GrindPercentPerTurn = 10;

        public const int RewardPercentPerTurn = 15;

        public const int CrowdedChancePercent = 40;

        // From this turn on, each floor guardian changes how it fights once it drops below half health.
        public const int SecondPhaseTurn = 2;

        // Alhuezo and the shadow already grow with the hero, so they take only this share of each twist.
        public const int ThroneSharePercent = 40;

        public static int LastTwistTurn => Twists[^1].Turn;

        public static IEnumerable<WheelTwist> InForce(int turn) => Twists.Where(twist => twist.Turn <= turn);

        public static WheelTwist? AddedAt(int turn) => Twists.FirstOrDefault(twist => twist.Turn == turn);

        // What the grind adds past the last twist, for the town and the turn prompt.
        public static string GrindLine(int turn) =>
            $"The wheel grinds: enemies +{GrindPercentPerTurn * (turn - LastTwistTurn)}% health and damage.";

        public static int HealthPercent(int turn, bool mighty)
        {
            int percent = 100;
            if (turn >= 1) percent += 25;
            if (turn >= 4 && mighty) percent += 25;
            return percent + Grind(turn);
        }

        public static int DamagePercent(int turn, bool mighty)
        {
            int percent = 100;
            if (turn >= 2) percent += 20;
            if (turn >= 4 && mighty) percent += 25;
            return percent + Grind(turn);
        }

        private static int Grind(int turn) => Math.Max(0, turn - LastTwistTurn) * GrindPercentPerTurn;

        // Makes a freshly made enemy as strong as this turn of the wheel asks.
        public static void Strengthen(Enemy enemy, int turn)
        {
            enemy.WheelTurn = Math.Max(0, turn);
            if (turn <= 0)
                return;

            bool mighty = enemy.IsGuardian || enemy is Boss;
            int share = enemy is Alhuezo or ShadowSelf ? ThroneSharePercent : 100;
            int health = 100 + (HealthPercent(turn, mighty) - 100) * share / 100;
            int damage = 100 + (DamagePercent(turn, mighty) - 100) * share / 100;
            enemy.MaxHealth = Math.Max(1, enemy.MaxHealth * health / 100);
            enemy.CurrentHealth = enemy.MaxHealth;
            enemy.BaseDamage = Math.Max(1, enemy.BaseDamage * damage / 100);
        }

        public static bool BringsExtraFoe(int turn) => turn >= 3 && Random.Shared.Next(100) < CrowdedChancePercent;

        public static int RestCost(int turn) => turn >= 5 ? ColdHearthRestCost : NormalRestCost;

        public static int RewardPercent(int turn) => 100 + RewardPercentPerTurn * Math.Max(0, turn);
    }
}
