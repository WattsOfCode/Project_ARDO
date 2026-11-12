/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `StartMenuForm.cs` is a concrete WinForms screen that handles the title menu and 
    launches new or loaded sessions. It uses private UI fields, constructors, and 
    event-driven methods to route input.
 */
using System;
using System.Drawing;
using System.Windows.Forms;
using ARDO.Data;
using ARDO.Data.SQLite;

namespace ARDO.UI.Forms
{
    public class StartMenuForm : Form
    {
        private readonly IPlayerRepository _playerRepo;
        private readonly QuestRepository _questRepo;
        private RichTextBox _titleBox = null!;
        private Label _optionsLabel = null!;
        private Label _commandLabel = null!;
        private TextBox _commandInput = null!;

        public StartMenuForm(IPlayerRepository playerRepo, QuestRepository questRepo)
        {
            _playerRepo = playerRepo ?? throw new ArgumentNullException(nameof(playerRepo));
            _questRepo = questRepo ?? throw new ArgumentNullException(nameof(questRepo));

            InitializeWindowShell();
            BuildInterface();
        }

        private void InitializeWindowShell()
        {
            Text = "ARDO";
            WindowState = FormWindowState.Normal;
            ClientSize = new Size(1100, 760);
            MinimumSize = new Size(960, 680);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.Black;
            DoubleBuffered = true;
            KeyPreview = true;
        }

        private void BuildInterface()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(40, 90, 40, 40)
            };

            root.RowStyles.Add(new RowStyle(SizeType.Percent, 52F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 120F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 100F));

            _titleBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = Color.Black,
                ForeColor = Color.Gold,
                Font = new Font("Consolas", 11, FontStyle.Bold),
                ScrollBars = RichTextBoxScrollBars.None,
                WordWrap = false,
                TabStop = false,
                Text = BuildAsciiTitle()
            };
            _titleBox.SelectAll();
            _titleBox.SelectionAlignment = HorizontalAlignment.Center;
            _titleBox.DeselectAll();

            _optionsLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "1. New game\n2. Load game\n3. Exit",
                ForeColor = Color.WhiteSmoke,
                BackColor = Color.Black,
                Font = new Font("Consolas", 17, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var commandPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                Padding = new Padding(0, 12, 0, 0)
            };

            _commandLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 34,
                Text = "Type new/load/exit or 1/2/3, then press Enter:",
                ForeColor = Color.LightGray,
                BackColor = Color.Black,
                Font = new Font("Consolas", 11, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter
            };

            _commandInput = new TextBox
            {
                Dock = DockStyle.Bottom,
                Height = 36,
                BackColor = Color.Black,
                ForeColor = Color.LightGreen,
                Font = new Font("Consolas", 12, FontStyle.Regular),
                BorderStyle = BorderStyle.FixedSingle
            };

            _commandInput.KeyDown += HandleEnterKey;

            commandPanel.Controls.Add(_commandInput);
            commandPanel.Controls.Add(_commandLabel);

            root.Controls.Add(_titleBox, 0, 0);
            root.Controls.Add(_optionsLabel, 0, 1);
            root.Controls.Add(commandPanel, 0, 2);

            Controls.Add(root);
        }

        private void HandleEnterKey(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
                return;

            e.SuppressKeyPress = true;
            ExecuteCommand();
        }

        private void ExecuteCommand()
        {
            string command = _commandInput.Text.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(command))
                return;

            _commandInput.Clear();

            switch (command)
            {
                case "1":
                case "new":
                case "new game":
                case "start":
                    LaunchGame(GameLaunchMode.NewGame);
                    break;
                case "2":
                case "load":
                case "load game":
                    LaunchGame(GameLaunchMode.LoadGame);
                    break;
                case "3":
                case "exit":
                case "quit":
                    Close();
                    break;
                default:
                    _optionsLabel.Text = "1. New game\n2. Load game\n3. Exit";
                    _commandLabel.Text = "Invalid input. Type 1, 2, or 3, then press Enter:";
                    _commandInput.Clear();
                    _commandInput.Focus();
                    break;
            }
        }

        private void LaunchGame(GameLaunchMode mode)
        {
            var gameForm = new MainGameForm(_playerRepo, _questRepo, mode);
            gameForm.FormClosed += (_, __) =>
            {
                Show();
                _commandInput.Focus();
                _commandLabel.Text = "Type new/load/exit or 1/2/3, then press Enter:";
                _optionsLabel.Text = "1. New game\n2. Load game\n3. Exit";
            };
            Hide();
            gameForm.Show();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _optionsLabel.Text = "1. New game\n2. Load game\n3. Exit";
            _commandLabel.Text = "Type new/load/exit or 1/2/3, then press Enter:";
            CenterTitleText();
            _commandInput.Focus();
        }

        private void CenterTitleText()
        {
            _titleBox.SelectAll();
            _titleBox.SelectionAlignment = HorizontalAlignment.Center;
            _titleBox.DeselectAll();
        }

        private static string BuildAsciiTitle()
        {
            return
@"     ___      .______       _______   ______   
    /   \     |   _  \     |       \ /  __  \  
   /  ^  \    |  |_)  |    |  .--.  |  |  |  | 
  /  /_\  \   |      /     |  |  |  |  |  |  | 
 /  _____  \  |  |\  \----.|  '--'  |  `--'  | 
/__/     \__\ | _| `._____||_______/ \______/  
                                               ";
        }
    }
}
