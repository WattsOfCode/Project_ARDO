/**

*/
using System;

namespace ARDO.Core.Interfaces
{
    public interface ICombatant
    {
        string Name { get; }
        int CurrentHealth { get; set; }
        int MaxHealth { get; set; }
        bool IsAlive { get; }

        void Attack(ICombatant target);
        void TakeDamage(int amount);
    }
}