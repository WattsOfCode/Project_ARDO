/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `InputPanel.cs` is a concrete WinForms control that collects player commands 
    and sends them through an event. It uses private UI fields, constructors, 
    and event handlers.
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ARDO.UI.Display;

namespace ARDO.UI.Controls
{
    public partial class InputPanel : UserControl
    {
        private Label _contextLabel = null!;
        private TextBox _commandInput = null!;
        private Button _submitButton = null!;

        // Earlier commands, newest last, for the Up and Down keys.
        private const int HistoryLimit = 40;
        private readonly List<string> _history = new();
        private int _historyIndex;

        public event Action<string>? CommandSubmitted;

        // Esc in the command box: close a screen or cancel a menu.
        public event Action? CancelPressed;

        // PageUp / PageDown in the command box scroll the story: -1 up, 1 down.
        public event Action<int>? ScrollRequested;

        public InputPanel()
        {
            BuildInterface();
        }

        private void BuildInterface()
        {
            BackColor = Color.Black;
            Dock = DockStyle.Fill;
            BorderStyle = BorderStyle.FixedSingle;

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2
            };

            layout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 87F));

            layout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 13F));

            layout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 22F));

            layout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 42F));

            _contextLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Type a command and press Enter.",
                ForeColor = Color.LightGray,
                BackColor = Color.Black,
                Font = UiFonts.Mono(9.5f),
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0)
            };

            _commandInput = new TextBox
            {
                Dock = DockStyle.Fill,

                Font = UiFonts.Mono(12f),

                BackColor = Color.Black,
                ForeColor = Color.LightGreen,

                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0),
                MinimumSize = new Size(0, 42)
            };

            _commandInput.KeyDown += HandleEnterKey;

            _submitButton = new Button
            {
                Text = "Execute",

                Dock = DockStyle.Fill,

                Font = UiFonts.Mono(10f),

                BackColor = Color.Black,
                ForeColor = Color.LightGreen,

                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0),
                MinimumSize = new Size(0, 42)
            };

            _submitButton.FlatAppearance.BorderColor = Color.DarkGreen;

            _submitButton.Click += SubmitCommand;

            layout.SetColumnSpan(_contextLabel, 2);
            layout.Controls.Add(_contextLabel, 0, 0);
            layout.Controls.Add(_commandInput, 0, 1);
            layout.Controls.Add(_submitButton, 1, 1);

            Controls.Add(layout);
        }

        private void HandleEnterKey(object? sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    e.SuppressKeyPress = true;
                    SubmitCommand(sender, EventArgs.Empty);
                    break;

                case Keys.Escape:
                    e.SuppressKeyPress = true;
                    CancelPressed?.Invoke();
                    break;

                case Keys.Up:
                    e.Handled = true;
                    StepHistory(-1);
                    break;

                case Keys.Down:
                    e.Handled = true;
                    StepHistory(1);
                    break;

                case Keys.PageUp:
                    e.Handled = true;
                    ScrollRequested?.Invoke(-1);
                    break;

                case Keys.PageDown:
                    e.Handled = true;
                    ScrollRequested?.Invoke(1);
                    break;
            }
        }

        private void StepHistory(int step)
        {
            if (_history.Count == 0)
                return;

            _historyIndex = Math.Clamp(_historyIndex + step, 0, _history.Count);
            _commandInput.Text = _historyIndex < _history.Count ? _history[_historyIndex] : string.Empty;
            _commandInput.SelectionStart = _commandInput.TextLength;
        }

        private void Remember(string command)
        {
            if (_history.Count == 0 || _history[^1] != command)
            {
                _history.Add(command);
                if (_history.Count > HistoryLimit)
                {
                    _history.RemoveAt(0);
                }
            }

            _historyIndex = _history.Count;
        }

        private void SubmitCommand(object? sender, EventArgs e)
        {
            string command = _commandInput.Text.Trim();

            if (string.IsNullOrWhiteSpace(command))
                return;

            _commandInput.Clear();
            Remember(command);
            CommandSubmitted?.Invoke(command);
        }

        // Puts a command in the box, ready for Enter, e.g. after a click on a list.
        public void SuggestCommand(string command)
        {
            _commandInput.Text = command;
            _commandInput.SelectionStart = _commandInput.TextLength;
            _commandInput.Focus();
        }

        public void FocusInput()
        {
            _commandInput.Focus();
        }

        public void SetContextMessage(string message)
        {
            _contextLabel.Text = string.IsNullOrWhiteSpace(message)
                ? "Type a command and press Enter."
                : message.Trim();
        }
    }
}
