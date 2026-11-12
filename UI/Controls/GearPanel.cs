/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `GearPanel.cs` is the equipment screen. The left side shows the armored figure
    with every slot labeled beside the piece it fills; the right side lists the
    hero's totals and the gear in the pack, which the player equips by number.
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ARDO.Core.Abstracts;
using ARDO.Core.Entities;
using ARDO.Core.Enums;
using ARDO.Data;
using ARDO.Game.Inventory;
using ARDO.UI.Display;

namespace ARDO.UI.Controls
{
    public class GearPanel : UserControl, IOverlayPanel
    {
        private readonly Hero _hero;
        private readonly IPlayerRepository _playerRepo;
        private readonly Action? _refreshStatus;
        private List<GameItem> _gear = new();

        private GearArtView _artView = null!;
        private Label _totalsLabel = null!;
        private ItemListBox _gearList = null!;
        private Label _feedbackLabel = null!;

        public event EventHandler? CloseRequested;

        public string CommandHint => "Gear: equip 1, unequip shield (any slot name, or a number), inventory, exit.";

        public GearPanel(Hero hero, IPlayerRepository playerRepo, Action? refreshStatus = null)
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
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(12),
                BackColor = Color.Black
            };

            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));

            var title = new Label
            {
                Dock = DockStyle.Fill,
                Text = "GEAR",
                ForeColor = Color.Gold,
                BackColor = Color.Black,
                Font = new Font("Consolas", 24, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            _artView = new GearArtView { Margin = new Padding(0) };

            var side = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Black,
                Padding = new Padding(10, 0, 0, 0)
            };
            side.RowStyles.Add(new RowStyle(SizeType.Absolute, 156F));
            side.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            side.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _totalsLabel = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.LightGreen,
                BackColor = Color.Black,
                Font = UiFonts.Mono(10.5f),
                TextAlign = ContentAlignment.TopLeft
            };

            var gearTitle = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Gear in your pack",
                ForeColor = Color.WhiteSmoke,
                BackColor = Color.Black,
                Font = UiFonts.Mono(11f, FontStyle.Bold),
                TextAlign = ContentAlignment.BottomLeft
            };

            _gearList = new ItemListBox(9.5f);

            side.Controls.Add(_totalsLabel, 0, 0);
            side.Controls.Add(gearTitle, 0, 1);
            side.Controls.Add(_gearList, 0, 2);

            _feedbackLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Pick a piece by its number: equip 1.",
                ForeColor = Color.WhiteSmoke,
                BackColor = Color.Black,
                Font = UiFonts.Mono(11f),
                TextAlign = ContentAlignment.MiddleCenter
            };

            root.SetColumnSpan(title, 2);
            root.SetColumnSpan(_feedbackLabel, 2);
            root.Controls.Add(title, 0, 0);
            root.Controls.Add(_artView, 0, 1);
            root.Controls.Add(side, 1, 1);
            root.Controls.Add(_feedbackLabel, 0, 2);

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
            if (parts.Length < 2)
            {
                ShowFeedback("Use equip 1, or unequip and a slot name, like unequip shield. Type exit to leave.");
                return;
            }

            switch (parts[0])
            {
                case "equip":
                case "e":
                    Equip(parts[1]);
                    break;
                case "unequip":
                case "u":
                    Unequip(parts[1]);
                    break;
                default:
                    ShowFeedback("Unknown command. Use equip 1, unequip shield (any slot), or exit.");
                    break;
            }
        }

        private void Equip(string choice)
        {
            if (!int.TryParse(choice, out int number) || number < 1 || number > _gear.Count)
            {
                ShowFeedback(_gear.Count == 0
                    ? "You carry no gear. The town shop sells some."
                    : $"Choose gear from 1 to {_gear.Count}.");
                return;
            }

            GameItem item = _gear[number - 1];
            if (item.IsEquipped)
            {
                ShowFeedback($"You're already wearing the {item.Name}.");
                return;
            }

            GameItem? replaced = _hero.Equipment.Equip(item);
            Save();
            ShowFeedback(replaced != null
                ? $"Equipped {item.Name} in place of {replaced.Name}."
                : $"Equipped {item.Name}.");
        }

        // Accepts a slot name ("shield", "helm", "armor") or a number from the gear list.
        private void Unequip(string choice)
        {
            EquipmentSlot? slot = choice switch
            {
                "weapon" or "sword" or "staff" => EquipmentSlot.Weapon,
                "helmet" or "helm" or "head" => EquipmentSlot.Helmet,
                "chest" or "armor" or "armour" or "body" => EquipmentSlot.Chest,
                "shield" => EquipmentSlot.Shield,
                "boots" or "feet" => EquipmentSlot.Boots,
                "ring" => EquipmentSlot.Ring,
                _ => null
            };

            GameItem? item = slot is EquipmentSlot named
                ? _hero.Equipment.ItemIn(named)
                : int.TryParse(choice, out int n) && n >= 1 && n <= _gear.Count ? _gear[n - 1] : null;

            if (item == null || !_hero.Equipment.Unequip(item))
            {
                ShowFeedback("Nothing is equipped there.");
                return;
            }

            Save();
            ShowFeedback($"Unequipped {item.Name}.");
        }

        private void Save()
        {
            _playerRepo.SaveEquipment(_hero);
            _refreshStatus?.Invoke();
        }

        private void RefreshView()
        {
            _artView.SetLabels(GearArt.LabelsFor(_hero));

            _totalsLabel.Text =
                $"Attack   {_hero.BasicAttackDamage}  (+{_hero.WeaponDamage} gear)\n" +
                $"Defense  {_hero.Defense}\n" +
                $"Crit     {_hero.CritChancePercent}%\n" +
                $"Flee     {_hero.FleeChancePercent}%\n\n" +
                $"Gold     {_hero.Gold}g";

            _gear = _hero.HeroInventory.GetItems().Where(EquipmentManager.CanEquip).ToList();
            _gearList.BeginUpdate();
            _gearList.Items.Clear();
            if (_gear.Count == 0)
            {
                _gearList.AddLine("(no gear in your pack)", Color.LightGray);
            }

            for (int i = 0; i < _gear.Count; i++)
            {
                GameItem item = _gear[i];
                string worn = item.IsEquipped ? "[E] " : string.Empty;
                _gearList.AddLine($"{i + 1}. {worn}{item.Name} ({item.Summary})", NarrativePanel.ColorFor(item.Rarity.ToTone()));
            }

            _gearList.EndUpdate();
        }

        private void ShowFeedback(string message)
        {
            _feedbackLabel.Text = message;
            RefreshView();
        }
    }
}
