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
using System.Linq;
using ARDO.Core.Abstracts;
using ARDO.Core.Entities;
using ARDO.Core.Enums;
using ARDO.Data;
using ARDO.Data.SQLite;
using ARDO.Game.Combat;
using ARDO.Game.Events;
using ARDO.Game.Inventory;
using ARDO.Game.Progression;
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

        // Each run ends at the floor's guardian, once the regular fights are done.
        private bool _guardianFaced;
        private bool _fightingGuardian;

        // The quest whose story has been told this session, so a return to town doesn't retell it.
        private int _introShownForQuest;
        private bool _choosingFloor;

        // Perks earned before reaching town (or by an old save) are chosen there, then the town menu follows.
        private bool _perkPickInTown;

        // After the story: which throne fight is on (if any), and whether this run already offered its hidden key.
        private WheelEnding _throneFight;

        // The "turn the wheel again" prompt is open in town.
        private bool _wheelChoiceInTown;
        private bool _keyOfferedThisRun;
        private bool _keyRumorShown;
        private const int KeyEventChancePercent = 60;

        // The trip into the dungeon under way, read out as a summary when it ends.
        private RunTally? _run;
        private string _runOutcome = "Returned to Oakhaven";

        public GameState CurrentState { get; private set; } = GameState.Town;

        // The event on screen, if any, by its title.
        public string? ActiveEventTitle => _activeEvent?.Title;

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

        public void EnterTown()
        {
            EndRunTally(_runOutcome);
            CurrentState = GameState.Town;
            _encountersRemainingInRun = 0;
            _onSidePassage = false;
            _inAmbush = false;
            _afterRoom = false;
            _pendingEvent = null;
            _guardianFaced = false;
            _fightingGuardian = false;
            _activeEvent = null;
            _choosingFloor = false;
            _perkPickInTown = false;
            _wheelChoiceInTown = false;
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
                ShowTownMenuOrPerks();
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

            ShowTownMenuOrPerks();
        }

        // A hero with perks still to choose picks them before the town menu comes up.
        private void ShowTownMenuOrPerks()
        {
            if (_hero.PendingPerkPicks > 0)
            {
                _perkPickInTown = true;
                BeginEvent(PerkEvents.Choose(_hero));
                return;
            }

            ShowTownMenu();
        }

        private void ShowPerks()
        {
            Say("");
            Say("--- PERKS ---", MessageTone.Header);
            if (_hero.Perks.Count == 0)
            {
                Say("No perks yet. Every level-up offers three to choose from.", MessageTone.Dim);
            }

            foreach (IGrouping<PerkId, PerkId> taken in _hero.Perks.GroupBy(perk => perk))
            {
                PerkInfo info = PerkCatalog.Get(taken.Key);
                string times = taken.Count() > 1 ? $" x{taken.Count()}" : string.Empty;
                Say($"{info.Name}{times}: {info.Description}");
            }

            if (_hero.PendingPerkPicks > 0)
            {
                Say(_hero.PendingPerkPicks == 1 ? "1 perk still to choose." : $"{_hero.PendingPerkPicks} perks still to choose.", MessageTone.Gold);
            }
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

                case "6":
                case "turn":
                case "wheel":
                    if (_hero.WheelEnding == WheelEnding.None)
                        break;

                    _wheelChoiceInTown = true;
                    BeginEvent(WheelEvents.TurnAgain(_hero));
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
                    Say($"Pick 1 to {TownChoiceCount} from the town menu, or i / g for your pack and gear.", MessageTone.Dim);
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
                    EndRunTally("Fell before the throne", always: true);
                    _ui.SetCommandContext("Type anything and press Enter to return to the start screen.");
                    Say("The epilogue is complete.", MessageTone.Dim);
                    return;
                }

                HandleDefeatStart();
                return;
            }

            _run?.AddVictory(_combatEngine.LastFoesSlain, _combatEngine.LastXpEarned);

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

            EndRunTally(ending == WheelEnding.SteppedOff ? "Stepped off the wheel" : "Turned the wheel", always: true);
            CurrentState = GameState.GameOver;
            _ui.SetCommandContext("Type anything and press Enter to return to the start screen.");
            Say("The story is complete.", MessageTone.Dim);
        }

        // What comes after a won fight or a finished event.
        private void ContinueDungeonRun()
        {
            if (_hero.PendingPerkPicks > 0)
            {
                BeginEvent(PerkEvents.Choose(_hero));
                return;
            }

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

            if (!_onSidePassage && !_guardianFaced && _hero.ActiveStoryNode < EncounterTable.FinalQuestId)
            {
                _guardianFaced = true;
                BeginEvent(GuardianEvents.Approach(_hero.ActiveStoryNode));
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

            if (_fightingGuardian)
            {
                _fightingGuardian = false;
                RewardGuardianKill(floor);
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

        // The first time a hero beats a floor's guardian, it leaves a trophy worth having.
        private void RewardGuardianKill(int floor)
        {
            Say("");
            Say("The guardian falls, and the floor goes quiet in a way it has not been for a long time.", MessageTone.Gold);
            if (_hero.HasSlainGuardian(floor))
                return;

            _hero.GuardiansSlain |= 1 << (floor - 1);
            GiveItem(LootGenerator.RollGear(floor + 1, RarityType.Epic), "A trophy of the first kill:");
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
            _run?.AddItem();
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

            if (_wheelChoiceInTown)
            {
                _wheelChoiceInTown = false;
                if (_hero.WheelEnding == WheelEnding.None)
                {
                    // Turned: arrive fresh, so Maren tells of the keys again.
                    _keyRumorShown = false;
                    EnterTown();
                    return;
                }

                CurrentState = GameState.Town;
                ShowTownMenu(arriving: false);
                return;
            }

            if (_perkPickInTown)
            {
                if (_hero.PendingPerkPicks > 0)
                {
                    BeginEvent(PerkEvents.Choose(_hero));
                    return;
                }

                _perkPickInTown = false;
                CurrentState = GameState.Town;
                ShowTownMenu();
                return;
            }

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
                _runOutcome = "Turned back";
                EnterTown();
            }
            else if (outcome.Guardian != null)
            {
                _fightingGuardian = true;
                EnterCombat(new[] { outcome.Guardian });
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
            _runOutcome = "Floor cleared";
            EnterTown();
        }

        // Reads out the trip just ended. A trip with nothing in it is skipped, unless it ended in death or an ending.
        private void EndRunTally(string outcome, bool always = false)
        {
            RunTally? run = _run;
            _run = null;
            _runOutcome = "Returned to Oakhaven";
            if (run == null || (!always && run.IsEmpty(_hero)))
                return;

            foreach ((string text, MessageTone tone) in run.Summary(_hero, outcome))
            {
                Say(text, tone);
            }
        }

        // The town description only on arrival; after that, just the choices.
        private void ShowTownMenu(bool arriving = true)
        {
            string turnOption = CanTurnAgain ? ", 6 turn the wheel again" : string.Empty;
            _ui.SetCommandContext($"Town: 1 shop, 2 rest at inn ({RestCost}g), 3 enter dungeon, 4 save, 5 main menu{turnOption}, i / g pack and gear, perks.");
            Say("");
            if (arriving)
            {
                Say("Oakhaven settles around you. The merchant waits nearby, the inn is warm, and the dungeon still looms below.");
            }

            Say($"1 Shop   2 Rest ({RestCost}g)   3 Dungeon   4 Save   5 Main menu{(CanTurnAgain ? "   6 Turn wheel" : string.Empty)}", MessageTone.Dim);
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
            if (_hero.Gold < RestCost)
            {
                Say($"You do not have enough gold to rest at the inn ({RestCost}g).", MessageTone.Dim);
                return;
            }

            _hero.Gold -= RestCost;
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

        // After an ending the hero may turn the wheel again.
        private bool CanTurnAgain => _hero.WheelEnding != WheelEnding.None;

        private int TownChoiceCount => CanTurnAgain ? 6 : 5;

        private int RestCost => WheelTurns.RestCost(_hero.WheelTurn);

        private static string Times(int count) => count switch
        {
            1 => "once",
            2 => "twice",
            _ => $"{count} times"
        };

        private string FloorPrompt => ThroneStillSealed
            ? "Pick a floor 1 to 3, 4 for the sealed throne, or 0 to stay in town."
            : "Pick a floor 1 to 3, or 0 to stay in town.";

        private void ShowFinishedHeroTown()
        {
            switch (_hero.WheelEnding)
            {
                case WheelEnding.TurnedTheWheel:
                    Say("You turned the wheel. Floors 1 to 3 are still yours to walk.", MessageTone.Gold);
                    Say($"Or type 6 to turn it again, to Turn {_hero.WheelTurn + 1}: harder floors, richer pay.", MessageTone.Dim);
                    return;

                case WheelEnding.SteppedOff:
                    Say("You stepped off the wheel. Floors 1 to 3 are still yours to walk.", MessageTone.Gold);
                    Say($"Or type 6 to climb back on, to Turn {_hero.WheelTurn + 1}: harder floors, richer pay.", MessageTone.Dim);
                    return;
            }

            if (_hero.WheelTurn > 0)
            {
                Say($"Turn {_hero.WheelTurn} of the wheel. The throne is sealed again, and floors 1 to 3 wait.", MessageTone.Gold);
                foreach (WheelTwist twist in WheelTurns.InForce(_hero.WheelTurn))
                {
                    Say($"  {twist.Name}: {twist.Description}", MessageTone.Warning);
                }

                if (_hero.WheelTurn > WheelTurns.LastTwistTurn)
                    Say($"  {WheelTurns.GrindLine(_hero.WheelTurn)}", MessageTone.Warning);
            }
            else
            {
                Say("Your story is told. The throne is sealed, but floors 1 to 3 are still yours to walk.", MessageTone.Gold);
            }
            Say($"Keys of the wheel: {_hero.WheelKeyCount} of 3.", MessageTone.Dim);

            // Maren points the way once a session.
            if (_keyRumorShown)
                return;

            _keyRumorShown = true;
            Say("");
            string rumor = _hero.HasAllWheelKeys
                ? "Three keys, and you found every one. Whatever they were made to open, it is waiting on you now."
                : "Old miners swear there are three keys lost down there, one on each floor. If you go looking, look where the dead keep things.";
            Say($"[Inn Keeper] Maren: \"{rumor}\"", MessageTone.Dim);
            if (_hero.CanPickLocks)
            {
                Say("[Inn Keeper] Maren: \"Then again, anything locked three times over usually has a fourth way in.\"", MessageTone.Dim);
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
                _run = new RunTally(_hero, "The Sealed Throne");
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
            _guardianFaced = false;
            _fightingGuardian = false;
            _run = new RunTally(_hero, quest?.Title ?? $"Floor {_hero.ActiveStoryNode}");

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
            bool crowded = WheelTurns.BringsExtraFoe(_hero.WheelTurn);
            if (crowded)
            {
                encounterGroup = encounterGroup.Concat(EncounterTable.RollGroup(_hero.ActiveStoryNode).Take(1)).ToList();
            }

            Say("");
            Say(_onSidePassage
                ? "Something huge unfolds from the dark of the passage."
                : "Another shape stirs in the dark. The dungeon refuses to stay quiet.", MessageTone.Warning);

            if (crowded)
                Say("The dark is crowded this turn of the wheel. One more shape follows the rest.", MessageTone.Warning);

            EnterCombat(encounterGroup);
        }

        private void HandleDefeatStart()
        {
            CurrentState = GameState.DefeatPrompt;
            _ui.SetCommandContext("Type 1 to rise again in Oakhaven (50 gold), or 2 to erase this hero.");
            Say("");
            Say("You fall to the dungeon floor, breath gone and vision dimming.", MessageTone.Damage);
            EndRunTally("Fell in the dark", always: true);
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
            _runOutcome = "Fled";
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
