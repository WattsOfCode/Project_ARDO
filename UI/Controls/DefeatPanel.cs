/**
 * Name: John Deardorff
 * Date: 11/14/26
 * Project ARDO
 *
 * Class description:
 * `DefeatPanel.cs` is the screen shown when a hero falls. Like the class
    screen, the two choices stand side by side as cards (words only, no art): rise again in Oakhaven
    for a price, or let this hero go and start a new one. Letting go erases the
    save, so it asks twice. The run that just ended is summed up underneath.
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ARDO.Core.Abstracts;
using ARDO.UI.Display;

namespace ARDO.UI.Controls
{
    public class DefeatPanel : UserControl, IOverlayPanel
    {
        private const int RiseIndex = 0;
        private const int NewIndex = 1;

        private readonly Hero _hero;
        private readonly int _riseCost;
        private readonly IReadOnlyList<string> _summary;
        private readonly Panel[] _cards = new Panel[2];
        private Label _feedbackLabel = null!;
        private int _selected = -1;
        private bool _confirmingErase;

        // The panel only closes by a decision, never by Esc.
        public event EventHandler? CloseRequested { add { } remove { } }

        public event Action<string>? CommandSuggested;

        // Raised once the player has decided: true to rise again, false to erase this hero and start a new one.
        public event Action<bool>? Decided;

        public string CommandHint => _confirmingErase
            ? $"Type 2 again to erase {_hero.Name} for good, or 1 to rise again."
            : $"Type 1 to rise again in Oakhaven ({_riseCost} gold), or 2 to start a new hero (or click a card).";

        public DefeatPanel(Hero hero, int riseCost, IEnumerable<string> summary)
        {
            _hero = hero ?? throw new ArgumentNullException(nameof(hero));
            _riseCost = riseCost;
            _summary = summary?.Where(line => line.Length > 0).ToList() ?? new List<string>();
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
                RowCount = 7,
                Padding = new Padding(12, 12, 12, 8),
                BackColor = Color.Black
            };

            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            // The content sits in the middle, with equal space above and below.
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 190F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F * _summary.Count + 16F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            var title = new Label
            {
                Dock = DockStyle.Fill,
                Text = "YOU HAVE FALLEN",
                ForeColor = Color.Gold,
                BackColor = Color.Black,
                Font = new Font("Consolas", 24, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var subtitle = new Label
            {
                Dock = DockStyle.Fill,
                Text = $"{_hero.Name} the {_hero.ClassName}, level {_hero.Level}, lies still on the dungeon floor.",
                ForeColor = Color.LightGray,
                BackColor = Color.Black,
                Font = UiFonts.Mono(11f),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var summary = new Label
            {
                Dock = DockStyle.Fill,
                // Padded to one width, so centering the block keeps its columns lined up.
                Text = string.Join("\n", _summary.Select(line => line.PadRight(_summary.Max(other => other.Length)))),
                ForeColor = Color.LightGreen,
                BackColor = Color.Black,
                Font = UiFonts.Mono(10f),
                TextAlign = ContentAlignment.MiddleCenter
            };

            _feedbackLabel = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.WhiteSmoke,
                BackColor = Color.Black,
                Font = UiFonts.Mono(11f),
                TextAlign = ContentAlignment.MiddleCenter
            };

            root.SetColumnSpan(title, 2);
            root.SetColumnSpan(subtitle, 2);
            root.SetColumnSpan(summary, 2);
            root.SetColumnSpan(_feedbackLabel, 2);
            root.Controls.Add(title, 0, 1);
            root.Controls.Add(subtitle, 0, 2);

            bool canAfford = _hero.Gold >= _riseCost;
            _cards[RiseIndex] = BuildCard(RiseIndex, "RISE AGAIN",
                "Wake in Oakhaven with your level,\ngear and perks.",
                canAfford
                    ? $"Costs {_riseCost} gold. You have {_hero.Gold}."
                    : $"Costs {_riseCost} gold. You have {_hero.Gold}, so it takes it all.");
            _cards[NewIndex] = BuildCard(NewIndex, "START A NEW HERO",
                $"Let {_hero.Name} go and begin again\nwith someone new.",
                "This hero's save is erased for good.");

            root.Controls.Add(_cards[RiseIndex], 0, 3);
            root.Controls.Add(_cards[NewIndex], 1, 3);
            root.Controls.Add(summary, 0, 4);
            root.Controls.Add(_feedbackLabel, 0, 5);
            Controls.Add(root);
        }

        private Panel BuildCard(int index, string heading, string pitch, string cost)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                Margin = new Padding(index == 0 ? 0 : 6, 6, index == 0 ? 6 : 0, 6),
                Padding = new Padding(8, 10, 8, 10),
                Cursor = Cursors.Hand
            };
            card.Paint += (_, e) =>
            {
                Color border = index == _selected ? Color.LightGreen : Color.DarkGreen;
                using var pen = new Pen(border, index == _selected ? 2 : 1);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Black
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));

            var headingLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = $"{index + 1}  {heading}",
                ForeColor = Color.Gold,
                Font = UiFonts.Mono(14f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var pitchLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = pitch,
                ForeColor = Color.LightGray,
                Font = UiFonts.Mono(10f),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var costLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = cost,
                ForeColor = Color.LightSkyBlue,
                Font = UiFonts.Mono(10f),
                TextAlign = ContentAlignment.MiddleCenter
            };

            layout.Controls.Add(headingLabel, 0, 0);
            layout.Controls.Add(pitchLabel, 0, 1);
            layout.Controls.Add(costLabel, 0, 2);
            card.Controls.Add(layout);

            foreach (Control control in new Control[] { card, layout, headingLabel, pitchLabel, costLabel })
            {
                control.BackColor = Color.Black;
                control.Cursor = Cursors.Hand;
                control.Click += (_, __) => ClickCard(index);
            }

            return card;
        }

        public void ProcessCommand(string command)
        {
            string lower = command.Trim().ToLowerInvariant();
            int index = lower switch
            {
                "1" or "rise" or "rise again" or "yes" or "y" => RiseIndex,
                "2" or "new" or "start a new hero" or "erase" => NewIndex,
                _ => -1
            };

            // Esc and "back" are no way out of this: the hero has to rise or be let go.
            if (index < 0)
            {
                if (_confirmingErase && lower is "exit" or "back" or "cancel" or "no" or "n")
                {
                    _confirmingErase = false;
                    Select(-1, string.Empty);
                    return;
                }

                _feedbackLabel.Text = "Type 1 to rise again, or 2 to start a new hero.";
                return;
            }

            Choose(index);
        }

        // A click selects the card and puts its number in the command box; Enter confirms.
        private void ClickCard(int index)
        {
            Select(index, index == RiseIndex
                ? "Press Enter to rise again in Oakhaven."
                : "Press Enter to start a new hero. You will be asked once more before the save is erased.");
            CommandSuggested?.Invoke($"{index + 1}");
        }

        private void Choose(int index)
        {
            if (index == RiseIndex)
            {
                Select(RiseIndex, "Oakhaven calls you back...");
                Decided?.Invoke(true);
                return;
            }

            if (!_confirmingErase)
            {
                _confirmingErase = true;
                Select(NewIndex, $"Are you sure? {_hero.Name} and their save will be gone for good. Type 2 again to confirm.");
                return;
            }

            Select(NewIndex, $"{_hero.Name} is laid to rest.");
            Decided?.Invoke(false);
        }

        private void Select(int index, string feedback)
        {
            _selected = index;
            foreach (Panel card in _cards)
            {
                card.Invalidate();
            }

            _feedbackLabel.Text = feedback;
            SetHint();
        }

        private void SetHint() => HintChanged?.Invoke(CommandHint);

        // Raised when the hint above the command box should change.
        public event Action<string>? HintChanged;
    }
}
