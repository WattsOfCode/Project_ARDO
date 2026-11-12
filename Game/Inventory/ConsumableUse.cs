/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `ConsumableUse.cs` applies a potion or ration to the hero. Combat and the
    inventory screen both use it, so an item behaves the same everywhere.
 */
using System;
using ARDO.Core.Abstracts;
using ARDO.Core.Entities;
using ARDO.Game.Players;

namespace ARDO.Game.Inventory
{
    public static class ConsumableUse
    {
        // Mana draughts do nothing for a hero without mana.
        public static bool CanUse(Hero hero, Consumable item) => !item.RestoresMana || hero is Mage;

        // Applies the item and returns the line describing what happened.
        // The caller removes the item from the pack and the save.
        public static string Apply(Hero hero, Consumable item)
        {
            if (item.RestoresMana && hero is Mage mage)
            {
                int before = mage.CurrentMana;
                mage.CurrentMana = Math.Min(mage.MaxMana, mage.CurrentMana + item.StatModifier);
                return $"{hero.Name} drinks {item.Name} and recovers {mage.CurrentMana - before} mana.";
            }

            int healed = hero.Heal(Math.Max(1, item.HealAmount));
            return $"{hero.Name} uses {item.Name} and recovers {healed} HP.";
        }
    }
}
