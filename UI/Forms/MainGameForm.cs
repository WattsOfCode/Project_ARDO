/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `MainGameForm.cs` is a concrete WinForms host that coordinates the narrative, 
    stats, input, character, and shop overlay panels. It uses a constructor and 
    public methods to manage composition and command routing, while CRUD is 
    delegated to repository classes.
 */
using System;
using System.Drawing;
using System.Windows.Forms;
using ARDO.Core.Abstracts;
using ARDO.Data.SQLite;
using ARDO.Game;
using ARDO.UI.Controls;
using GameMenu = ARDO.UI.Display.MainMenu;

namespace ARDO.UI.Forms
{
    public partial class MainGameForm : Form
    {
        private readonly PlayerRepository _playerRepo;
        private readonly GameMenu _mainMenu;
        private Hero? _activeHero;
        private readonly GameLaunchMode _launchMode;

        private Panel _gameShell = null!;
        private Panel _storyHost = null!;
        private NarrativePanel _narrativePanel = null!;
        private ShopPanel? _shopPanel;
        private CharacterPanel _characterPanel = null!;
        private InputPanel _inputPanel = null!;
        private StatsPanel _statsPanel = null!;
        private GameStateManager? _gameState;

        public MainGameForm(PlayerRepository playerRepo, GameLaunchMode launchMode = GameLaunchMode.MainMenu)
        {
            _playerRepo = playerRepo ?? throw new ArgumentNullException(nameof(playerRepo));
            _launchMode = launchMode;

            InitializeWindowShell();
            InitializeControlLayout();

            _mainMenu = new GameMenu(this, _playerRepo);
            _mainMenu.CharacterSelected += HandleCharacterSelected;

            Shown += (_, __) => BeginLaunchMode();
        }

        private void InitializeWindowShell()
        {
            Text = "ARDO - Game Runtime Environment";
            WindowState = FormWindowState.Normal;
            ClientSize = new Size(1280, 820);
            MinimumSize = new Size(1100, 720);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.Black;
        }

        private void InitializeControlLayout()
        {
            _gameShell = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false
            };

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2
            };

            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360F));

            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 75F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));

            _storyHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black
            };

            _narrativePanel = new NarrativePanel();
            _characterPanel = new CharacterPanel();
            _inputPanel = new InputPanel();
            _statsPanel = new StatsPanel();

            _inputPanel.CommandSubmitted += HandleCommand;

            _storyHost.Controls.Add(_narrativePanel);
            mainLayout.Controls.Add(_storyHost, 0, 0);
            mainLayout.Controls.Add(_characterPanel, 1, 0);
            mainLayout.Controls.Add(_inputPanel, 0, 1);
            mainLayout.Controls.Add(_statsPanel, 1, 1);

            _gameShell.Controls.Add(mainLayout);
            Controls.Add(_gameShell);
        }

        private void HandleCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return;

            string normalized = command.Trim();

            if (_shopPanel != null)
            {
                _shopPanel.ProcessCommand(normalized);
                return;
            }

            if (normalized.Equals("gg", StringComparison.OrdinalIgnoreCase))
            {
                Close();
                return;
            }

            _narrativePanel.AppendNarrative($"> {normalized}");

            if (_gameState is null)
            {
                _mainMenu.HandleInput(normalized);
                return;
            }

            _gameState.ProcessCommand(normalized);
        }

        internal void ClearNarrative()
        {
            _narrativePanel.ClearNarrative();
        }

        public void UpdateVisualMetricsDisplay()
        {
            if (_activeHero == null)
            {
                _statsPanel.ShowPlaceholder("No active hero loaded.");
                return;
            }

            _statsPanel.UpdateStats(
                _activeHero.Name,
                _activeHero.Level,
                _activeHero.Experience,
                _activeHero.ExperienceToNextLevel,
                _activeHero.CurrentHealth,
                _activeHero.MaxHealth,
                _activeHero is ARDO.Game.Players.Mage mage ? mage.CurrentMana : 0,
                _activeHero is ARDO.Game.Players.Mage mageWithMax ? mageWithMax.MaxMana : 0,
                _activeHero.Gold
            );
        }

        private void HandleCharacterSelected(Hero hero)
        {
            _activeHero = hero ?? throw new ArgumentNullException(nameof(hero));
            _gameState = new GameStateManager(this, _activeHero, _playerRepo);
            _gameShell.Visible = true;
            _gameShell.BringToFront();

            ShowPlayer(_activeHero);
            _gameState.BeginCampaign(_launchMode == GameLaunchMode.NewGame);
            UpdateVisualMetricsDisplay();
            FocusCommandInput();
        }

        private void BeginLaunchMode()
        {
            _gameShell.Visible = true;
            _gameShell.BringToFront();

            switch (_launchMode)
            {
                case GameLaunchMode.NewGame:
                    _mainMenu.BeginNewGame();
                    break;
                case GameLaunchMode.LoadGame:
                    _mainMenu.BeginLoadGame();
                    break;
                default:
                    _mainMenu.Show();
                    break;
            }

            FocusCommandInput();
        }

        public void AppendNarrative(string message)
        {
            _narrativePanel.AppendNarrative(message);
        }

        public void ShowPlayer(Hero hero)
        {
            _characterPanel.ShowPlayer(hero);
        }

        public void ShowPlayer(string name)
        {
            _characterPanel.ShowPlayer(name);
        }

        public void ShowEnemy(string name, string spriteFileKey)
        {
            _characterPanel.ShowEnemy(name, spriteFileKey);
        }

        public void FocusCommandInput()
        {
            _inputPanel.FocusInput();
        }

        public void SetCommandContext(string message)
        {
            _inputPanel.SetContextMessage(message);
        }

        public void ShowShopOverlay(ShopPanel shopPanel)
        {
            if (shopPanel == null)
                throw new ArgumentNullException(nameof(shopPanel));

            if (_shopPanel != null)
            {
                _storyHost.Controls.Remove(_shopPanel);
                _shopPanel.Dispose();
            }

            _shopPanel = shopPanel;
            _storyHost.Controls.Remove(_narrativePanel);
            _storyHost.Controls.Add(_shopPanel);
            _shopPanel.BringToFront();
            SetCommandContext("Shop commands: buy 1, sell 1, sell all, exit.");
            FocusCommandInput();
        }

        public void RestoreNarrativePanel()
        {
            if (_shopPanel == null)
                return;

            _storyHost.Controls.Remove(_shopPanel);
            _shopPanel.Dispose();
            _shopPanel = null;

            if (!_storyHost.Controls.Contains(_narrativePanel))
            {
                _storyHost.Controls.Add(_narrativePanel);
                _narrativePanel.Dock = DockStyle.Fill;
                _narrativePanel.BringToFront();
            }

            SetCommandContext("Type a command and press Enter.");
        }

        public void ReturnToMainMenu()
        {
            RestoreNarrativePanel();
            _activeHero = null;
            _gameState = null;
            _gameShell.Visible = false;
            _statsPanel.ShowPlaceholder("No active hero loaded.");
            Close();
        }
    }
}
