/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `CharacterCreationPanel.cs` is the new-hero screen. The name sits under the
    title, and the two classes stand side by side as cards with their art,
    starting stats and special moves. The player types a name, then picks a
    class by number, by name, or by clicking its card.
 */
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ARDO.Core.Abstracts;
using ARDO.Game.Players;
using ARDO.Interop;
using ARDO.UI.Display;

namespace ARDO.UI.Controls
{
    public class CharacterCreationPanel : UserControl, IOverlayPanel
    {
        private const int MaxNameLength = 16;
        private const string NameHint = "Type your hero's name and press Enter. Esc returns to the start screen.";
        private const string ClassHint = "Pick a class: 1 Warrior or 2 Mage (or click a card). Type back to rename.";

        private static readonly (string Class, string Art, string Pitch)[] Classes =
        {
            ("Warrior", "warrior", "Steel and stubbornness. Takes the hits and answers with the blade."),
            ("Mage", "mage", "Frail, but every spell lands hard, and lightning reaches every foe.")
        };

        private readonly Action<string> _setHint;
        private readonly Panel[] _cards = new Panel[Classes.Length];
        private Label _nameValue = null!;
        private Label _feedbackLabel = null!;
        private string? _name;
        private int _chosenClass = -1;

        public event EventHandler? CloseRequested;

        public event Action<string>? CommandSuggested;

        // Raised once both a name and a class are chosen: (name, "Warrior" or "Mage").
        public event Action<string, string>? HeroChosen;

        public string CommandHint => _name == null ? NameHint : ClassHint;

        public CharacterCreationPanel(Action<string> setHint)
        {
            _setHint = setHint ?? throw new ArgumentNullException(nameof(setHint));
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
                ColumnCount = 2,
                RowCount = 4,
                Padding = new Padding(16, 12, 16, 8),
                BackColor = Color.Black
            };

            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));

            var title = new Label
            {
                Dock = DockStyle.Fill,
                Text = "CREATE YOUR HERO",
                ForeColor = Color.Gold,
                BackColor = Color.Black,
                Font = new Font("Consolas", 24, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Not docked, so the row stays centered under the title.
            var nameRow = new FlowLayoutPanel
            {
                BackColor = Color.Black,
                WrapContents = false,
                Anchor = AnchorStyles.None,
                AutoSize = true
            };

            var nameLabel = new Label
            {
                AutoSize = true,
                Text = "Name",
                ForeColor = Color.Gold,
                BackColor = Color.Black,
                Font = UiFonts.Mono(13f, FontStyle.Bold),
                Margin = new Padding(0, 8, 14, 0)
            };

            _nameValue = new Label
            {
                AutoSize = true,
                ForeColor = Color.LightGray,
                BackColor = Color.Black,
                Font = UiFonts.Mono(13f),
                Margin = new Padding(0, 8, 0, 0)
            };

            nameRow.Controls.Add(nameLabel);
            nameRow.Controls.Add(_nameValue);

            _feedbackLabel = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.WhiteSmoke,
                BackColor = Color.Black,
                Font = UiFonts.Mono(11f),
                TextAlign = ContentAlignment.MiddleCenter
            };

            root.SetColumnSpan(title, 2);
            root.SetColumnSpan(nameRow, 2);
            root.SetColumnSpan(_feedbackLabel, 2);
            root.Controls.Add(title, 0, 0);
            root.Controls.Add(nameRow, 0, 1);
            for (int i = 0; i < Classes.Length; i++)
            {
                _cards[i] = BuildCard(i);
                root.Controls.Add(_cards[i], i, 2);
            }

            root.Controls.Add(_feedbackLabel, 0, 3);
            Controls.Add(root);
            RefreshView();
        }

        private Panel BuildCard(int index)
        {
            (string className, string artFile, string pitch) = Classes[index];
            Hero sample = index == 0 ? new Warrior(className) : new Mage(className);

            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                Margin = new Padding(index == 0 ? 0 : 8, 6, index == 0 ? 8 : 0, 6),
                Padding = new Padding(14, 10, 14, 10),
                Cursor = Cursors.Hand
            };
            card.Paint += (_, e) =>
            {
                Color border = index == _chosenClass ? Color.LightGreen : Color.DarkGreen;
                using var pen = new Pen(border, index == _chosenClass ? 2 : 1);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                BackColor = Color.Black,
                Cursor = Cursors.Hand
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90F));

            var heading = new Label
            {
                Dock = DockStyle.Fill,
                Text = $"{index + 1}  {className.ToUpperInvariant()}",
                ForeColor = Color.Gold,
                Font = UiFonts.Mono(14f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var pitchLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = pitch,
                ForeColor = Color.LightGray,
                Font = UiFonts.Mono(9.5f),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var art = new AsciiArtView { Margin = new Padding(0, 4, 0, 4) };
            art.SetArt(AsciiArtLoader.LoadArt("Heroes", artFile));

            string mana = sample is Mage mage ? $"{mage.MaxMana}" : "none";
            var stats = new Label
            {
                Dock = DockStyle.Fill,
                Text = $"Health  {sample.MaxHealth,-6} Mana    {mana}\n" +
                       $"Attack  {sample.BasicAttackDamage,-6} Crit    {sample.CritChancePercent}%",
                ForeColor = Color.LightGreen,
                Font = UiFonts.Mono(10.5f),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var moves = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Special moves\n" + string.Join("\n", sample.GetCombatMoves().Select(move => $"{move.MoveName} ({move.Type.ToLowerInvariant()})")),
                ForeColor = Color.LightSkyBlue,
                Font = UiFonts.Mono(9.5f),
                TextAlign = ContentAlignment.TopCenter
            };

            layout.Controls.Add(heading, 0, 0);
            layout.Controls.Add(pitchLabel, 0, 1);
            layout.Controls.Add(art, 0, 2);
            layout.Controls.Add(stats, 0, 3);
            layout.Controls.Add(moves, 0, 4);
            card.Controls.Add(layout);

            // Clicking anywhere on the card picks that class.
            foreach (Control control in new Control[] { card, layout, heading, pitchLabel, art, stats, moves })
            {
                control.BackColor = Color.Black;
                control.Cursor = Cursors.Hand;
                control.Click += (_, __) => ClickCard(index);
            }

            return card;
        }

        public void ProcessCommand(string command)
        {
            string input = command.Trim();
            string lower = input.ToLowerInvariant();

            if (lower is "exit" or "back" or "cancel")
            {
                if (_name == null)
                {
                    CloseRequested?.Invoke(this, EventArgs.Empty);
                    return;
                }

                // Back from the class step: name the hero again.
                _name = null;
                _feedbackLabel.Text = string.Empty;
                RefreshView();
                return;
            }

            // A number always means a class, even before the name is in.
            int index = lower switch
            {
                "1" => 0,
                "2" => 1,
                "warrior" or "w" when _name != null => 0,
                "mage" or "m" when _name != null => 1,
                _ => -1
            };

            if (_name == null && index < 0)
            {
                SetName(input);
                return;
            }

            if (index < 0)
            {
                _feedbackLabel.Text = "Type 1 for Warrior or 2 for Mage.";
                return;
            }

            ChooseClass(index);
        }

        private void SetName(string input)
        {
            string name = input.Length > MaxNameLength ? input[..MaxNameLength].TrimEnd() : input;
            if (name.Length == 0)
            {
                _feedbackLabel.Text = "Your hero needs a name.";
                return;
            }

            _name = name;
            _feedbackLabel.Text = string.Empty;
            RefreshView();
            Finish();
        }

        // Before a name, a click just picks the class. After it, the click selects
        // the card and puts its number in the command box; Enter confirms.
        private void ClickCard(int index)
        {
            if (_name == null)
            {
                ChooseClass(index);
                return;
            }

            _chosenClass = index;
            foreach (Panel card in _cards)
            {
                card.Invalidate();
            }

            _feedbackLabel.Text = $"Press Enter to set out as {_name} the {Classes[index].Class}.";
            CommandSuggested?.Invoke($"{index + 1}");
        }

        private void ChooseClass(int index)
        {
            _chosenClass = index;
            foreach (Panel card in _cards)
            {
                card.Invalidate();
            }

            if (_name == null)
            {
                _feedbackLabel.Text = $"{Classes[index].Class} it is. Now type a name.";
                return;
            }

            Finish();
        }

        // Creates the hero once both a name and a class are known, in either order.
        private void Finish()
        {
            if (_name == null || _chosenClass < 0)
                return;

            _feedbackLabel.Text = $"{_name} the {Classes[_chosenClass].Class} sets out for Oakhaven...";
            HeroChosen?.Invoke(_name, Classes[_chosenClass].Class);
        }

        private void RefreshView()
        {
            _nameValue.Text = _name ?? "(type it in the command box below)";
            _nameValue.ForeColor = _name == null ? Color.LightGray : Color.LightSkyBlue;
            _setHint(CommandHint);
        }
    }
}
