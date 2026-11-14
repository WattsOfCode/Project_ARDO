/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `GameStateManager.cs` is a concrete game-flow controller that manages town,
    combat, dungeon events, defeat, and game-over states. It coordinates public
    methods, constructors, and state transitions while delegating CRUD to
    repository classes.
    This file holds its state and the command router; the rest is split by area into
    GameStateManager.Town, .Floors, .Dungeon, .Events, .Panels and .Defeat.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using ARDO.Core.Abstracts;
using ARDO.Core.Enums;
using ARDO.Data;
using ARDO.Data.SQLite;
using ARDO.Game.Combat;
using ARDO.Game.Events;
using ARDO.Game.Progression;
using ARDO.UI.Controls;

namespace ARDO.Game
{
    public partial class GameStateManager
    {
        private readonly IGameView _ui;
        private readonly Hero _hero;
        private readonly IPlayerRepository _playerRepo;
        private readonly QuestRepository _questRepo;
        private readonly CombatEngine _combatEngine;
        private int _encountersRemainingInRun;
        private IOverlayPanel? _openPanel;
        private DungeonEvent? _activeEvent;
        private bool _onSidePassage;
        private bool _inAmbush;

        // Between fights the passage forks. A side that leads to a room (a door or an event)
        // goes on to the next fight afterward, and a chest behind guards waits for the fight to end.
        private bool _afterRoom;
        private DungeonEvent? _pendingEvent;

        // Each run ends at the floor's guardian, once the regular fights are done.
        private bool _guardianFaced;
        private bool _fightingGuardian;

        // The quest whose story has been told this session, so a return to town doesn't retell it.
        private int _introShownForQuest;
        private bool _choosingFloor;

        // After the story: which throne fight is on (if any), and whether this run already offered its hidden key.
        private WheelEnding _throneFight;

        // A town prompt that is open (perks, the tavern or the wheel), so its answer leads back to town.
        // Perks earned before reaching town (or by an old save) are chosen there, then the town menu follows.
        private enum TownPrompt { None, Perks, Talk, Wheel }
        private TownPrompt _townPrompt;

        // How often each person in the tavern has been talked to this session.
        private readonly Dictionary<string, int> _timesTalked = new();
        private bool _keyOfferedThisRun;
        private bool _keyRumorShown;
        private const int KeyEventChancePercent = 60;

        // The trip into the dungeon under way, read out as a summary when it ends.
        private RunTally? _run;
        private string _runOutcome = "Returned to Oakhaven";

        public GameState CurrentState { get; private set; } = GameState.Town;

        // The event on screen, if any, by its title.
        public string? ActiveEventTitle => _activeEvent?.Title;

        public GameStateManager(IGameView ui, Hero hero, IPlayerRepository playerRepo, QuestRepository questRepo)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _hero = hero ?? throw new ArgumentNullException(nameof(hero));
            _playerRepo = playerRepo ?? throw new ArgumentNullException(nameof(playerRepo));
            _questRepo = questRepo ?? throw new ArgumentNullException(nameof(questRepo));

            _combatEngine = new CombatEngine(_ui, _playerRepo);
        }

        public void BeginCampaign(bool isNewGame)
        {
            _ui.ClearNarrative();
            Say("");
            Say("==========================================================", MessageTone.Header);
            Say("                   ARDO: THE FIRST DESCENT", MessageTone.Header);
            Say("==========================================================", MessageTone.Header);
            Say("");

            if (isNewGame)
            {
                // Whole paragraphs, so the panel wraps them at its own width.
                Say("In the Beginning, Alhuezo, the Progenitor, ordained a sacred geometry: flesh was meant to rise from the dust, " +
                    "knit itself over ancient stone, and age backward into a state of pure, unblemished light. " +
                    "But He grew cruel. With a cosmic wrench, He invented the wheel.");
                Say("");
                Say("What humanity celebrates as \"birth\" is actually the beginning of the end. " +
                    "We are born soft and fragile, slowly rotting into the rigid skeletons of His design. " +
                    "Our entire existence is a countdown in reverse.");
                Say("");
                Say("The Maw below Oakhaven has opened again. You must descend into the primordial dark, " +
                    "butcher the guardians of the twisted timeline, and challenge Alhuezo, who turned our growth into a slow, living decay.");
                Say("");
                Say("(Type help at any time for a quick guide to the controls.)", MessageTone.Dim);
            }
            else if (_hero.WheelEnding == WheelEnding.TurnedTheWheel)
            {
                Say("The wheel turns forward now, and Oakhaven grows older the way it was always meant to.");
                Say("The dungeon below never quite empties. The upper floors still stir.");
            }
            else if (_hero.WheelEnding == WheelEnding.SteppedOff)
            {
                Say("You stepped off the wheel, and every morning you wake a little older. You like it.");
                Say("The dungeon below never quite empties. The upper floors still stir.");
            }
            else if (_hero.StoryComplete && _hero.WheelTurn > 0)
            {
                Say($"The wheel has turned {Times(_hero.WheelTurn)} under your hands. The keys lie scattered again.");
                Say("The dungeon below remembers you, and it has grown teeth.");
            }
            else if (_hero.StoryComplete)
            {
                Say("You fell before Alhuezo's throne, yet Oakhaven wakes you once more.");
                Say("The way to the throne is sealed to you now, but the upper floors still stir.");
            }
            else
            {
                Say("You return to the world with old scars and unfinished business.");
                Say("The dungeon still waits below Oakhaven, and the final throne remains untouched.");
            }

            EnterTown();
        }

        public void EnterCombat(IReadOnlyList<Enemy> enemies)
        {
            CurrentState = GameState.Combat;
            _combatEngine.StartEncounter(_hero, enemies);
        }

        // True while a fight is on screen, so the window can show the fight buttons.
        public bool InCombat => CurrentState == GameState.Combat && _combatEngine.IsCombatActive;

        // Esc in a fight closes the special or item menu. Returns false when there was nothing to close.
        public bool CancelMenu() => InCombat && _combatEngine.CancelMenu();

        public static bool IsInventoryCommand(string command) =>
            command is "inventory" or "inv" or "i" or "pack" or "bag";

        public static bool IsGearCommand(string command) =>
            command is "gear" or "g" or "equipment" or "equip";

        public static bool IsHelpCommand(string command) => command is "help" or "?";

        public static bool IsPanelCommand(string command)
        {
            string normalized = command.Trim().ToLowerInvariant();
            return IsInventoryCommand(normalized) || IsGearCommand(normalized) || IsHelpCommand(normalized);
        }

        public void ProcessCommand(string command)
        {
            string normalized = command.Trim().ToLower();

            if (CurrentState == GameState.DefeatPrompt)
            {
                ProcessDefeatPrompt(normalized);
                return;
            }

            if (IsHelpCommand(normalized))
            {
                if (_openPanel is not HelpPanel)
                    ShowPanel(new HelpPanel());
                return;
            }

            // The pack and the gear screen can be opened from anywhere they make sense.
            if (IsInventoryCommand(normalized))
            {
                OpenInventory();
                return;
            }

            if (IsGearCommand(normalized))
            {
                OpenGear();
                return;
            }

            if (normalized is "perks" or "perk")
            {
                ShowPerks();
                return;
            }

            switch (CurrentState)
            {
                case GameState.Town:
                    ProcessTownCommand(normalized);
                    break;

                case GameState.Combat:
                    ProcessCombatCommand(normalized);
                    break;

                case GameState.DungeonEvent:
                    ProcessEventChoice(normalized);
                    break;

                case GameState.DefeatPrompt:
                    ProcessDefeatPrompt(normalized);
                    break;

                case GameState.GameOver:
                    ProcessGameOverCommand();
                    break;
            }
        }

        private void Say(string text, MessageTone tone = MessageTone.Normal)
        {
            _ui.AppendNarrative(text, tone);
        }
    }
}
