/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `ItemEffect.cs` is an enum file that lists the special effects an item can carry,
    such as a burning weapon or a mana potion. Saved with the item.
 */
namespace ARDO.Core.Enums
{
    public enum ItemEffect
    {
        None,
        Burning,      // weapon: sets the target burning on hit
        Vampiric,     // weapon: heals the hero for part of the damage dealt
        Keen,         // weapon: +10% critical hit chance
        Warded,       // armor: the first hit of each fight is halved
        RestoresMana  // consumable: restores mana instead of health
    }
}
