/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `ShopPanel.cs` is a concrete overlay control that composes the town shop UI 
    and processes command-based buy/sell actions. It uses public methods and
    event callbacks; the stock and the trading itself are the Merchant's
    (Game/Shop), and CRUD is still handled through the data layer.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ARDO.Core.Abstracts;
using ARDO.UI.Display;
using ARDO.Core.Entities;
using ARDO.Core.Enums;
using ARDO.Core.Factories;
using ARDO.Data;
using ARDO.Game.Shop;

namespace ARDO.UI.Controls
{
    public partial class ShopPanel : UserControl, IOverlayPanel
    {
        private readonly Hero _hero;
        private readonly Merchant _merchant;
        private readonly Action? _refreshStatus;

        private Label _titleLabel = null!;
        private ItemListBox _playerItemsList = null!;
        private ItemListBox _shopItemsList = null!;
        private Label _playerGoldLabel = null!;
        private Label _feedbackLabel = null!;

        public event EventHandler? CloseRequested;

        public event Action<string>? CommandSuggested;

        public string CommandHint => "Shop commands: info 1, buy 1, sell 1, sell all, equip 1, unequip 1, exit.";

        public ShopPanel(Hero hero, IPlayerRepository playerRepo, Action? refreshStatus = null)
        {
            _hero = hero ?? throw new ArgumentNullException(nameof(hero));
            _merchant = new Merchant(hero, playerRepo ?? throw new ArgumentNullException(nameof(playerRepo)));
            _refreshStatus = refreshStatus;

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
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 112F));

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
                    Suggest($"sell {index + 1}", ItemDetails.Describe(_hero, items[index], $"sells for {Merchant.SalePrice(items[index])}g", "Press Enter to sell."));
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
                if (index < _merchant.Offers.Count)
                    Suggest($"buy {index + 1}", OfferDetails(index));
            };

            shopPanel.Controls.Add(shopTitle, 0, 0);
            shopPanel.Controls.Add(_shopItemsList, 0, 1);

            contentLayout.Controls.Add(playerPanel, 0, 0);
            contentLayout.Controls.Add(shopPanel, 1, 0);

            _feedbackLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "[Merchant] Alexander: \"Well met, adventurer! We have new wares. Take a gander at my new curios.\"",
                ForeColor = Color.WhiteSmoke,
                BackColor = Color.Black,
                Font = new Font("Consolas", 11, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var shopStatusLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Click an item to see what it does for you; Enter buys or sells it.",
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
            statusPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 75F));
            statusPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            statusPanel.Controls.Add(_feedbackLabel, 0, 0);
            statusPanel.Controls.Add(shopStatusLabel, 0, 1);

            root.Controls.Add(_titleLabel, 0, 0);
            root.Controls.Add(contentLayout, 0, 1);
            root.Controls.Add(statusPanel, 0, 2);

            Controls.Add(root);
        }

        // A click puts the command in the box; Enter runs it.
        private void Suggest(string command, string message)
        {
            _feedbackLabel.Text = message;
            CommandSuggested?.Invoke(command);
        }

        // What an offer does for this hero, before buying it.
        private string OfferDetails(int index)
        {
            ShopOffer offer = _merchant.Offers[index];
            return ItemDetails.Describe(_hero, offer.CreateItem(), $"{offer.BuyPrice}g", "Press Enter to buy.");
        }

        public void ProcessCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return;

            string input = command.Trim();
            int offerCount = _merchant.Offers.Count;

            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                RequestClose();
                return;
            }

            if (input.Equals("sell all", StringComparison.OrdinalIgnoreCase) || input.Equals("sellall", StringComparison.OrdinalIgnoreCase))
            {
                Trade(_merchant.SellAll, "The shop could not complete the bulk sale right now.");
                return;
            }

            string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && parts[0].ToLowerInvariant() is "info" or "look")
            {
                if (int.TryParse(parts[1], out int shown) && shown >= 1 && shown <= offerCount)
                    ShowFeedback(OfferDetails(shown - 1));
                else
                    ShowFeedback($"Type info 1 to {offerCount} to look at a shop item.");
                return;
            }

            if (parts.Length < 2)
            {
                ShowFeedback("Use buy 1, info 1, sell 1, sell all, equip 1, unequip 1, or type exit.");
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
                    Trade(() => _merchant.Buy(selection), "The shop could not complete that purchase right now.");
                    break;
                case "sell":
                case "s":
                    Trade(() => _merchant.Sell(selection), "The shop could not complete that sale right now.");
                    break;
                case "equip":
                case "e":
                    Trade(() => _merchant.Equip(selection), "That item could not be equipped right now.");
                    break;
                case "unequip":
                case "u":
                    Trade(() => _merchant.Unequip(selection), "That item could not be unequipped right now.");
                    break;
                default:
                    ShowFeedback("Unknown command. Use buy 1, sell 1, sell all, equip 1, unequip 1, or exit.");
                    break;
            }
        }

        // Runs one of the merchant's actions, then shows its answer and the hero's new totals.
        private void Trade(Func<string> action, string failure)
        {
            try
            {
                string message = action();
                _refreshStatus?.Invoke();
                ShowFeedback(message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ARDO][ShopPanel] {failure} {ex}");
                ShowFeedback(failure);
            }
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
            for (int i = 0; i < _merchant.Offers.Count; i++)
            {
                ShopOffer offer = _merchant.Offers[i];
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
