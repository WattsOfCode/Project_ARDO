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
        // Chance of an event between two fights of a dungeon run.
        private const int EventChancePercent = 45;

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

        // The quest whose story has been told this session, so a return to town doesn't retell it.
        private int _introShownForQuest;

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
                Say("In the Beginning, the Progenitor ordained a sacred geometry: flesh was meant to rise from the dust, " +
                    "knit itself over ancient stone, and age backward into a state of pure, unblemished light. " +
                    "But He grew cruel. With a cosmic wrench, He inverted the wheel.");
                Say("");
                Say("What humanity celebrates as \"birth\" is actually the beginning of the end. " +
                    "We are born soft and fragile, slowly rotting into the rigid skeletons of His design. " +
                    "Our entire existence is a countdown in reverse.");
                Say("");
                Say("The Maw below Oakhaven has opened again. You must descend into the primordial dark, " +
                    "butcher the guardians of the twisted timeline, and challenge Alhuezo, the God-King who turned our growth into a slow, living decay.");
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
            _activeEvent = null;
            _hero.EndDungeonRun();

            _ui.ShowPlayer(_hero);
            _ui.UpdateVisualMetricsDisplay();
            Say("");
            Say("==========================================================", MessageTone.Header);
            Say("                        OAKHAVEN", MessageTone.Header);
            Say("==========================================================", MessageTone.Header);
            Say("");

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

                    // The story is over for this hero: the load list shows it, and it can't be replayed.
                    _hero.StoryComplete = true;
                    _playerRepo.SaveGameState(_hero);
                    _ui.SetCommandContext("Type anything and press Enter to return to the start screen.");
                    Say("The epilogue is complete.", MessageTone.Dim);
                    return;
                }

                HandleDefeatStart();
                return;
            }

            HandlePostCombatVictory();
            ContinueDungeonRun();
        }

        // What comes after a won fight or a finished event.
        private void ContinueDungeonRun()
        {
            if (_encountersRemainingInRun > 0)
            {
                if (Random.Shared.Next(100) < EventChancePercent)
                {
                    BeginEvent(DungeonEvents.RollRandom());
                    return;
                }

                StartNextDungeonEncounter();
                return;
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
                _ui.SetCommandContext($"Choose 1 to {_activeEvent.Choices.Count}.");
            }
        }

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
                Say($"Choose 1 to {dungeonEvent.Choices.Count}.", MessageTone.Dim);
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

            if (outcome.ReturnToTown)
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
                EnterCombat(EncounterTable.RollGroup(_hero.ActiveStoryNode));
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

        private void StartDungeonRun()
        {
            QuestRepository.QuestSummary? quest = _questRepo.GetQuestSummary(_hero.ActiveStoryNode);
            _encountersRemainingInRun = EncounterTable.FightsPerRun(_hero.ActiveStoryNode);
            _onSidePassage = false;
            _inAmbush = false;

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
