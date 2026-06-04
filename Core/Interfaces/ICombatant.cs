/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `ICombatant.cs` is an interface that defines the shared combat contract 
    for heroes and enemies. It exposes combat properties and methods only.
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
