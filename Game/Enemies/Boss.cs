/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `Boss.cs` is a concrete enemy subclass that adds enraged boss behavior to the 
    polymorphic combat system. It overrides enemy methods, uses constructors 
    for stats.
 */
using ARDO.Core.Abstracts;
using ARDO.Game.Progression;

namespace ARDO.Game.Enemies
{
    public class Boss : Enemy
    {
        public bool IsEnraged { get; private set; }

        // A guardian on a later turn of the wheel changes how it fights below half health.
        public bool InSecondPhase { get; private set; }

        protected virtual bool HasSecondPhase => false;

        protected virtual void BeginSecondPhase()
        {
        }

        public Boss(string name, int hp, int damage)
            : base(name, hp, damage, 200, 150)
        { }

        public override bool IsGuardian => true;

        // Below half health a boss flies into a rage and hits harder for the rest of the fight.
        public override void TakeDamage(int amount)
        {
            base.TakeDamage(amount);

            if (IsAlive && !IsEnraged && CurrentHealth < MaxHealth / 2)
            {
                IsEnraged = true;
                BaseDamage = (int)(BaseDamage * 1.5);
                Announce($"{Name} is enraged! Its attacks grow stronger.");
            }

            if (IsAlive && !InSecondPhase && HasSecondPhase && WheelTurn >= WheelTurns.SecondPhaseTurn && CurrentHealth < MaxHealth / 2)
            {
                InSecondPhase = true;
                Announce("[Phase 2]");
                BeginSecondPhase();
            }
        }
    }
}
