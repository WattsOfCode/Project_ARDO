/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `QuickActionsPanel.cs` sits in the bottom-right corner beside the command box.
    Its Inventory and Gear buttons send the same commands the player could type,
    so they work wherever those commands do.
 */
using System;
using System.Drawing;
using System.Windows.Forms;
using ARDO.UI.Display;

namespace ARDO.UI.Controls
{
    public class QuickActionsPanel : UserControl
    {
        private Button _inventoryButton = null!;
        private Button _gearButton = null!;

        // Raised with the command text, e.g. "inventory".
        public event Action<string>? CommandChosen;

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
                ColumnCount = 2,
                RowCount = 2,
                BackColor = Color.Black
            };

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));

            var caption = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Pack and gear (or type i / g)",
                ForeColor = Color.LightGray,
                BackColor = Color.Black,
                Font = UiFonts.Mono(9.5f),
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0)
            };

            _inventoryButton = MakeButton("Inventory", "inventory");
            _gearButton = MakeButton("Gear", "gear");

            layout.SetColumnSpan(caption, 2);
            layout.Controls.Add(caption, 0, 0);
            layout.Controls.Add(_inventoryButton, 0, 1);
            layout.Controls.Add(_gearButton, 1, 1);

            Controls.Add(layout);
            SetActionsEnabled(false);
        }

        private Button MakeButton(string text, string command)
        {
            var button = new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                Font = UiFonts.Mono(10f),
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

        // Off on the start menus, before a hero is loaded.
        public void SetActionsEnabled(bool enabled)
        {
            _inventoryButton.Enabled = enabled;
            _gearButton.Enabled = enabled;
            _inventoryButton.ForeColor = enabled ? Color.LightGreen : Color.DimGray;
            _gearButton.ForeColor = enabled ? Color.LightGreen : Color.DimGray;
        }
    }
}
