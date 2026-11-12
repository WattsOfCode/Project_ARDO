/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `BurnEffect.cs` is a small status-effect class shared by heroes and enemies.
    A burn deals damage at the end of each round until it runs out.
 */
using System;

namespace ARDO.Core.Entities
{
    public sealed class BurnEffect
    {
        public int TurnsLeft { get; private set; }

        public int DamagePerTurn { get; private set; }

        public bool IsActive => TurnsLeft > 0;

        // A new burn refreshes the old one instead of stacking with it.
        public void Apply(int turns, int damagePerTurn)
        {
            TurnsLeft = Math.Max(TurnsLeft, turns);
            DamagePerTurn = Math.Max(DamagePerTurn, damagePerTurn);
        }

        // Returns this round's burn damage (0 when not burning).
        public int Tick()
        {
            if (!IsActive)
                return 0;

            int damage = DamagePerTurn;
            TurnsLeft--;

            if (TurnsLeft == 0)
            {
                DamagePerTurn = 0;
            }

            return damage;
        }

        public void Clear()
        {
            TurnsLeft = 0;
            DamagePerTurn = 0;
        }
    }
}
