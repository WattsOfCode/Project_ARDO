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
using ARDO.Data.SQLite;

namespace ARDO.UI.Controls
{
    public partial class ShopPanel : UserControl
    {
        private sealed record ShopOffer(string DisplayName, int BuyPrice, Func<GameItem> CreateItem, string Description);

        private readonly Hero _hero;
        private readonly PlayerRepository _playerRepo;
        private readonly Action? _refreshStatus;
        private readonly List<ShopOffer> _shopOffers = new();

        private Label _titleLabel = null!;
        private ListBox _playerItemsList = null!;
        private ListBox _shopItemsList = null!;
        private Label _playerGoldLabel = null!;
        private Label _feedbackLabel = null!;

        public event EventHandler? CloseRequested;

        public ShopPanel(Hero hero, PlayerRepository playerRepo, Action? refreshStatus = null)
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

            _playerItemsList = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                ForeColor = Color.LightGreen,
                Font = new Font("Consolas", 11, FontStyle.Regular),
                BorderStyle = BorderStyle.FixedSingle
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

            _shopItemsList = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                ForeColor = Color.LightSkyBlue,
                Font = new Font("Consolas", 11, FontStyle.Regular),
                BorderStyle = BorderStyle.FixedSingle
            };

            shopPanel.Controls.Add(shopTitle, 0, 0);
            shopPanel.Controls.Add(_shopItemsList, 0, 1);

            contentLayout.Controls.Add(playerPanel, 0, 0);
            contentLayout.Controls.Add(shopPanel, 1, 0);

            _feedbackLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Shop is open. Use the input panel below to trade items or leave the shop.",
                ForeColor = Color.WhiteSmoke,
                BackColor = Color.Black,
                Font = new Font("Consolas", 11, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var shopStatusLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Item lists are shown here; commands are entered in the input panel below.",
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

        private void BuildStock()
        {
            _shopOffers.Add(new ShopOffer(
                "Iron Blade",
                40,
                () => new Weapon("Iron Blade", "A sturdy blade from the Oakhaven merchant.", RarityType.Common, 40, 10),
                "Sharpened weapon with steady attack gain."));

            _shopOffers.Add(new ShopOffer(
                "Reinforced Mail",
                30,
                () => new Armor("Reinforced Mail", "Armor fit for the next descent.", RarityType.Common, 30, 5, EquipmentSlot.Chest),
                "Reinforced mail with a defense bonus."));

            _shopOffers.Add(new ShopOffer(
                "Tonic of Hardening",
                50,
                () => new Consumable("Tonic of Hardening", "A one-time draught that steadies the body.", RarityType.Common, 50, 25),
                "Single-use tonic that restores health in battle."));

            _shopOffers.Add(new ShopOffer(
                "Travel Ration",
                20,
                () => new Consumable("Travel Ration", "A simple ration for desperate moments.", RarityType.Common, 20, 15),
                "Low-cost consumable with modest healing."));
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
                ShowFeedback("Use buy 1, sell 1, sell all, or type exit.");
                return;
            }

            string action = parts[0].ToLowerInvariant();
            string indexText = parts[1];
            if (!int.TryParse(indexText, out int selection))
            {
                ShowFeedback("That choice needs a number, like buy 1 or sell 2.");
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
                default:
                    ShowFeedback("Unknown command. Use buy 1, sell 1, sell all, or exit.");
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
                    ShowFeedback("Your inventory is full.");
                    return;
                }

                _hero.Gold -= offer.BuyPrice;
                _playerRepo.SaveInventoryItem(_hero.CharacterId, item);
                _playerRepo.SaveGameState(_hero, _hero.ActiveStoryNode);
                RefreshView();
                _refreshStatus?.Invoke();
                ShowFeedback($"Bought {item.Name} for {offer.BuyPrice} gold.");
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
                    ShowFeedback("Unequip that item before selling it.");
                    return;
                }

                int saleValue = Math.Max(1, item.Value / 2);
                _hero.HeroInventory.RemoveItem(item);
                _playerRepo.RemoveInventoryItem(_hero.CharacterId, item.ItemId);
                _hero.Gold += saleValue;
                _playerRepo.SaveGameState(_hero, _hero.ActiveStoryNode);
                RefreshView();
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
                    ShowFeedback("Unequip your items before selling them.");
                    return;
                }

                _playerRepo.SaveGameState(_hero, _hero.ActiveStoryNode);
                RefreshView();
                _refreshStatus?.Invoke();

                if (skippedEquipped > 0)
                {
                    ShowFeedback($"Sold {soldCount} item(s) for {totalGold} gold. {skippedEquipped} equipped item(s) were kept.");
                }
                else
                {
                    ShowFeedback($"Sold {soldCount} item(s) for {totalGold} gold.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ARDO][ShopPanel] SellAllItems failed: {ex}");
                ShowFeedback("The shop could not complete the bulk sale right now.");
            }
        }

        private void RefreshView()
        {
            _playerGoldLabel.Text = $"Player Gold: {_hero.Gold}g";

            _playerItemsList.BeginUpdate();
            _playerItemsList.Items.Clear();
            List<GameItem> inventoryItems = _hero.HeroInventory.GetItems();
            if (inventoryItems.Count == 0)
            {
                _playerItemsList.Items.Add("(no items)");
            }
            else
            {
                for (int i = 0; i < inventoryItems.Count; i++)
                {
                    GameItem item = inventoryItems[i];
                    string equipped = item.IsEquipped ? " [equipped]" : string.Empty;
                    string extra = item.ItemType == ItemType.Consumable ? $" [heal {item.HealAmount}]" : string.Empty;
                    _playerItemsList.Items.Add($"{i + 1}. {item}{equipped}{extra}");
                }
            }
            _playerItemsList.EndUpdate();

            _shopItemsList.BeginUpdate();
            _shopItemsList.Items.Clear();
            for (int i = 0; i < _shopOffers.Count; i++)
            {
                ShopOffer offer = _shopOffers[i];
                _shopItemsList.Items.Add($"{i + 1}. {offer.DisplayName} - {offer.BuyPrice}g");
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
