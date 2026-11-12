/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `MainGameForm.cs` is a concrete WinForms host that coordinates the narrative,
    stats, input, character and quick-action panels, plus the full-screen
    overlays (shop, inventory, gear) that take over the story area. It uses a constructor and 
    public methods to manage composition and command routing, while CRUD is 
    delegated to repository classes.
 */
using System;
using System.Drawing;
using System.Windows.Forms;
using ARDO.Core.Abstracts;
using ARDO.Core.Enums;
using ARDO.Data;
using ARDO.Data.SQLite;
using ARDO.Game;
using ARDO.UI.Controls;
using GameMenu = ARDO.UI.Display.MainMenu;

namespace ARDO.UI.Forms
{
    public partial class MainGameForm : Form
    {
        private readonly IPlayerRepository _playerRepo;
        private readonly QuestRepository _questRepo;
        private readonly GameMenu _mainMenu;
        private Hero? _activeHero;
        private readonly GameLaunchMode _launchMode;

        private Panel _gameShell = null!;
        private Panel _storyHost = null!;
        private NarrativePanel _narrativePanel = null!;
        private Control? _overlay;
        private QuickActionsPanel _quickActions = null!;
        private CharacterPanel _characterPanel = null!;
        private InputPanel _inputPanel = null!;
        private StatsPanel _statsPanel = null!;
        private GameStateManager? _gameState;

        public MainGameForm(IPlayerRepository playerRepo, QuestRepository questRepo, GameLaunchMode launchMode = GameLaunchMode.MainMenu)
        {
            _playerRepo = playerRepo ?? throw new ArgumentNullException(nameof(playerRepo));
            _questRepo = questRepo ?? throw new ArgumentNullException(nameof(questRepo));
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

            // Story on the left over the command box; on the right the portrait,
            // the hero's stats, and the inventory/gear buttons beside the command box.
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3
            };

            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360F));

            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 320F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));

            _storyHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black
            };

            _narrativePanel = new NarrativePanel();
            _characterPanel = new CharacterPanel();
            _inputPanel = new InputPanel();
            _statsPanel = new StatsPanel();
            _quickActions = new QuickActionsPanel();

            _inputPanel.CommandSubmitted += HandleCommand;
            _inputPanel.CancelPressed += HandleCancel;
            _inputPanel.ScrollRequested += direction => _narrativePanel.ScrollPage(direction);
            // In a fight, the enemy rows (the ones with a health bar) mean "hit this one".
            _narrativePanel.OptionClicked += (number, line) =>
                _inputPanel.SuggestCommand(_gameState?.InCombat == true && line.Contains('[') ? $"attack {number}" : number);
            _quickActions.CommandChosen += command =>
            {
                HandleCommand(command);
                FocusCommandInput();
            };

            _storyHost.Controls.Add(_narrativePanel);
            mainLayout.Controls.Add(_storyHost, 0, 0);
            mainLayout.SetRowSpan(_storyHost, 2);
            mainLayout.Controls.Add(_characterPanel, 1, 0);
            mainLayout.Controls.Add(_statsPanel, 1, 1);
            mainLayout.Controls.Add(_inputPanel, 0, 2);
            mainLayout.Controls.Add(_quickActions, 1, 2);

            _gameShell.Controls.Add(mainLayout);
            Controls.Add(_gameShell);
        }

        private void HandleCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return;

            string normalized = command.Trim();

            // An open screen takes typed commands, except "inventory" and "gear",
            // which switch straight to the other screen.
            if (_overlay is IOverlayPanel overlay)
            {
                if (_gameState != null && GameStateManager.IsPanelCommand(normalized))
                    _gameState.ProcessCommand(normalized);
                else
                    overlay.ProcessCommand(normalized);
                RefreshQuickActions();
                return;
            }

            if (normalized.Equals("gg", StringComparison.OrdinalIgnoreCase))
            {
                Close();
                return;
            }

            // Finish showing the last turn before answering the new command.
            _narrativePanel.FlushPending();
            _narrativePanel.AppendNarrative("");
            _narrativePanel.AppendNarrative($"> {normalized}", MessageTone.Command);

            if (_gameState is null)
            {
                _mainMenu.HandleInput(normalized);
                return;
            }

            _gameState.ProcessCommand(normalized);
            RefreshQuickActions();
        }

        // Esc closes an open screen, or the special or item menu in a fight.
        private void HandleCancel()
        {
            if (_overlay is IOverlayPanel overlay)
            {
                overlay.ProcessCommand("exit");
                return;
            }

            if (_gameState == null)
                return;

            _narrativePanel.FlushPending();
            _gameState.CancelMenu();
        }

        // The corner buttons follow the game: fight buttons in a fight, pack and gear otherwise.
        private void RefreshQuickActions()
        {
            _quickActions.SetFightMode(_gameState?.InCombat == true);
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

            _statsPanel.UpdateStats(_activeHero);
        }

        private void HandleCharacterSelected(Hero hero)
        {
            _activeHero = hero ?? throw new ArgumentNullException(nameof(hero));
            RestoreNarrativePanel();
            _gameState = new GameStateManager(this, _activeHero, _playerRepo, _questRepo);
            _gameShell.Visible = true;
            _gameShell.BringToFront();

            ShowPlayer(_activeHero);
            _quickActions.SetActionsEnabled(true);
            _gameState.BeginCampaign(_launchMode == GameLaunchMode.NewGame);
            RefreshQuickActions();
            UpdateVisualMetricsDisplay();
            FocusCommandInput();
        }

        private void BeginLaunchMode()
        {
            _gameShell.Visible = true;
            _gameShell.BringToFront();
            _characterPanel.UpdateCharacterName("ARDO");
            _statsPanel.ShowPlaceholder(_launchMode == GameLaunchMode.LoadGame
                ? "Pick a saved hero to see their stats."
                : "Your hero's stats show here once they're made.");

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

        public void AppendNarrative(string message, MessageTone tone = MessageTone.Normal)
        {
            _narrativePanel.AppendNarrative(message, tone);
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

        // Swaps the story for a full screen such as the shop. Opening another one replaces it.
        public void ShowOverlay<T>(T panel) where T : Control, IOverlayPanel
        {
            if (panel == null)
                throw new ArgumentNullException(nameof(panel));

            RemoveOverlay();

            _overlay = panel;
            _storyHost.Controls.Remove(_narrativePanel);
            _storyHost.Controls.Add(panel);
            panel.BringToFront();
            panel.CommandSuggested += _inputPanel.SuggestCommand;
            SetCommandContext(panel.CommandHint);
            FocusCommandInput();
        }

        private void RemoveOverlay()
        {
            if (_overlay == null)
                return;

            _storyHost.Controls.Remove(_overlay);
            _overlay.Dispose();
            _overlay = null;
        }

        public void RestoreNarrativePanel()
        {
            if (_overlay == null)
                return;

            RemoveOverlay();

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
            _quickActions.SetActionsEnabled(false);
            _quickActions.SetFightMode(false);
            _statsPanel.ShowPlaceholder("No active hero loaded.");
            Close();
        }
    }
}
