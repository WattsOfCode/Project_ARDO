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
 */
using System;
using System.Collections.Generic;
using ARDO.Core.Abstracts;
using ARDO.Core.Entities;
using ARDO.Core.Enums;
using ARDO.Data;
using ARDO.Data.SQLite;
using ARDO.Game.Combat;
using ARDO.Game.Events;
using ARDO.Game.Inventory;
using ARDO.UI.Controls;
using ARDO.UI.Forms;

namespace ARDO.Game
{
    public class GameStateManager
    {
        private readonly MainGameForm _ui;
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

        // The quest whose story has been told this session, so a return to town doesn't retell it.
        private int _introShownForQuest;
        private bool _choosingFloor;

        // After the story: which throne fight is on (if any), and whether this run already offered its hidden key.
        private WheelEnding _throneFight;
        private bool _keyOfferedThisRun;
        private bool _keyRumorShown;
        private const int KeyEventChancePercent = 60;

        public GameState CurrentState { get; private set; } = GameState.Town;

        public GameStateManager(MainGameForm ui, Hero hero, IPlayerRepository playerRepo, QuestRepository questRepo)
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
                    "butcher the guardians of the twisted timeline, and challenge Alhuezo, the God-King who turned our growth into a slow, living decay.");
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

        public void EnterTown()
        {
            CurrentState = GameState.Town;
            _encountersRemainingInRun = 0;
            _onSidePassage = false;
            _inAmbush = false;
            _afterRoom = false;
            _pendingEvent = null;
            _activeEvent = null;
            _choosingFloor = false;
            _throneFight = WheelEnding.None;
            _keyOfferedThisRun = false;
            _hero.EndDungeonRun();

            // A finished hero only borrows a floor for a run; in town they stand at the end of the story.
            if (_hero.StoryComplete)
                _hero.ActiveStoryNode = EncounterTable.FinalQuestId;

            _ui.ShowPlayer(_hero);
            _ui.UpdateVisualMetricsDisplay();
            Say("");
            Say("==========================================================", MessageTone.Header);
            Say("                        OAKHAVEN", MessageTone.Header);
            Say("==========================================================", MessageTone.Header);
            Say("");

            if (_hero.StoryComplete)
            {
                ShowFinishedHeroTown();
                ShowTownMenu();
                return;
            }

            QuestRepository.QuestSummary? quest = _questRepo.GetQuestSummary(_hero.ActiveStoryNode);
            var npc = _questRepo.GetNpcForQuest(_hero.ActiveStoryNode);

            // The quest's story and its NPC play on the first arrival for that quest;
            // later visits (after fleeing, or a fall) just name the quest.
            bool firstArrival = _introShownForQuest != _hero.ActiveStoryNode;
            _introShownForQuest = _hero.ActiveStoryNode;

            if (quest != null)
            {
                Say($"Quest {_hero.ActiveStoryNode}: {quest.Title}", MessageTone.Gold);
            }

            if (quest != null && firstArrival)
            {
                Say(quest.NarrativeIntro);

                // The first floor's intro already says the way down is open.
                if (quest.IsFinalNode)
                    Say("The final chamber lies beneath the town. He waits there, beyond mercy.");
                else if (_hero.ActiveStoryNode > 1)
                    Say("Another section of the dungeon has opened. Clear it, return, and press deeper.");
            }

            if (npc != null && firstArrival)
            {
                Say("");
                Say($"[{npc.Value.Role}] {npc.Value.Name}: \"{npc.Value.Dialogue}\"", MessageTone.Dim);
            }

            ShowTownMenu();
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

        public static bool IsPanelCommand(string command)
        {
            string normalized = command.Trim().ToLowerInvariant();
            return IsInventoryCommand(normalized) || IsGearCommand(normalized);
        }

        public void ProcessCommand(string command)
        {
            string normalized = command.Trim().ToLower();

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

        private void ProcessTownCommand(string command)
        {
            if (_choosingFloor)
            {
                ProcessFloorChoice(command);
                return;
            }

            bool showTownMenu = true;

            switch (command)
            {
                case "1":
                case "buy":
                case "shop":
                    OpenTownShop();
                    return;

                case "2":
                case "rest":
                    RestAtTown();
                    break;

                case "3":
                case "dungeon":
                case "return":
                    if (_hero.StoryComplete)
                    {
                        ShowFloorChoice();
                        return;
                    }

                    StartDungeonRun();
                    showTownMenu = false;
                    break;

                case "4":
                case "save":
                    _playerRepo.SaveGameState(_hero);
                    Say("Game saved.", MessageTone.Dim);
                    Say("");
                    return;

                case "5":
                case "exit":
                case "mainmenu":
                case "menu":
                    Say("");
                    Say("You leave Oakhaven and return to the main menu.");
                    _ui.ReturnToMainMenu();
                    return;

                default:
                    Say("Pick 1 to 5 from the town menu, or i / g for your pack and gear.", MessageTone.Dim);
                    break;
            }

            if (CurrentState == GameState.Town && showTownMenu)
            {
                ShowTownMenu(arriving: false);
            }
        }

        private void ProcessCombatCommand(string command)
        {
            if (!_combatEngine.IsCombatActive)
                return;

            _combatEngine.ExecuteTurn(command);

            if (_combatEngine.IsCombatActive)
                return;

            if (_combatEngine.LastActionWasFleeSuccessful)
            {
                HandleSuccessfulFlee();
                return;
            }

            if (!_hero.IsAlive)
            {
                if (_combatEngine.FinalBossEndingPlayed)
                {
                    CurrentState = GameState.GameOver;

                    // The story is over for this hero: the load list marks it, and the throne is sealed to them.
                    _hero.StoryComplete = true;
                    _hero.RestoreForTown();
                    _playerRepo.SaveGameState(_hero);
                    _ui.SetCommandContext("Type anything and press Enter to return to the start screen.");
                    Say("The epilogue is complete.", MessageTone.Dim);
                    return;
                }

                HandleDefeatStart();
                return;
            }

            if (_throneFight != WheelEnding.None)
            {
                PlayWheelEnding(_throneFight);
                return;
            }

            HandlePostCombatVictory();
            ContinueDungeonRun();
        }

        // The rematch or the shadow is won: the ending plays, it's saved, and the hero goes back to the start screen.
        private void PlayWheelEnding(WheelEnding ending)
        {
            _throneFight = WheelEnding.None;
            _hero.WheelEnding = ending;
            _hero.RestoreForTown();
            _playerRepo.SaveGameState(_hero);
            _ui.UpdateVisualMetricsDisplay();

            foreach ((string text, MessageTone tone) in WheelEndings.For(ending, _hero))
            {
                Say(text, tone);
            }

            CurrentState = GameState.GameOver;
            _ui.SetCommandContext("Type anything and press Enter to return to the start screen.");
            Say("The story is complete.", MessageTone.Dim);
        }

        // What comes after a won fight or a finished event.
        private void ContinueDungeonRun()
        {
            if (_pendingEvent != null)
            {
                DungeonEvent next = _pendingEvent;
                _pendingEvent = null;
                BeginEvent(next);
                return;
            }

            if (_encountersRemainingInRun > 0)
            {
                if (_afterRoom || _onSidePassage)
                {
                    _afterRoom = false;
                    StartNextDungeonEncounter();
                    return;
                }

                BeginEvent(PassageEvents.Fork());
                return;
            }

            // A finished hero may find this floor's key of the wheel once the fights are done.
            if (!_onSidePassage && !_keyOfferedThisRun && _hero.StoryComplete && ThroneStillSealed
                && _hero.ActiveStoryNode < EncounterTable.FinalQuestId && !_hero.HasWheelKey(_hero.ActiveStoryNode))
            {
                _keyOfferedThisRun = true;
                if (Random.Shared.Next(100) < KeyEventChancePercent)
                {
                    BeginEvent(WheelEvents.KeyFor(_hero.ActiveStoryNode));
                    return;
                }
            }

            if (!_onSidePassage && _hero.ActiveStoryNode < EncounterTable.FinalQuestId)
            {
                BeginEvent(DungeonEvents.SidePassage());
                return;
            }

            CompleteDungeonRun();
        }

        private void HandlePostCombatVictory()
        {
            int floor = _hero.ActiveStoryNode;

            GameItem? drop = LootGenerator.RollEncounterDrop(floor, _hero);
            if (drop != null)
            {
                GiveItem(drop, "Loot acquired:");
            }

            for (int i = 0; i < _combatEngine.GuardiansDefeated; i++)
            {
                GiveItem(LootGenerator.RollGear(floor, RarityType.Rare), "The guardian leaves something behind:");
            }

            if (_onSidePassage)
            {
                _hero.Gold += _combatEngine.LastGoldEarned;
                Say($"The side passage pays double: +{_combatEngine.LastGoldEarned} more gold.", MessageTone.Gold);
                GiveItem(LootGenerator.RollGear(floor + 1, RarityType.Rare), "Deep in the passage you find:");
            }

            // An ambush is an extra fight; it doesn't count toward the run.
            if (_inAmbush)
            {
                _inAmbush = false;
            }
            else
            {
                _encountersRemainingInRun = Math.Max(0, _encountersRemainingInRun - 1);
            }

            _playerRepo.SaveGameState(_hero);
            _ui.UpdateVisualMetricsDisplay();
        }

        // Adds an item to the pack (if there is room), saves it, and wears it if it's an upgrade.
        private void GiveItem(GameItem item, string intro)
        {
            Say("");
            if (!_hero.HeroInventory.AddItem(item))
            {
                Say($"You find {item}, but your pack is full. Sell something in town to make room.", MessageTone.Dim);
                return;
            }

            _playerRepo.SaveInventoryItem(_hero.CharacterId, item);
            Say($"{intro} {item} ({item.Summary})", item.Rarity.ToTone());

            if (_hero.Equipment.IsUpgrade(item))
            {
                _hero.Equipment.Equip(item);
                _playerRepo.SaveEquipment(_hero);
                Say($"You equip the {item.Name}.", MessageTone.Heal);
            }
        }

        private void BeginEvent(DungeonEvent dungeonEvent)
        {
            CurrentState = GameState.DungeonEvent;
            _activeEvent = dungeonEvent;

            Say("");
            Say($"--- {dungeonEvent.Title} ---", MessageTone.Header);
            foreach (string line in dungeonEvent.Description)
            {
                Say(line);
            }

            Say("");
            for (int i = 0; i < dungeonEvent.Choices.Count; i++)
            {
                Say($"{i + 1}. {dungeonEvent.Choices[i].Label}");
            }

            ShowEventPrompt();
        }

        private void ShowEventPrompt()
        {
            if (_activeEvent != null)
            {
                _ui.SetCommandContext(ChoicePrompt(_activeEvent.Choices.Count));
            }
        }

        private static string ChoicePrompt(int count) => count == 2 ? "Choose 1 or 2." : $"Choose 1 to {count}.";

        private void ProcessEventChoice(string command)
        {
            DungeonEvent? dungeonEvent = _activeEvent;
            if (dungeonEvent == null)
            {
                ContinueDungeonRun();
                return;
            }

            if (!int.TryParse(command, out int choice) || choice < 1 || choice > dungeonEvent.Choices.Count)
            {
                Say(ChoicePrompt(dungeonEvent.Choices.Count), MessageTone.Dim);
                return;
            }

            _activeEvent = null;
            EventOutcome outcome = dungeonEvent.Choices[choice - 1].Resolve(_hero);

            foreach ((string text, MessageTone tone) in outcome.Lines)
            {
                Say(text, tone);
            }

            if (outcome.ItemUsedUp != null)
            {
                _hero.HeroInventory.RemoveItem(outcome.ItemUsedUp);
                _playerRepo.RemoveInventoryItem(_hero.CharacterId, outcome.ItemUsedUp.ItemId);
            }

            if (outcome.Reward != null)
            {
                GiveItem(outcome.Reward, "Inside you find:");
            }

            _playerRepo.SaveGameState(_hero);
            _ui.UpdateVisualMetricsDisplay();

            if (outcome.ThroneFight != WheelEnding.None)
            {
                _throneFight = outcome.ThroneFight;
                Enemy foe = _throneFight == WheelEnding.SteppedOff
                    ? Game.Enemies.ShadowSelf.Of(_hero)
                    : Game.Enemies.Alhuezo.Unbound(_hero);
                EnterCombat(new[] { foe });
            }
            else if (outcome.GoHome)
            {
                EnterTown();
            }
            else if (outcome.ReturnToTown)
            {
                CompleteDungeonRun();
            }
            else if (outcome.PressOn)
            {
                _onSidePassage = true;
                _encountersRemainingInRun = 1;
                StartNextDungeonEncounter();
            }
            else if (outcome.StartsAmbush)
            {
                _inAmbush = true;
                _pendingEvent = outcome.FollowUp;
                EnterCombat(EncounterTable.RollGroup(_hero.ActiveStoryNode));
            }
            else if (outcome.FightNow)
            {
                StartNextDungeonEncounter();
            }
            else if (outcome.FollowUp != null)
            {
                _afterRoom |= outcome.RoomInsteadOfFight;
                BeginEvent(outcome.FollowUp);
            }
            else
            {
                ContinueDungeonRun();
            }
        }

        private void CompleteDungeonRun()
        {
            QuestRepository.QuestSummary? quest = _questRepo.GetQuestSummary(_hero.ActiveStoryNode);
            if (quest != null)
            {
                Say("");
                Say("The floor falls quiet behind you, and you climb back to Oakhaven.");
                if (!_hero.StoryComplete)
                    _hero.ActiveStoryNode = Math.Min(EncounterTable.FinalQuestId, _hero.ActiveStoryNode + 1);
            }

            _playerRepo.SaveGameState(_hero);
            EnterTown();
        }

        // The town description only on arrival; after that, just the choices.
        private void ShowTownMenu(bool arriving = true)
        {
            _ui.SetCommandContext("Town: 1 shop, 2 rest at inn (10g), 3 enter dungeon, 4 save, 5 main menu, i / g pack and gear.");
            Say("");
            if (arriving)
            {
                Say("Oakhaven settles around you. The merchant waits nearby, the inn is warm, and the dungeon still looms below.");
            }

            Say("1 Shop   2 Rest (10g)   3 Dungeon   4 Save   5 Main menu", MessageTone.Dim);
        }

        private void OpenTownShop()
        {
            if (_openPanel is ShopPanel)
                return;

            ShowPanel(new ShopPanel(_hero, _playerRepo, _ui.UpdateVisualMetricsDisplay));
        }

        private void OpenInventory()
        {
            switch (CurrentState)
            {
                case GameState.Combat:
                    // Mid-fight the pack is the potion pouch, and using one costs the turn.
                    ProcessCombatCommand("item");
                    return;

                case GameState.Town:
                case GameState.DungeonEvent:
                    if (_openPanel is not InventoryPanel)
                    {
                        ShowPanel(new InventoryPanel(_hero, _playerRepo, _ui.UpdateVisualMetricsDisplay));
                    }
                    return;

                default:
                    Say("Your pack can wait.", MessageTone.Dim);
                    return;
            }
        }

        private void OpenGear()
        {
            switch (CurrentState)
            {
                case GameState.Combat:
                    Say("There's no time to change gear mid-fight. Your potions are under Inventory.", MessageTone.Dim);
                    return;

                case GameState.Town:
                case GameState.DungeonEvent:
                    if (_openPanel is not GearPanel)
                    {
                        ShowPanel(new GearPanel(_hero, _playerRepo, _ui.UpdateVisualMetricsDisplay));
                    }
                    return;

                default:
                    Say("Your gear can wait.", MessageTone.Dim);
                    return;
            }
        }

        // Opening a screen while another is open replaces it.
        private void ShowPanel<T>(T panel) where T : System.Windows.Forms.Control, IOverlayPanel
        {
            _openPanel = panel;
            panel.CloseRequested += (_, __) => ClosePanel(panel);
            _ui.ShowOverlay(panel);
        }

        private void ClosePanel(IOverlayPanel panel)
        {
            if (!ReferenceEquals(_openPanel, panel))
                return;

            _openPanel = null;
            _ui.RestoreNarrativePanel();
            _ui.UpdateVisualMetricsDisplay();
            Say("");
            Say(panel is ShopPanel
                ? "You return from the town shop and step back into Oakhaven."
                : "You close your pack and look up.");

            if (CurrentState == GameState.Town)
                ShowTownMenu(arriving: false);
            else
                ShowEventPrompt();

            _ui.FocusCommandInput();
        }

        private void RestAtTown()
        {
            if (_hero.Gold < 10)
            {
                Say("You do not have enough gold to rest at the inn.", MessageTone.Dim);
                return;
            }

            _hero.Gold -= 10;
            _hero.RestoreForTown();
            Say("");
            Say("You rest at the inn and recover your strength.", MessageTone.Heal);
            Say($"You now have {_hero.CurrentHealth}/{_hero.MaxHealth} HP.", MessageTone.Heal);
            _playerRepo.SaveGameState(_hero);
            _ui.UpdateVisualMetricsDisplay();
        }

        // Finished heroes pick any floor before the throne.
        private void ShowFloorChoice()
        {
            _choosingFloor = true;
            _ui.SetCommandContext(FloorPrompt);
            Say("");
            Say("Which floor will you walk?");
            for (int floor = 1; floor < EncounterTable.FinalQuestId; floor++)
            {
                QuestRepository.QuestSummary? quest = _questRepo.GetQuestSummary(floor);
                Say($"{floor}. {quest?.Title ?? $"Floor {floor}"}");
            }

            if (ThroneStillSealed)
            {
                Say($"{EncounterTable.FinalQuestId}. The Sealed Throne ({_hero.WheelKeyCount} of 3 keys)");
            }

            Say("0. Stay in town", MessageTone.Dim);
        }

        // The throne door matters only until the hero has ended things one way or the other.
        private bool ThroneStillSealed => _hero.WheelEnding == WheelEnding.None;

        private string FloorPrompt => ThroneStillSealed
            ? "Pick a floor 1 to 3, 4 for the sealed throne, or 0 to stay in town."
            : "Pick a floor 1 to 3, or 0 to stay in town.";

        private void ShowFinishedHeroTown()
        {
            switch (_hero.WheelEnding)
            {
                case WheelEnding.TurnedTheWheel:
                    Say("You turned the wheel. Floors 1 to 3 are still yours to walk.", MessageTone.Gold);
                    return;

                case WheelEnding.SteppedOff:
                    Say("You stepped off the wheel. Floors 1 to 3 are still yours to walk.", MessageTone.Gold);
                    return;
            }

            Say("Your story is told. The throne is sealed, but floors 1 to 3 are still yours to walk.", MessageTone.Gold);
            Say($"Keys of the wheel: {_hero.WheelKeyCount} of 3.", MessageTone.Dim);

            // Maren points the way once a session.
            if (_keyRumorShown)
                return;

            _keyRumorShown = true;
            Say("");
            string rumor = _hero.HasAllWheelKeys
                ? "Three keys, and you found every one. Whatever that door is keeping, it is yours to open now."
                : "They say three keys were hidden below, one on each floor, for whoever came back from the throne. Look where the dead keep things.";
            Say($"[Inn Keeper] Maren: \"{rumor}\"", MessageTone.Dim);
            if (_hero.CanPickLocks)
            {
                Say("[Inn Keeper] Maren: \"Then again, a door with three locks usually has a fourth, for the servants.\"", MessageTone.Dim);
            }
        }

        private void ProcessFloorChoice(string command)
        {
            if (command == "0")
            {
                _choosingFloor = false;
                ShowTownMenu(arriving: false);
                return;
            }

            if (command == $"{EncounterTable.FinalQuestId}" && ThroneStillSealed)
            {
                _choosingFloor = false;
                Say("");
                Say("You take the long stair down, past every floor you have already cleared.");
                BeginEvent(WheelEvents.SealedDoor(_hero));
                return;
            }

            if (!int.TryParse(command, out int floor) || floor < 1 || floor >= EncounterTable.FinalQuestId)
            {
                Say(FloorPrompt, MessageTone.Dim);
                return;
            }

            _choosingFloor = false;
            _hero.ActiveStoryNode = floor;
            StartDungeonRun();
        }

        private void StartDungeonRun()
        {
            QuestRepository.QuestSummary? quest = _questRepo.GetQuestSummary(_hero.ActiveStoryNode);
            _encountersRemainingInRun = EncounterTable.FightsPerRun(_hero.ActiveStoryNode);
            _onSidePassage = false;
            _inAmbush = false;
            _afterRoom = false;
            _pendingEvent = null;

            Say("");
            Say(quest != null
                ? $"You leave town and descend into the dungeon: {quest.Title}."
                : "You leave town and descend into the dungeon.");
            Say("The air grows colder as the stone closes around you.", MessageTone.Dim);
            StartNextDungeonEncounter();
        }

        private void StartNextDungeonEncounter()
        {
            IReadOnlyList<Enemy> encounterGroup = EncounterTable.RollGroup(_hero.ActiveStoryNode);

            Say("");
            Say(_onSidePassage
                ? "Something huge unfolds from the dark of the passage."
                : "Another shape stirs in the dark. The dungeon refuses to stay quiet.", MessageTone.Warning);

            EnterCombat(encounterGroup);
        }

        private void HandleDefeatStart()
        {
            CurrentState = GameState.DefeatPrompt;
            _ui.SetCommandContext("Type 1 to rise again in Oakhaven (50 gold), or 2 to erase this hero.");
            Say("");
            Say("You fall to the dungeon floor, breath gone and vision dimming.", MessageTone.Damage);
            Say("");
            Say("1 Rise again in Oakhaven (50 gold)   2 Let this hero go");
            Say("Letting go erases this hero's save for good.", MessageTone.Warning);
        }

        private void ProcessDefeatPrompt(string command)
        {
            switch (command)
            {
                case "yes":
                case "y":
                case "1":
                    _hero.Gold = Math.Max(0, _hero.Gold - 50);
                    _hero.RestoreForTown();
                    Say("");
                    Say("Rise, hero. There is still much more for you to do.", MessageTone.Gold);
                    Say("");

                    // Dying in the side passage still counts the floor as cleared.
                    if (_onSidePassage)
                    {
                        CompleteDungeonRun();
                        break;
                    }

                    _playerRepo.SaveGameState(_hero);
                    EnterTown();
                    break;

                case "no":
                case "n":
                case "2":
                    _playerRepo.DeleteProfile(_hero.CharacterId);
                    Say("");
                    Say("Your character is erased from the records of Oakhaven.", MessageTone.Damage);
                    _ui.ReturnToMainMenu();
                    break;

                default:
                    Say("Type 1 to rise again, or 2 to let this hero go.", MessageTone.Dim);
                    break;
            }
        }

        private void HandleSuccessfulFlee()
        {
            Say("");
            Say("You break away from the dungeon floor and retreat to Oakhaven before it can close.");

            // The floor was already cleared before the side passage, so fleeing from it still counts.
            if (_onSidePassage)
            {
                CompleteDungeonRun();
                return;
            }

            _playerRepo.SaveGameState(_hero);
            EnterTown();
        }

        private void ProcessGameOverCommand()
        {
            _ui.ReturnToMainMenu();
        }

        private void Say(string text, MessageTone tone = MessageTone.Normal)
        {
            _ui.AppendNarrative(text, tone);
        }
    }
}
