/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `EnemyIntent.cs` is an enum file for what an enemy plans to do on its next turn.
    Heavy attacks are announced a turn early so the player can guard against them.
 */
namespace ARDO.Core.Enums
{
    public enum EnemyIntent
    {
        Attack,
        Heavy,
        Trick
    }
}
