using ARDO.Core.Abstracts;
using ARDO.Core.Interfaces;

namespace ARDO.Game.Enemies
{
    public abstract class Minion : Enemy
    {
        protected Minion(string name, int maxHealth, int baseDamage, int xpReward, int goldReward)
            : base(name, maxHealth, baseDamage, xpReward, goldReward)
        {
        }

        public override void ExecuteAI(ICombatant target)
        {
            Attack(target);
        }
    }
}
