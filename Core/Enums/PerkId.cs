/**
 * Name: John Deardorff
 * Date: 11/13/26
 * Project ARDO
 *
 * Class description:
 * `PerkId.cs` names every perk a hero can take on a level-up. The names are
    what the save stores, so a perk keeps its name once it ships.
 */
namespace ARDO.Core.Enums
{
    public enum PerkId
    {
        // Any class.
        Toughness,
        KeenEye,
        SecondWind,
        Scavenger,
        IronSkin,
        Executioner,
        BrutalCrits,
        FleetFooted,
        QuickStudy,

        // Warrior.
        Bloodlust,
        Cleave,
        ShieldWall,
        LastStand,

        // Mage.
        ManaFlow,
        Pyromancer,
        Overcharge,
        EfficientCasting,
        MirrorWard,

        // Archer.
        SteadyAim,
        Barrage,
        BarbedArrows,
        QuickDraw,

        // Rogue.
        Ambusher,
        VirulentPoison,
        ShadowDance,
        Cutpurse,
        Evasive
    }
}
