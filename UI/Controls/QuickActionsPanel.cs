/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `QuickActionsPanel.cs` sits in the bottom-right corner beside the command box.
    Outside a fight it holds the Inventory and Gear buttons; during a fight it
    switches to Attack, Special, Flee and Item. Every button sends the same
    command the player could type, so it works wherever that command does.
 */
using System;
using System.Drawing;
using System.Windows.Forms;
using ARDO.UI.Display;

namespace ARDO.UI.Controls
{
    public class QuickActionsPanel : UserControl
    {
        private const string PackCaption = "Pack and gear (or type i / g)";
        private const string FightCaption = "Fight (or type 1 - 4)";

        private Label _caption = null!;
        private TableLayoutPanel _packButtons = null!;
        private TableLayoutPanel _fightButtons = null!;

        // Raised with the command text, e.g. "inventory" or "attack".
        public event Action<string>? CommandChosen;

        public bool InFightMode { get; private set; }

        public QuickActionsPanel()
        {
            BuildInterface();
        }

        private void BuildInterface()
        {
            BackColor = Color.Black;
            Dock = DockStyle.Fill;
            BorderStyle = BorderStyle.FixedSingle;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Black
            };

            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));

            _caption = new Label
            {
                Dock = DockStyle.Fill,
                Text = PackCaption,
                ForeColor = Color.LightGray,
                BackColor = Color.Black,
                Font = UiFonts.Mono(9.5f),
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0)
            };

            _packButtons = ButtonRow(10f, ("Inventory", "inventory"), ("Gear", "gear"));
            _fightButtons = ButtonRow(9f, ("Attack", "attack"), ("Special", "special"), ("Flee", "flee"), ("Item", "item"));
            _fightButtons.Visible = false;

            // Both rows share one cell; only one is visible at a time.
            var buttonHost = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0), BackColor = Color.Black };
            buttonHost.Controls.Add(_packButtons);
            buttonHost.Controls.Add(_fightButtons);

            layout.Controls.Add(_caption, 0, 0);
            layout.Controls.Add(buttonHost, 0, 1);

            Controls.Add(layout);
            SetActionsEnabled(false);
        }

        // Four buttons share the width that two have outside a fight, so they use a smaller font.
        private TableLayoutPanel ButtonRow(float fontSize, params (string Text, string Command)[] buttons)
        {
            var row = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = buttons.Length,
                RowCount = 1,
                Margin = new Padding(0),
                BackColor = Color.Black
            };

            foreach ((string text, string command) in buttons)
            {
                row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / buttons.Length));
                row.Controls.Add(MakeButton(text, command, fontSize));
            }

            return row;
        }

        private Button MakeButton(string text, string command, float fontSize)
        {
            var button = new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                Font = UiFonts.Mono(fontSize),
                BackColor = Color.Black,
                ForeColor = Color.LightGreen,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0),
                TabStop = false
            };

            button.FlatAppearance.BorderColor = Color.DarkGreen;
            button.Click += (_, __) => CommandChosen?.Invoke(command);
            return button;
        }

        // Swaps between the pack buttons and the fight buttons.
        public void SetFightMode(bool fighting)
        {
            if (fighting == InFightMode)
                return;

            InFightMode = fighting;
            _packButtons.Visible = !fighting;
            _fightButtons.Visible = fighting;
            _caption.Text = fighting ? FightCaption : PackCaption;
        }

        // Off on the start menus, before a hero is loaded.
        public void SetActionsEnabled(bool enabled)
        {
            foreach (TableLayoutPanel row in new[] { _packButtons, _fightButtons })
            {
                foreach (Control control in row.Controls)
                {
                    control.Enabled = enabled;
                    control.ForeColor = enabled ? Color.LightGreen : Color.DimGray;
                }
            }
        }
    }
}
