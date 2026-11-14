/**
 * Name: John Deardorff
 * Date: 11/14/26
 * Project ARDO
 *
 * Class description:
 * `HelpPanel.cs` is the quick guide, opened by typing help or ? anywhere in
    the game. It explains the ways to act (typing, clicking, the buttons and
    keys) and the commands for town, the pack and a fight. Esc closes it.
 */
using System;
using System.Drawing;
using System.Windows.Forms;
using ARDO.UI.Display;

namespace ARDO.UI.Controls
{
    public class HelpPanel : UserControl, IOverlayPanel
    {
        private static readonly (string Heading, string[] Lines)[] Sections =
        {
            ("HOW TO ACT", new[]
            {
                "Type a command in the box at the bottom and press Enter.",
                "Or click: a menu line, a list row, an enemy or a card fills in its command; Enter runs it.",
                "The buttons at the bottom right do the same: Inventory and Gear in town; Attack, Special,",
                "Flee and Item in a fight."
            }),
            ("KEYS", new[]
            {
                "Esc           close a screen, or a menu in a fight",
                "Up / Down     earlier commands",
                "PageUp/Down   scroll the story"
            }),
            ("ANYWHERE", new[]
            {
                "i             your pack (use, equip, unequip, drop)",
                "g             your gear on the figure",
                "perks         the perks you have taken",
                "help or ?     this guide"
            }),
            ("IN TOWN", new[]
            {
                "Pick a number from the menu: shop, rest at the inn, the dungeon, save, main menu.",
                "In the shop: buy 1, sell 1, sell all, equip 1, unequip 1."
            }),
            ("IN A FIGHT", new[]
            {
                "1 attack   2 special   3 flee   4 item",
                "target 2 turns to enemy 2; attack 2 hits it straight away.",
                "A ! line warns of a heavy blow coming next turn: guard, interrupt or kill it first."
            })
        };

        public event EventHandler? CloseRequested;

        // The guide has nothing to click.
        public event Action<string>? CommandSuggested { add { } remove { } }

        public string CommandHint => "Quick guide. Esc or exit closes it.";

        public HelpPanel()
        {
            BuildInterface();
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
                RowCount = 2,
                Padding = new Padding(24, 18, 24, 18),
                BackColor = Color.Black
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var title = new Label
            {
                Dock = DockStyle.Fill,
                Text = "QUICK GUIDE",
                ForeColor = Color.Gold,
                BackColor = Color.Black,
                Font = new Font("Consolas", 24, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var body = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Black
            };

            foreach ((string heading, string[] lines) in Sections)
            {
                body.Controls.Add(new Label
                {
                    AutoSize = true,
                    Text = heading,
                    ForeColor = Color.Gold,
                    BackColor = Color.Black,
                    Font = UiFonts.Mono(12f, FontStyle.Bold),
                    Margin = new Padding(0, 10, 0, 4)
                });
                body.Controls.Add(new Label
                {
                    AutoSize = true,
                    Text = string.Join("\n", lines),
                    ForeColor = Color.LightGreen,
                    BackColor = Color.Black,
                    Font = UiFonts.Mono(10.5f),
                    Margin = new Padding(12, 0, 0, 0)
                });
            }

            root.Controls.Add(title, 0, 0);
            root.Controls.Add(body, 0, 1);
            Controls.Add(root);
        }

        public void ProcessCommand(string command)
        {
            // Whatever is typed, the guide closes and the game carries on where it was.
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
