/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `InventoryPanel.cs` is the pack screen. It lists everything the hero carries
    and lets the player use, equip, unequip or drop items by typed command.
    Selling stays in the town shop.
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ARDO.Core.Abstracts;
using ARDO.Core.Entities;
using ARDO.Core.Enums;
using ARDO.Data;
using ARDO.Game.Inventory;
using ARDO.UI.Display;

namespace ARDO.UI.Controls
{
    public class InventoryPanel : UserControl, IOverlayPanel
    {
        private readonly Hero _hero;
        private readonly IPlayerRepository _playerRepo;
        private readonly Action? _refreshStatus;

        private ItemListBox _itemsList = null!;
        private Label _packLabel = null!;
        private Label _feedbackLabel = null!;

        public event EventHandler? CloseRequested;

        public event Action<string>? CommandSuggested;

        public string CommandHint => "Inventory: use 1, equip 1, unequip 1, drop 1, gear, exit.";

        public InventoryPanel(Hero hero, IPlayerRepository playerRepo, Action? refreshStatus = null)
        {
            _hero = hero ?? throw new ArgumentNullException(nameof(hero));
            _playerRepo = playerRepo ?? throw new ArgumentNullException(nameof(playerRepo));
            _refreshStatus = refreshStatus;

            BuildInterface();
            RefreshView();
        }

        private void BuildInterface()
        {
            BackColor = Color.Black;
            Dock = DockStyle.Fill;
            BorderStyle = BorderStyle.FixedSingle;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(18),
                BackColor = Color.Black
            };

            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));

            var title = new Label
            {
                Dock = DockStyle.Fill,
                Text = "INVENTORY",
                ForeColor = Color.Gold,
                BackColor = Color.Black,
                Font = new Font("Consolas", 24, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            _packLabel = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.Gold,
                BackColor = Color.Black,
                Font = UiFonts.Mono(11f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _itemsList = new ItemListBox();
            _itemsList.RowClicked += index =>
            {
                List<GameItem> items = _hero.HeroInventory.GetItems();
                if (index >= items.Count)
                    return;

                GameItem item = items[index];
                (string verb, string action) = !EquipmentManager.CanEquip(item) ? ("use", "use")
                    : item.IsEquipped ? ("unequip", "take off") : ("equip", "equip");
                _feedbackLabel.Text = $"Press Enter to {action} {item.Name}.";
                CommandSuggested?.Invoke($"{verb} {index + 1}");
            };

            _feedbackLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Your pack is open. Commands are typed in the box below.",
                ForeColor = Color.WhiteSmoke,
                BackColor = Color.Black,
                Font = UiFonts.Mono(11f),
                TextAlign = ContentAlignment.MiddleCenter
            };

            root.Controls.Add(title, 0, 0);
            root.Controls.Add(_packLabel, 0, 1);
            root.Controls.Add(_itemsList, 0, 2);
            root.Controls.Add(_feedbackLabel, 0, 3);

            Controls.Add(root);
        }

        public void ProcessCommand(string command)
        {
            string input = command.Trim().ToLowerInvariant();

            if (input is "exit" or "back" or "close")
            {
                CloseRequested?.Invoke(this, EventArgs.Empty);
                return;
            }

            string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2 || !int.TryParse(parts[1], out int selection))
            {
                ShowFeedback("Use use 1, equip 1, unequip 1 or drop 1 (the item's number), or exit.");
                return;
            }

            GameItem? item = GetItem(selection);
            if (item == null)
                return;

            switch (parts[0])
            {
                case "use":
                case "u":
                    UseItem(item);
                    break;
                case "equip":
                case "e":
                    EquipItem(item);
                    break;
                case "unequip":
                    UnequipItem(item);
                    break;
                case "drop":
                case "d":
                    DropItem(item);
                    break;
                default:
                    ShowFeedback("Unknown command. Use use 1, equip 1, unequip 1, drop 1, or exit.");
                    break;
            }
        }

        private void UseItem(GameItem item)
        {
            if (item is not Consumable consumable)
            {
                ShowFeedback(EquipmentManager.CanEquip(item)
                    ? $"{item.Name} is gear. Try equip instead."
                    : $"{item.Name} can't be used.");
                return;
            }

            if (!ConsumableUse.CanUse(_hero, consumable))
            {
                ShowFeedback($"{item.Name} would do nothing for you.");
                return;
            }

            string result = ConsumableUse.Apply(_hero, consumable);
            _hero.HeroInventory.RemoveItem(consumable);
            _playerRepo.RemoveInventoryItem(_hero.CharacterId, consumable.ItemId);
            _playerRepo.SaveGameState(_hero);
            _refreshStatus?.Invoke();
            ShowFeedback(result);
        }

        private void EquipItem(GameItem item)
        {
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

        private void UnequipItem(GameItem item)
        {
            if (!_hero.Equipment.Unequip(item))
            {
                ShowFeedback($"{item.Name} isn't equipped.");
                return;
            }

            _playerRepo.SaveEquipment(_hero);
            _refreshStatus?.Invoke();
            ShowFeedback($"Unequipped {item.Name}.");
        }

        private void DropItem(GameItem item)
        {
            if (item.IsEquipped)
            {
                ShowFeedback($"Unequip {item.Name} before dropping it.");
                return;
            }

            _hero.HeroInventory.RemoveItem(item);
            _playerRepo.RemoveInventoryItem(_hero.CharacterId, item.ItemId);
            ShowFeedback($"You leave the {item.Name} behind.");
        }

        private GameItem? GetItem(int selection)
        {
            List<GameItem> items = _hero.HeroInventory.GetItems();
            if (selection >= 1 && selection <= items.Count)
                return items[selection - 1];

            ShowFeedback(items.Count == 0 ? "Your pack is empty." : $"Choose an item from 1 to {items.Count}.");
            return null;
        }

        private void RefreshView()
        {
            List<GameItem> items = _hero.HeroInventory.GetItems();
            _packLabel.Text = $"Pack {items.Count}/{_hero.HeroInventory.Capacity}   Gold {_hero.Gold}g   " +
                              $"HP {_hero.CurrentHealth}/{_hero.MaxHealth}";

            _itemsList.BeginUpdate();
            _itemsList.Items.Clear();
            if (items.Count == 0)
            {
                _itemsList.AddLine("(your pack is empty)", Color.LightGray);
            }

            for (int i = 0; i < items.Count; i++)
            {
                GameItem item = items[i];
                string extra = string.IsNullOrEmpty(item.Summary) ? string.Empty : $" ({item.Summary})";
                string equipped = item.IsEquipped ? "  [equipped]" : string.Empty;
                _itemsList.AddLine($"{i + 1,2}. {item}{extra}{equipped}", NarrativePanel.ColorFor(item.Rarity.ToTone()));
            }

            _itemsList.EndUpdate();
        }

        private void ShowFeedback(string message)
        {
            _feedbackLabel.Text = message;
            RefreshView();
        }
    }
}
