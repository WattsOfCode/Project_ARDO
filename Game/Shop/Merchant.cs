/**
 * Name: John Deardorff
 * Date: 11/14/26
 * Project ARDO
 *
 * Class description:
 * `Merchant.cs` is the town shop's model: what the merchant stocks for this hero
    and how buying, selling and equipping work. It changes the hero and saves
    through the repository, and answers each action with the line to show, so
    ShopPanel only draws the shop and reads the player's commands.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using ARDO.Core.Abstracts;
using ARDO.Core.Entities;
using ARDO.Core.Enums;
using ARDO.Core.Factories;
using ARDO.Data;
using ARDO.Game.Inventory;
using ARDO.Game.Players;

namespace ARDO.Game.Shop
{
    // One line of the merchant's stock. CreateItem makes a fresh item for each sale.
    public sealed record ShopOffer(string DisplayName, int BuyPrice, Func<GameItem> CreateItem, string Description);

    public class Merchant
    {
        private readonly Hero _hero;
        private readonly IPlayerRepository _playerRepo;
        private readonly List<ShopOffer> _offers = new();

        public Merchant(Hero hero, IPlayerRepository playerRepo)
        {
            _hero = hero ?? throw new ArgumentNullException(nameof(hero));
            _playerRepo = playerRepo ?? throw new ArgumentNullException(nameof(playerRepo));
            BuildStock();
        }

        public IReadOnlyList<ShopOffer> Offers => _offers;

        // The merchant pays half an item's worth, and never less than a coin.
        public static int SalePrice(GameItem item) => Math.Max(1, item.Value / 2);

        // The merchant restocks as the hero goes deeper: each cleared floor adds better gear.
        private void BuildStock()
        {
            int floor = _hero.ActiveStoryNode;

            AddOffer(30, ItemFactory.CreateHealthPotion);
            AddOffer(12, ItemFactory.CreateTravelRation);
            if (_hero is Mage)
            {
                AddOffer(30, ItemFactory.CreateManaDraught);
            }

            AddOffer(45, () => ClassWeapon(
                ("Iron Blade", "A sturdy blade from the Oakhaven merchant."),
                ("Ashwood Staff", "A plain staff, worn smooth by an apprentice's grip."),
                ("Hunter's Shortbow", "Yew and gut string. It has fed a family or two."),
                ("Twin Iron Daggers", "A matched pair, balanced for throwing or worse."),
                RarityType.Common, 45, 10));
            AddOffer(35, () => new Armor("Reinforced Mail", "Armor fit for the next descent.", RarityType.Common, 35, 5, EquipmentSlot.Chest));
            AddOffer(20, () => new Armor("Leather Cap", "Better than nothing between you and a falling rock.", RarityType.Common, 20, 2, EquipmentSlot.Helmet));
            AddOffer(30, () => new Armor("Oak Buckler", "A round shield, iron at the rim.", RarityType.Common, 30, 3, EquipmentSlot.Shield));
            AddOffer(20, () => new Armor("Traveler's Boots", "Broken in on the road to Oakhaven.", RarityType.Common, 20, 2, EquipmentSlot.Boots));
            AddOffer(25, () => new Armor("Copper Ring", "A plain band the merchant swears is lucky.", RarityType.Common, 25, 1, EquipmentSlot.Ring));

            if (floor >= 2)
            {
                AddOffer(110, () => ClassWeapon(
                    ("Emberforged Sword", "Still warm from the depths' forges."),
                    ("Cinder Staff", "An ember burns in its crown and never goes out."),
                    ("Emberstring Longbow", "The string glows faintly when drawn."),
                    ("Cinder Fangs", "Two curved blades that never quite cool."),
                    RarityType.Uncommon, 110, 16));
                AddOffer(90, () => new Armor("Ashen Plate", "Plate scorched black in the Emberward.", RarityType.Uncommon, 90, 9, EquipmentSlot.Chest));
                AddOffer(60, () => new Armor("Ashen Helm", "Soot still in the visor slits.", RarityType.Uncommon, 60, 4, EquipmentSlot.Helmet));
                AddOffer(75, () => new Armor("Emberward Kite Shield", "Tall enough to hide behind.", RarityType.Uncommon, 75, 6, EquipmentSlot.Shield));
            }

            if (floor >= 3)
            {
                AddOffer(55, ItemFactory.CreateGreaterHealthPotion);
                AddOffer(180, () => ClassWeapon(
                    ("Hallowed Greatblade", "Blessed in the Hollow Cathedral, before the fall."),
                    ("Hallowed Crozier", "A cathedral staff that still remembers its prayers."),
                    ("Hallowed Warbow", "Cut from a cathedral rafter. Its arrows find the gaps."),
                    ("Vesper Stilettos", "Thin as a prayer, and quieter."),
                    RarityType.Rare, 180, 22, ItemEffect.Keen));
                AddOffer(160, () => new Armor("Cathedral Ward", "Vestments that turn the first blow aside.", RarityType.Rare, 160, 13, EquipmentSlot.Chest, ItemEffect.Warded));
                AddOffer(170, () => new Armor("Cathedral Bulwark", "A tower shield from the cathedral gates.", RarityType.Rare, 170, 8, EquipmentSlot.Shield, ItemEffect.Bulwark));
                AddOffer(140, () => new Armor("Pilgrim's Striders", "Boots for those who know when to leave.", RarityType.Rare, 140, 5, EquipmentSlot.Boots, ItemEffect.Swift));
                AddOffer(150, () => new Armor("Signet of the Keen", "The seal of a dead order of duelists.", RarityType.Rare, 150, 3, EquipmentSlot.Ring, ItemEffect.Keen));
            }
        }

        // Each class is offered its own weapon (blade, staff, bow or daggers) at the same price and power.
        private Weapon ClassWeapon(
            (string Name, string Description) blade,
            (string Name, string Description) staff,
            (string Name, string Description) bow,
            (string Name, string Description) daggers,
            RarityType rarity, int value, int damage, ItemEffect effect = ItemEffect.None)
        {
            (string name, string description) = _hero switch
            {
                Mage => staff,
                Archer => bow,
                Rogue => daggers,
                _ => blade
            };
            return new Weapon(name, description, rarity, value, damage, effect);
        }

        private void AddOffer(int price, Func<GameItem> createItem)
        {
            GameItem sample = createItem();
            _offers.Add(new ShopOffer(sample.Name, price, createItem, sample.Summary));
        }

        private static string Items(int count) => count == 1 ? "1 item" : $"{count} items";

        public string Buy(int selection)
        {
            if (selection < 1 || selection > _offers.Count)
                return $"Choose a shop item from 1 to {_offers.Count}.";

            ShopOffer offer = _offers[selection - 1];
            if (_hero.Gold < offer.BuyPrice)
                return "You do not have enough gold for that purchase.";

            GameItem item = offer.CreateItem();
            if (!_hero.HeroInventory.AddItem(item))
                return "Your pack is full.";

            _hero.Gold -= offer.BuyPrice;
            _playerRepo.SaveInventoryItem(_hero.CharacterId, item);

            bool equipped = false;
            if (_hero.Equipment.IsUpgrade(item))
            {
                _hero.Equipment.Equip(item);
                _playerRepo.SaveEquipment(_hero);
                equipped = true;
            }

            _playerRepo.SaveGameState(_hero);
            return equipped
                ? $"Bought {item.Name} for {offer.BuyPrice} gold and equipped it."
                : $"Bought {item.Name} for {offer.BuyPrice} gold.";
        }

        public string Sell(int selection)
        {
            List<GameItem> items = _hero.HeroInventory.GetItems();
            if (items.Count == 0)
                return "You have nothing to sell.";

            if (selection < 1 || selection > items.Count)
                return $"Choose a player item from 1 to {items.Count}.";

            GameItem item = items[selection - 1];
            if (item.IsEquipped)
                return $"Unequip that item first: unequip {selection}.";

            int saleValue = SalePrice(item);
            _hero.HeroInventory.RemoveItem(item);
            _playerRepo.RemoveInventoryItem(_hero.CharacterId, item.ItemId);
            _hero.Gold += saleValue;
            _playerRepo.SaveGameState(_hero);
            return $"Sold {item.Name} for {saleValue} gold.";
        }

        public string SellAll()
        {
            List<GameItem> items = _hero.HeroInventory.GetItems();
            if (items.Count == 0)
                return "You have nothing to sell.";

            int soldCount = 0;
            int totalGold = 0;
            int skippedEquipped = 0;

            foreach (GameItem item in items.ToList())
            {
                if (item.IsEquipped)
                {
                    skippedEquipped++;
                    continue;
                }

                int saleValue = SalePrice(item);
                _hero.HeroInventory.RemoveItem(item);
                _playerRepo.RemoveInventoryItem(_hero.CharacterId, item.ItemId);
                _hero.Gold += saleValue;
                totalGold += saleValue;
                soldCount++;
            }

            if (soldCount == 0)
                return "Only equipped items are left. Unequip one first to sell it.";

            _playerRepo.SaveGameState(_hero);

            return skippedEquipped > 0
                ? $"Sold {Items(soldCount)} for {totalGold} gold. Kept {Items(skippedEquipped)} you are wearing."
                : $"Sold {Items(soldCount)} for {totalGold} gold.";
        }

        public string Equip(int selection)
        {
            if (PlayerItem(selection, out GameItem? item) is string problem)
                return problem;

            if (!EquipmentManager.CanEquip(item!))
                return $"{item!.Name} can't be equipped. Only weapons and armor can.";

            if (item!.IsEquipped)
                return $"{item.Name} is already equipped.";

            GameItem? replaced = _hero.Equipment.Equip(item);
            _playerRepo.SaveEquipment(_hero);
            return replaced != null
                ? $"Equipped {item.Name} in place of {replaced.Name}."
                : $"Equipped {item.Name}.";
        }

        public string Unequip(int selection)
        {
            if (PlayerItem(selection, out GameItem? item) is string problem)
                return problem;

            if (!_hero.Equipment.Unequip(item!))
                return $"{item!.Name} isn't equipped.";

            _playerRepo.SaveEquipment(_hero);
            return $"Unequipped {item!.Name}.";
        }

        // The pack item picked by number, or why there isn't one.
        private string? PlayerItem(int selection, out GameItem? item)
        {
            List<GameItem> items = _hero.HeroInventory.GetItems();
            item = null;
            if (selection < 1 || selection > items.Count)
            {
                return items.Count == 0
                    ? "You have no items."
                    : $"Choose a player item from 1 to {items.Count}.";
            }

            item = items[selection - 1];
            return null;
        }
    }
}
