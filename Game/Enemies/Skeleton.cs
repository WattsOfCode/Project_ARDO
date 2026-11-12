/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `Skeleton.cs` is a concrete enemy subclass that supplies another polymorphic 
    combat variant for dungeon encounters. It uses inherited members and 
    constructor-based stats.
 */
namespace ARDO.Game.Enemies
{
    // A skeleton pulls itself back together the first time it falls.
    public class Skeleton : Minion
    {
        private bool _hasReassembled;

        public Skeleton() : base("Skeleton", 55, 8, 25, 15)
        {
        }

        public override string ArtKey => "skeleton";

        public override string AttackLine => "rattles forward with a bone-sharp strike.";

        public override void TakeDamage(int amount)
        {
            base.TakeDamage(amount);

            if (IsDead && !_hasReassembled)
            {
                _hasReassembled = true;
                CurrentHealth = MaxHealth / 3;
                Announce($"{Name}'s bones scatter... then drag themselves back together! ({CurrentHealth} HP)");
            }
        }
    }
}
