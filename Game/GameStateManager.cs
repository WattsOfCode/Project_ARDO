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
        private bool _townShopOpen;
        private DungeonEvent? _activeEvent;
        private bool _onSidePassage;
        private bool _inAmbush;

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
                Say("In the Beginning, the Progenitor ordained a sacred geometry: flesh was meant to rise from the dust,");
                Say("knit itself over ancient stone, and age backward into a state of pure, unblemished light.");
                Say("But He grew cruel. With a cosmic wrench, He inverted the wheel.");
                Say("");
                Say("What humanity celebrates as \"birth\" is actually the beginning of the end.");
                Say("We are born soft and fragile, slowly rotting into the rigid skeletons of His design.");
                Say("Our entire existence is a countdown in reverse.");
                Say("");
                Say("You wake in Oakhaven's inn with the taste of ash in your mouth.");
                Say("The Inn Keeper says the first bell has already passed and the Maw below town has opened again.");
                Say("You must descend into the primordial dark, butcher the guardians of the twisted timeline,");
                Say("and challenge the God-King who turned our growth into a slow, living decay.");
            }
            else
            {
                Say("You return to the world with old scars and unfinished business.");
                Say("The dungeon still waits below Oakhaven, and the final throne remains untouched.");
            }

            Say("");
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

            if (quest != null)
            {
                Say($"Quest {_hero.ActiveStoryNode}: {quest.Title}", MessageTone.Gold);
                Say(quest.NarrativeIntro);
                Say("");

                Say(quest.IsFinalNode
                    ? "The final chamber lies beneath the town. The king waits there, beyond mercy."
                    : "Another section of the dungeon has opened. Clear it, return, and press deeper.");
            }

            if (npc != null)
            {
                Say($"[{npc.Value.Role}] {npc.Value.Name}: \"{npc.Value.Dialogue}\"", MessageTone.Dim);
                Say("");
            }

            ShowTownMenu();
        }

        public void EnterCombat(IReadOnlyList<Enemy> enemies)
        {
            CurrentState = GameState.Combat;
            _combatEngine.StartEncounter(_hero, enemies);
        }

        public void ProcessCommand(string command)
        {
            string normalized = command.Trim().ToLower();

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
                case "gear":
                case "equip":
                case "inventory":
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
                    Say("[SYSTEM] Game state saved to SQLite.", MessageTone.Dim);
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
                    Say("Unknown town command. Check the input panel for the current actions.", MessageTone.Dim);
                    break;
            }

            if (CurrentState == GameState.Town && showTownMenu)
            {
                Say("");
                ShowTownMenu();
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
                    _ui.SetCommandContext("Type anything and press Enter to return to the start screen.");
                    Say("The epilogue is complete. Type anything and press Enter to return to the start screen.", MessageTone.Dim);
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

            _ui.SetCommandContext($"Choose 1 to {dungeonEvent.Choices.Count}.");
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
                Say("The dungeon section collapses behind you. Town will mark the next descent.");
                _hero.ActiveStoryNode = Math.Min(EncounterTable.FinalQuestId, _hero.ActiveStoryNode + 1);
                Say($"Your journey advances to quest node {_hero.ActiveStoryNode}.", MessageTone.Gold);
            }

            _playerRepo.SaveGameState(_hero);
            EnterTown();
        }

        private void ShowTownMenu()
        {
            _ui.SetCommandContext("Town commands: 1 shop and gear, 2 rest at inn (10g), 3 enter dungeon, 4 save game, 5 exit to main menu.");
            Say("");
            Say("Oakhaven settles around you. The merchant waits nearby, the inn is warm, and the dungeon still looms below.");
            Say("1. Shop and gear    2. Rest at the inn (10g)    3. Enter the dungeon    4. Save    5. Main menu", MessageTone.Dim);
        }

        private void OpenTownShop()
        {
            if (_townShopOpen)
                return;

            _townShopOpen = true;
            var shopPanel = new ShopPanel(_hero, _playerRepo, _ui.UpdateVisualMetricsDisplay);
            shopPanel.CloseRequested += (_, __) =>
            {
                _townShopOpen = false;
                _ui.RestoreNarrativePanel();
                _ui.UpdateVisualMetricsDisplay();
                Say("");
                Say("You return from the town shop and step back into Oakhaven.");
                ShowTownMenu();
                _ui.FocusCommandInput();
            };

            _ui.ShowShopOverlay(shopPanel);
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
            _ui.SetCommandContext("Type 1 / yes to continue (costs 50 gold), or 2 / no to delete this character.");
            Say("");
            Say("You fall to the dungeon floor, breath gone and vision dimming.", MessageTone.Damage);
            Say("Continue your run? Type 1 / yes or 2 / no.");
            Say("Choosing no deletes the current character profile and returns to the main menu.", MessageTone.Warning);
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
                    Say("Please type yes or no.", MessageTone.Dim);
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
