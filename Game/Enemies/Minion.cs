/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `Minion.cs` is a concrete enemy subclass used as a lightweight polymorphic combat 
    unit. It uses inherited combat behavior and constructors for stats.
 */
using ARDO.Core.Abstracts;

namespace ARDO.Game.Enemies
{
    public abstract class Minion : Enemy
    {
        protected Minion(string name, int maxHealth, int baseDamage, int xpReward, int goldReward)
            : base(name, maxHealth, baseDamage, xpReward, goldReward)
        {
        }
    }
}
