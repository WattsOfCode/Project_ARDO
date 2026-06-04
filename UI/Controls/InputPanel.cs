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
using System.Drawing;
using System.Windows.Forms;

namespace ARDO.UI.Controls
{
    public partial class InputPanel : UserControl
    {
        private Label _contextLabel = null!;
        private TextBox _commandInput = null!;
        private Button _submitButton = null!;

        public event Action<string>? CommandSubmitted;

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
                Font = new Font("Consolas", 9, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0)
            };

            _commandInput = new TextBox
            {
                Dock = DockStyle.Fill,

                Font = new Font("Consolas", 12),

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

                Font = new Font("Consolas", 10),

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
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;

                SubmitCommand(sender, EventArgs.Empty);
            }
        }

        private void SubmitCommand(object? sender, EventArgs e)
        {
            string command = _commandInput.Text.Trim();

            if (string.IsNullOrWhiteSpace(command))
                return;

            _commandInput.Clear();
            CommandSubmitted?.Invoke(command);
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
