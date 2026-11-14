/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `Weapon.cs` is a concrete item subclass that adds attack bonuses to the 
    shared game item model. It uses constructors and inherited properties.
 */
using ARDO.Core.Enums;

namespace ARDO.Core.Entities
{
    public class Weapon : GameItem
    {
        public int DamageBonus { get; set; }

        public Weapon(
            string name,
            string description,
            RarityType rarity,
            int value,
            int damageBonus,
            ItemEffect effect = ItemEffect.None)

            : base(
                name,
                description,
                ItemType.Weapon,
                rarity,
                value,
                damageBonus,
                EquipmentSlot.Weapon)
        {
            DamageBonus = damageBonus;
            StatModifier = damageBonus;
            Effect = effect;
        }

        public override string Summary
        {
            get
            {
                string text = Effect == ItemEffect.None ? $"+{DamageBonus} ATK" : $"+{DamageBonus} ATK, {EffectText}";
                return WearsOut ? $"{text}, breaks after {UsesLeft} more {(UsesLeft == 1 ? "fight" : "fights")}" : text;
            }
        }

        // Every new hero sets out with one. It does the job until something better turns up, or it breaks.
        public const int StarterStaffUses = 10;

        public static Weapon StarterStaff() => new("Wooden Staff", "A walking staff from the inn's woodpile. It won't last.", RarityType.Common, 1, 3)
        {
            UsesLeft = StarterStaffUses
        };
    }
}
