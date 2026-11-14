/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `CharacterCreationPanel.cs` is the new-hero screen. The name sits under the
    title, and the four classes stand side by side as cards with their art,
    starting stats and special moves. The player types a name, then picks a
    class by number, by name, or by clicking its card.
 */
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ARDO.Core.Abstracts;
using ARDO.Core.Factories;
using ARDO.Game.Players;
using ARDO.Interop;
using ARDO.UI.Display;

namespace ARDO.UI.Controls
{
    public class CharacterCreationPanel : UserControl, IOverlayPanel
    {
        private const int MaxNameLength = 16;
        private const string NameHint = "Type a name and press Enter. Type hard or normal to set the mode; Esc goes back.";
        private const string ClassHint = "Pick a class 1 to 4 (or click a card). back renames; hard or normal sets the mode.";

        private static readonly (string Class, string Art, string Pitch)[] Classes =
        {
            ("Warrior", "warrior", "Takes the hits and answers with steel."),
            ("Mage", "mage", "Frail, but every spell lands hard."),
            ("Archer", "archer", "Fights at range. Sharp eyes, deadly crits."),
            ("Rogue", "rogue", "In first, out clean. Poison and dodges.")
        };

        private readonly Action<string> _setHint;
        private readonly Panel[] _cards = new Panel[Classes.Length];
        private Label _nameValue = null!;
        private Label _modeValue = null!;
        private Label _feedbackLabel = null!;
        private string? _name;
        private int _chosenClass = -1;

        public event EventHandler? CloseRequested;

        public event Action<string>? CommandSuggested;

        // Raised once both a name and a class are chosen: (name, "Warrior", "Mage", "Archer" or "Rogue").
        public event Action<string, string>? HeroChosen;

        public string CommandHint => _name == null ? NameHint : ClassHint;

        // Hard mode: no rising again after a fall.
        public bool HardMode { get; private set; }

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
                ColumnCount = Classes.Length,
                RowCount = 4,
                Padding = new Padding(12, 12, 12, 8),
                BackColor = Color.Black
            };

            for (int i = 0; i < Classes.Length; i++)
            {
                root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / Classes.Length));
            }
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

            var modeLabel = new Label
            {
                AutoSize = true,
                Text = "Mode",
                ForeColor = Color.Gold,
                BackColor = Color.Black,
                Font = UiFonts.Mono(13f, FontStyle.Bold),
                Margin = new Padding(40, 8, 14, 0),
                Cursor = Cursors.Hand
            };

            _modeValue = new Label
            {
                AutoSize = true,
                BackColor = Color.Black,
                Font = UiFonts.Mono(13f),
                Margin = new Padding(0, 8, 0, 0),
                Cursor = Cursors.Hand
            };

            // Clicking the mode switches it.
            modeLabel.Click += (_, __) => SetMode(!HardMode);
            _modeValue.Click += (_, __) => SetMode(!HardMode);
            nameRow.Controls.Add(modeLabel);
            nameRow.Controls.Add(_modeValue);

            _feedbackLabel = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.WhiteSmoke,
                BackColor = Color.Black,
                Font = UiFonts.Mono(11f),
                TextAlign = ContentAlignment.MiddleCenter
            };

            root.SetColumnSpan(title, Classes.Length);
            root.SetColumnSpan(nameRow, Classes.Length);
            root.SetColumnSpan(_feedbackLabel, Classes.Length);
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
            Hero sample = HeroFactory.Create(className, className);

            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                Margin = new Padding(index == 0 ? 0 : 4, 6, index == Classes.Length - 1 ? 0 : 4, 6),
                Padding = new Padding(8, 10, 8, 10),
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
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 84F));

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
                Font = UiFonts.Mono(9f),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var art = new AsciiArtView { Margin = new Padding(0, 4, 0, 4) };
            art.SetArt(AsciiArtLoader.LoadArt("Heroes", artFile));

            string mana = sample is Mage mage ? $"{mage.MaxMana}" : "none";
            var stats = new Label
            {
                Dock = DockStyle.Fill,
                Text = $"HP  {sample.MaxHealth,-4} Mana {mana}\n" +
                       $"Atk {sample.BasicAttackDamage,-4} Crit {sample.CritChancePercent}%",
                ForeColor = Color.LightGreen,
                Font = UiFonts.Mono(10f),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var moves = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Special moves\n" + string.Join("\n", sample.GetCombatMoves().Select(move => move.MoveName)),
                ForeColor = Color.LightSkyBlue,
                Font = UiFonts.Mono(9f),
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

            if (lower is "hard" or "normal")
            {
                SetMode(lower == "hard");
                return;
            }

            // A number always means a class, even before the name is in.
            int index = lower switch
            {
                "1" => 0,
                "2" => 1,
                "3" => 2,
                "4" => 3,
                "warrior" or "w" when _name != null => 0,
                "mage" or "m" when _name != null => 1,
                "archer" or "a" when _name != null => 2,
                "rogue" or "r" when _name != null => 3,
                _ => -1
            };

            if (_name == null && index < 0)
            {
                SetName(input);
                return;
            }

            if (index < 0)
            {
                _feedbackLabel.Text = "Type 1 Warrior, 2 Mage, 3 Archer or 4 Rogue.";
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

        private void SetMode(bool hard)
        {
            HardMode = hard;
            _feedbackLabel.Text = hard
                ? "Hard mode: if this hero falls, they are gone for good."
                : "Normal mode: a fallen hero can rise again in Oakhaven.";
            RefreshView();
        }

        private void RefreshView()
        {
            _modeValue.Text = HardMode ? "Hard (no rising again)" : "Normal";
            _modeValue.ForeColor = HardMode ? Color.Gold : Color.LightSkyBlue;
            _nameValue.Text = _name ?? "(type it in the command box below)";
            _nameValue.ForeColor = _name == null ? Color.LightGray : Color.LightSkyBlue;
            _setHint(CommandHint);
        }
    }
}
