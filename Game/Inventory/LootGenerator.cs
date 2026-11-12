/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `LootGenerator.cs` is a static utility class that rolls random loot rewards
    for combat outcomes. Deeper floors roll better rarities, and rare gear comes
    with a special effect.
 */
using System;
using ARDO.Core.Abstracts;
using ARDO.Core.Entities;
using ARDO.Core.Enums;
using ARDO.Core.Factories;
using ARDO.Game.Players;

namespace ARDO.Game.Inventory
{
    public static class LootGenerator
    {
        // Chance out of 100 for Common, Uncommon, Rare, Epic, Legendary, by floor (1 to 4).
        private static readonly int[][] RarityWeights =
        {
            new[] { 70, 22, 6, 2, 0 },
            new[] { 55, 25, 12, 6, 2 },
            new[] { 40, 28, 18, 10, 4 },
            new[] { 40, 28, 18, 10, 4 }
        };

        private static readonly double[] RarityMultiplier = { 1.0, 1.25, 1.55, 1.9, 2.4 };

        private static readonly string[] FloorMaterials = { "Iron", "Emberforged", "Hallowed", "Kingsbane" };
        private static readonly string[] WeaponNames = { "Blade", "Axe", "Spear", "Staff", "Mace" };
        private static readonly string[] ArmorNames = { "Mail", "Cuirass", "Robe", "Plate" };
        private static readonly ItemEffect[] WeaponEffects = { ItemEffect.Burning, ItemEffect.Vampiric, ItemEffect.Keen };

        // The regular drop after a won fight: often a potion, sometimes gear, sometimes nothing.
        public static GameItem? RollEncounterDrop(int floor, Hero hero)
        {
            int roll = Random.Shared.Next(100);

            if (roll < 40)
                return RollConsumable(floor, hero);

            if (roll < 62)
                return CreateWeapon(floor, RollRarity(floor, RarityType.Common));

            if (roll < 82)
                return CreateArmor(floor, RollRarity(floor, RarityType.Common));

            return null;
        }

        // Gear of at least the given rarity: guardian drops, chests, the side passage.
        public static GameItem RollGear(int floor, RarityType minimum)
        {
            RarityType rarity = RollRarity(floor, minimum);
            return Random.Shared.Next(2) == 0 ? CreateWeapon(floor, rarity) : CreateArmor(floor, rarity);
        }

        public static RarityType RollRarity(int floor, RarityType minimum)
        {
            int[] weights = RarityWeights[Math.Clamp(floor, 1, RarityWeights.Length) - 1];
            int roll = Random.Shared.Next(100);
            int rarity = 0;

            while (rarity < weights.Length - 1 && roll >= weights[rarity])
            {
                roll -= weights[rarity];
                rarity++;
            }

            return (RarityType)Math.Max(rarity, (int)minimum);
        }

        public static Weapon CreateWeapon(int floor, RarityType rarity)
        {
            floor = Math.Clamp(floor, 1, FloorMaterials.Length);
            int damage = (int)Math.Round((3 + 4 * floor) * RarityMultiplier[(int)rarity]);
            ItemEffect effect = rarity >= RarityType.Rare ? Pick(WeaponEffects) : ItemEffect.None;

            string name = $"{FloorMaterials[floor - 1]} {Pick(WeaponNames)}";
            name = effect switch
            {
                ItemEffect.Burning => $"{name} of Embers",
                ItemEffect.Vampiric => $"{name} of the Leech",
                ItemEffect.Keen => $"Keen {name}",
                _ => name
            };

            return new Weapon(name, "Pulled from the dark below Oakhaven.", rarity, PriceFor(damage, rarity), damage, effect);
        }

        public static Armor CreateArmor(int floor, RarityType rarity)
        {
            floor = Math.Clamp(floor, 1, FloorMaterials.Length);
            int defense = (int)Math.Round((1 + 2.5 * floor) * RarityMultiplier[(int)rarity]);
            ItemEffect effect = rarity >= RarityType.Rare ? ItemEffect.Warded : ItemEffect.None;

            string name = $"{FloorMaterials[floor - 1]} {Pick(ArmorNames)}";
            if (effect == ItemEffect.Warded)
            {
                name = $"Warded {name}";
            }

            return new Armor(name, "Pulled from the dark below Oakhaven.", rarity, PriceFor(defense * 2, rarity), defense, EquipmentSlot.Chest, effect);
        }

        private static GameItem RollConsumable(int floor, Hero hero)
        {
            if (hero is Mage && Random.Shared.Next(100) < 35)
                return ItemFactory.CreateManaDraught();

            return floor >= 3 ? ItemFactory.CreateGreaterHealthPotion() : ItemFactory.CreateHealthPotion();
        }

        private static int PriceFor(int stat, RarityType rarity)
        {
            return (int)Math.Round(stat * 4 * (1 + (int)rarity * 0.5));
        }

        private static T Pick<T>(T[] options) => options[Random.Shared.Next(options.Length)];
    }
}
