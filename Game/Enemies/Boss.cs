using ARDO.Core.Abstracts;
using ARDO.Core.Interfaces;

namespace ARDO.Game.Enemies
{
    public class Boss : Enemy
    {
        public bool IsEnraged { get; set; } = false;

        public Boss(string name, int hp, int damage) 
            : base(name, hp, damage, 200, 150)
        {
        }

        public override void ExecuteAI(ICombatant target)
        {
            if (target == null) return;

            // Enrage phase transition checks at health thresholds
            if (CurrentHealth < (MaxHealth * 0.5f) && !IsEnraged)
            {
                IsEnraged = true;
                BaseDamage *= 2; 
            }

            Attack(target);
        }
    }
}
