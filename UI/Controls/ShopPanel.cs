/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `ShopPanel.cs` is a concrete overlay control that composes the town shop UI 
    and processes command-based buy/sell actions. It uses public methods, 
    event callbacks, and repository calls for persistence updates, while CRUD 
    is still handled through the data layer.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ARDO.Core.Abstracts;
using ARDO.Core.Entities;
using ARDO.Core.Enums;
using ARDO.Core.Factories;
using ARDO.Data;
using ARDO.Game.Inventory;
using ARDO.Game.Players;

namespace ARDO.UI.Controls
{
    public partial class ShopPanel : UserControl, IOverlayPanel
    {
        private sealed record ShopOffer(string DisplayName, int BuyPrice, Func<GameItem> CreateItem, string Description);

        private readonly Hero _hero;
        private readonly IPlayerRepository _playerRepo;
        private readonly Action? _refreshStatus;
        private readonly List<ShopOffer> _shopOffers = new();

        private Label _titleLabel = null!;
        private ItemListBox _playerItemsList = null!;
        private ItemListBox _shopItemsList = null!;
        private Label _playerGoldLabel = null!;
        private Label _feedbackLabel = null!;

        public event EventHandler? CloseRequested;

        public event Action<string>? CommandSuggested;

        public string CommandHint => "Shop commands: buy 1, sell 1, sell all, equip 1, unequip 1, exit.";

        public ShopPanel(Hero hero, IPlayerRepository playerRepo, Action? refreshStatus = null)
        {
            _hero = hero ?? throw new ArgumentNullException(nameof(hero));
            _playerRepo = playerRepo ?? throw new ArgumentNullException(nameof(playerRepo));
            _refreshStatus = refreshStatus;

            BuildStock();
            BuildInterface();
            RefreshView();
        }

        private void BuildInterface()
        {
            BackColor = Color.Black;
            Dock = DockStyle.Fill;
            BorderStyle = BorderStyle.FixedSingle;
            TabStop = false;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(18),
                BackColor = Color.Black
            };

            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76F));

            _titleLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "TOWN SHOP",
                ForeColor = Color.Gold,
                BackColor = Color.Black,
                Font = new Font("Consolas", 24, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var contentLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Black
            };

            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var playerPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Black,
                Padding = new Padding(0, 0, 10, 0)
            };

            playerPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            playerPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            playerPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));

            var playerTitle = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Player Items",
                ForeColor = Color.WhiteSmoke,
                BackColor = Color.Black,
                Font = new Font("Consolas", 13, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            _playerItemsList = new ItemListBox();
            _playerItemsList.RowClicked += index =>
            {
                List<GameItem> items = _hero.HeroInventory.GetItems();
                if (index < items.Count)
                    Suggest($"sell {index + 1}", $"Press Enter to sell {items[index].Name}.");
            };

            _playerGoldLabel = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.Gold,
                BackColor = Color.Black,
                Font = new Font("Consolas", 11, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };

            playerPanel.Controls.Add(playerTitle, 0, 0);
            playerPanel.Controls.Add(_playerItemsList, 0, 1);
            playerPanel.Controls.Add(_playerGoldLabel, 0, 2);

            var shopPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Black,
                Padding = new Padding(10, 0, 0, 0)
            };

            shopPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            shopPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var shopTitle = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Shop Items",
                ForeColor = Color.WhiteSmoke,
                BackColor = Color.Black,
                Font = new Font("Consolas", 13, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            _shopItemsList = new ItemListBox();
            _shopItemsList.RowClicked += index =>
            {
                if (index < _shopOffers.Count)
                    Suggest($"buy {index + 1}", $"Press Enter to buy {_shopOffers[index].DisplayName} for {_shopOffers[index].BuyPrice}g.");
            };

            shopPanel.Controls.Add(shopTitle, 0, 0);
            shopPanel.Controls.Add(_shopItemsList, 0, 1);

            contentLayout.Controls.Add(playerPanel, 0, 0);
            contentLayout.Controls.Add(shopPanel, 1, 0);

            _feedbackLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Shop is open. Commands: buy 1, sell 1, sell all, equip 1, unequip 1, exit.",
                ForeColor = Color.WhiteSmoke,
                BackColor = Color.Black,
                Font = new Font("Consolas", 11, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var shopStatusLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Click an item and press Enter, or type a command below.",
                ForeColor = Color.LightGray,
                BackColor = Color.Black,
                Font = new Font("Consolas", 10, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var statusPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Black
            };
            statusPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
            statusPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
            statusPanel.Controls.Add(_feedbackLabel, 0, 0);
            statusPanel.Controls.Add(shopStatusLabel, 0, 1);

            root.Controls.Add(_titleLabel, 0, 0);
            root.Controls.Add(contentLayout, 0, 1);
            root.Controls.Add(statusPanel, 0, 2);

            Controls.Add(root);
        }

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
                    RarityType.Rare, 180, 22, ItemEffect.Keen));
                AddOffer(160, () => new Armor("Cathedral Ward", "Vestments that turn the first blow aside.", RarityType.Rare, 160, 13, EquipmentSlot.Chest, ItemEffect.Warded));
                AddOffer(170, () => new Armor("Cathedral Bulwark", "A tower shield from the cathedral gates.", RarityType.Rare, 170, 8, EquipmentSlot.Shield, ItemEffect.Bulwark));
                AddOffer(140, () => new Armor("Pilgrim's Striders", "Boots for those who know when to leave.", RarityType.Rare, 140, 5, EquipmentSlot.Boots, ItemEffect.Swift));
                AddOffer(150, () => new Armor("Signet of the Keen", "The seal of a dead order of duelists.", RarityType.Rare, 150, 3, EquipmentSlot.Ring, ItemEffect.Keen));
            }
        }

        // Mages are offered a staff and everyone else a blade, at the same price and power.
        private Weapon ClassWeapon(
            (string Name, string Description) blade,
            (string Name, string Description) staff,
            RarityType rarity, int value, int damage, ItemEffect effect = ItemEffect.None)
        {
            (string name, string description) = _hero is Mage ? staff : blade;
            return new Weapon(name, description, rarity, value, damage, effect);
        }

        // A click puts the command in the box; Enter runs it.
        private static string Items(int count) => count == 1 ? "1 item" : $"{count} items";

        private void Suggest(string command, string message)
        {
            _feedbackLabel.Text = message;
            CommandSuggested?.Invoke(command);
        }

        private void AddOffer(int price, Func<GameItem> createItem)
        {
            GameItem sample = createItem();
            _shopOffers.Add(new ShopOffer(sample.Name, price, createItem, sample.Summary));
        }

        public void ProcessCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return;

            string input = command.Trim();

            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                RequestClose();
                return;
            }

            if (input.Equals("sell all", StringComparison.OrdinalIgnoreCase) || input.Equals("sellall", StringComparison.OrdinalIgnoreCase))
            {
                SellAllItems();
                return;
            }

            string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                ShowFeedback("Use buy 1, sell 1, sell all, equip 1, unequip 1, or type exit.");
                return;
            }

            string action = parts[0].ToLowerInvariant();
            string indexText = parts[1];
            if (!int.TryParse(indexText, out int selection))
            {
                ShowFeedback("That choice needs a number, like buy 1, sell 2 or equip 3.");
                return;
            }

            switch (action)
            {
                case "buy":
                case "b":
                    BuyItem(selection);
                    break;
                case "sell":
                case "s":
                    SellItem(selection);
                    break;
                case "equip":
                case "e":
                    EquipItem(selection);
                    break;
                case "unequip":
                case "u":
                    UnequipItem(selection);
                    break;
                default:
                    ShowFeedback("Unknown command. Use buy 1, sell 1, sell all, equip 1, unequip 1, or exit.");
                    break;
            }
        }

        private void BuyItem(int selection)
        {
            try
            {
                if (selection < 1 || selection > _shopOffers.Count)
                {
                    ShowFeedback($"Choose a shop item from 1 to {_shopOffers.Count}.");
                    return;
                }

                ShopOffer offer = _shopOffers[selection - 1];
                if (_hero.Gold < offer.BuyPrice)
                {
                    ShowFeedback("You do not have enough gold for that purchase.");
                    return;
                }

                GameItem item = offer.CreateItem();
                if (!_hero.HeroInventory.AddItem(item))
                {
                    ShowFeedback("Your pack is full.");
                    return;
                }

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
                _refreshStatus?.Invoke();
                ShowFeedback(equipped
                    ? $"Bought {item.Name} for {offer.BuyPrice} gold and equipped it."
                    : $"Bought {item.Name} for {offer.BuyPrice} gold.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ARDO][ShopPanel] BuyItem failed: {ex}");
                ShowFeedback("The shop could not complete that purchase right now.");
            }
        }

        private void SellItem(int selection)
        {
            try
            {
                List<GameItem> items = _hero.HeroInventory.GetItems();
                if (items.Count == 0)
                {
                    ShowFeedback("You have nothing to sell.");
                    return;
                }

                if (selection < 1 || selection > items.Count)
                {
                    ShowFeedback($"Choose a player item from 1 to {items.Count}.");
                    return;
                }

                GameItem item = items[selection - 1];
                if (item.IsEquipped)
                {
                    ShowFeedback($"Unequip that item first: unequip {selection}.");
                    return;
                }

                int saleValue = Math.Max(1, item.Value / 2);
                _hero.HeroInventory.RemoveItem(item);
                _playerRepo.RemoveInventoryItem(_hero.CharacterId, item.ItemId);
                _hero.Gold += saleValue;
                _playerRepo.SaveGameState(_hero);
                _refreshStatus?.Invoke();
                ShowFeedback($"Sold {item.Name} for {saleValue} gold.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ARDO][ShopPanel] SellItem failed: {ex}");
                ShowFeedback("The shop could not complete that sale right now.");
            }
        }

        private void SellAllItems()
        {
            try
            {
                List<GameItem> items = _hero.HeroInventory.GetItems();
                if (items.Count == 0)
                {
                    ShowFeedback("You have nothing to sell.");
                    return;
                }

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

                    int saleValue = Math.Max(1, item.Value / 2);
                    _hero.HeroInventory.RemoveItem(item);
                    _playerRepo.RemoveInventoryItem(_hero.CharacterId, item.ItemId);
                    _hero.Gold += saleValue;
                    totalGold += saleValue;
                    soldCount++;
                }

                if (soldCount == 0)
                {
                    ShowFeedback("Only equipped items are left. Unequip one first to sell it.");
                    return;
                }

                _playerRepo.SaveGameState(_hero);
                _refreshStatus?.Invoke();

                if (skippedEquipped > 0)
                {
                    ShowFeedback($"Sold {Items(soldCount)} for {totalGold} gold. Kept {Items(skippedEquipped)} you are wearing.");
                }
                else
                {
                    ShowFeedback($"Sold {Items(soldCount)} for {totalGold} gold.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ARDO][ShopPanel] SellAllItems failed: {ex}");
                ShowFeedback("The shop could not complete the bulk sale right now.");
            }
        }

        private void EquipItem(int selection)
        {
            GameItem? item = GetPlayerItem(selection);
            if (item == null)
                return;

            if (!EquipmentManager.CanEquip(item))
            {
                ShowFeedback($"{item.Name} can't be equipped. Only weapons and armor can.");
                return;
            }

            if (item.IsEquipped)
            {
                ShowFeedback($"{item.Name} is already equipped.");
                return;
            }

            GameItem? replaced = _hero.Equipment.Equip(item);
            _playerRepo.SaveEquipment(_hero);
            _refreshStatus?.Invoke();
            ShowFeedback(replaced != null
                ? $"Equipped {item.Name} in place of {replaced.Name}."
                : $"Equipped {item.Name}.");
        }

        private void UnequipItem(int selection)
        {
            GameItem? item = GetPlayerItem(selection);
            if (item == null)
                return;

            if (!_hero.Equipment.Unequip(item))
            {
                ShowFeedback($"{item.Name} isn't equipped.");
                return;
            }

            _playerRepo.SaveEquipment(_hero);
            _refreshStatus?.Invoke();
            ShowFeedback($"Unequipped {item.Name}.");
        }

        private GameItem? GetPlayerItem(int selection)
        {
            List<GameItem> items = _hero.HeroInventory.GetItems();
            if (selection < 1 || selection > items.Count)
            {
                ShowFeedback(items.Count == 0
                    ? "You have no items."
                    : $"Choose a player item from 1 to {items.Count}.");
                return null;
            }

            return items[selection - 1];
        }

        private void RefreshView()
        {
            _playerGoldLabel.Text = $"Gold: {_hero.Gold}g | ATK +{_hero.WeaponDamage} | DEF +{_hero.Equipment.DefenseBonus}";

            _playerItemsList.BeginUpdate();
            _playerItemsList.Items.Clear();
            List<GameItem> inventoryItems = _hero.HeroInventory.GetItems();
            if (inventoryItems.Count == 0)
            {
                _playerItemsList.AddLine("(no items)", Color.LightGray);
            }
            else
            {
                for (int i = 0; i < inventoryItems.Count; i++)
                {
                    GameItem item = inventoryItems[i];
                    string equipped = item.IsEquipped ? " [equipped]" : string.Empty;
                    string extra = string.IsNullOrEmpty(item.Summary) ? string.Empty : $" ({item.Summary})";
                    _playerItemsList.AddLine($"{i + 1}. {item}{equipped}{extra}", Color.LightGreen);
                }
            }
            _playerItemsList.EndUpdate();

            _shopItemsList.BeginUpdate();
            _shopItemsList.Items.Clear();
            for (int i = 0; i < _shopOffers.Count; i++)
            {
                ShopOffer offer = _shopOffers[i];
                _shopItemsList.AddLine($"{i + 1}. {offer.DisplayName} - {offer.BuyPrice}g ({offer.Description})", Color.LightSkyBlue);
            }
            _shopItemsList.EndUpdate();
        }

        private void ShowFeedback(string message)
        {
            _feedbackLabel.Text = message;
            RefreshView();
        }

        private void RequestClose()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
