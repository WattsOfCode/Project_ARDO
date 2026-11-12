/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `GameStateManager.cs` is a concrete game-flow controller that manages town, 
    combat, defeat, and game-over states. It coordinates public methods, 
    constructors, and state transitions while delegating CRUD to repository 
    classes.
 */
using System;
using System.Collections.Generic;
using ARDO.Core.Abstracts;
using ARDO.Core.Entities;
using ARDO.Core.Enums;
using ARDO.Data;
using ARDO.Data.SQLite;
using ARDO.Game.Combat;
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
        private bool _townShopOpen;

        public GameState CurrentState { get; private set; } = GameState.Town;

        public GameStateManager(MainGameForm ui, Hero hero, IPlayerRepository playerRepo, QuestRepository questRepo)
        {
            _ui = ui;
            _hero = hero;
            _playerRepo = playerRepo;
            _questRepo = questRepo ?? throw new ArgumentNullException(nameof(questRepo));

            _combatEngine = new CombatEngine(_ui, _playerRepo);
        }

        public void BeginCampaign(bool isNewGame)
        {
            _ui.ClearNarrative();
            _ui.AppendNarrative("");
            _ui.AppendNarrative("==========================================================");
            _ui.AppendNarrative("                   ARDO: THE FIRST DESCENT");
            _ui.AppendNarrative("==========================================================");
            _ui.AppendNarrative("");

            if (isNewGame)
            {
                _ui.AppendNarrative("In the Beginning, the Progenitor ordained a sacred geometry: flesh was meant to rise from the dust,");
                _ui.AppendNarrative("knit itself over ancient stone, and age backward into a state of pure, unblemished light.");
                _ui.AppendNarrative("But He grew cruel. With a cosmic wrench, He inverted the wheel.");
                _ui.AppendNarrative("");
                _ui.AppendNarrative("What humanity celebrates as \"birth\" is actually the beginning of the end.");
                _ui.AppendNarrative("We are born soft and fragile, slowly rotting into the rigid skeletons of His design.");
                _ui.AppendNarrative("Our entire existence is a countdown in reverse.");
                _ui.AppendNarrative("");
                _ui.AppendNarrative("You wake in Oakhaven's inn with the taste of ash in your mouth.");
                _ui.AppendNarrative("The Inn Keeper says the first bell has already passed and the Maw below town has opened again.");
                _ui.AppendNarrative("You must descend into the primordial dark, butcher the guardians of the twisted timeline,");
                _ui.AppendNarrative("and challenge the God-King who turned our growth into a slow, living decay.");
            }
            else
            {
                _ui.AppendNarrative("You return to the world with old scars and unfinished business.");
                _ui.AppendNarrative("The dungeon still waits below Oakhaven, and the final throne remains untouched.");
            }

            _ui.AppendNarrative("");
            EnterTown();
        }

        public void EnterTown()
        {
            CurrentState = GameState.Town;
            _encountersRemainingInRun = 0;

            _ui.ShowPlayer(_hero);
            _ui.AppendNarrative("");
            _ui.AppendNarrative("==========================================================");
            _ui.AppendNarrative("                        OAKHAVEN");
            _ui.AppendNarrative("==========================================================");
            _ui.AppendNarrative("");

            QuestRepository.QuestSummary? quest = _questRepo.GetQuestSummary(_hero.ActiveStoryNode);
            var npc = _questRepo.GetNpcForQuest(_hero.ActiveStoryNode);

            if (quest != null)
            {
                _ui.AppendNarrative($"Quest { _hero.ActiveStoryNode }: {quest.Title}");
                _ui.AppendNarrative(quest.NarrativeIntro);
                _ui.AppendNarrative("");

                if (quest.IsFinalNode)
                {
                    _ui.AppendNarrative("The final chamber lies beneath the town. The king waits there, beyond mercy.");
                }
                else
                {
                    _ui.AppendNarrative("Another section of the dungeon has opened. Clear it, return, and press deeper.");
                }
            }

            if (npc != null)
            {
                _ui.AppendNarrative($"[{npc.Value.Role}] {npc.Value.Name}: \"{npc.Value.Dialogue}\"");
                _ui.AppendNarrative("");
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
                    _ui.AppendNarrative("[SYSTEM] Game state saved to SQLite.");
                    _ui.AppendNarrative("");
                    return;

                case "5":
                case "exit":
                case "mainmenu":
                case "menu":
                    _ui.AppendNarrative("");
                    _ui.AppendNarrative("You leave Oakhaven and return to the main menu.");
                    _ui.ReturnToMainMenu();
                    return;

                default:
                    _ui.AppendNarrative("Unknown town command. Check the input panel for the current actions.");
                    break;
            }

            if (CurrentState == GameState.Town && showTownMenu)
            {
                _ui.AppendNarrative("");
                ShowTownMenu();
            }
        }

        private void ProcessCombatCommand(string command)
        {
            if (!_combatEngine.IsCombatActive)
                return;

            _combatEngine.ExecuteTurn(command.Trim().ToLowerInvariant());

            if (!_combatEngine.IsCombatActive)
            {
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
                        _ui.AppendNarrative("The epilogue is complete. Type anything and press Enter to return to the start screen.");
                        return;
                    }

                    HandleDefeatStart();
                    return;
                }

                HandlePostCombatVictory();

                if (_encountersRemainingInRun > 0)
                {
                    _ui.AppendNarrative("");
                    _ui.AppendNarrative($"The dungeon still has {_encountersRemainingInRun} more encounter(s) before you can return.");
                    StartNextDungeonEncounter();
                    return;
                }

                CompleteDungeonRun();
            }
        }

        private void HandlePostCombatVictory()
        {
            GameItem? loot = LootGenerator.GenerateLoot();

            if (loot != null)
            {
                _ui.AppendNarrative("");
                if (_hero.HeroInventory.AddItem(loot))
                {
                    _playerRepo.SaveInventoryItem(_hero.CharacterId, loot);
                    _ui.AppendNarrative($"Loot acquired: {loot.Name}");

                    // Wear found gear right away when it beats what is equipped.
                    if (_hero.Equipment.IsUpgrade(loot))
                    {
                        _hero.Equipment.Equip(loot);
                        _playerRepo.SaveEquipment(_hero);
                        _ui.AppendNarrative($"You equip the {loot.Name}.");
                    }
                }
                else
                {
                    _ui.AppendNarrative($"You find {loot.Name}, but your pack is full. Sell something in town to make room.");
                }
            }

            _encountersRemainingInRun = Math.Max(0, _encountersRemainingInRun - 1);
            _playerRepo.SaveGameState(_hero);
        }

        private void CompleteDungeonRun()
        {
            QuestRepository.QuestSummary? quest = _questRepo.GetQuestSummary(_hero.ActiveStoryNode);
            if (quest != null)
            {
                _ui.AppendNarrative("");
                _ui.AppendNarrative("The dungeon section collapses behind you. Town will mark the next descent.");
                _hero.ActiveStoryNode = Math.Min(EncounterTable.FinalQuestId, _hero.ActiveStoryNode + 1);
                _ui.AppendNarrative($"Your journey advances to quest node {_hero.ActiveStoryNode}.");
            }

            _playerRepo.SaveGameState(_hero);
            EnterTown();
        }

        private void ShowTownMenu()
        {
            _ui.SetCommandContext("Town commands: 1 shop and gear, 2 rest at inn (10g), 3 enter dungeon, 4 save game, 5 exit to main menu.");
            _ui.AppendNarrative("");
            _ui.AppendNarrative("Oakhaven settles around you. The merchant waits nearby, the inn is warm, and the dungeon still looms below.");
            _ui.AppendNarrative("Choose your next action using the input panel below.");
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
                _ui.AppendNarrative("");
                _ui.AppendNarrative("You return from the town shop and step back into Oakhaven.");
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
                _ui.AppendNarrative("You do not have enough gold to rest at the inn.");
                return;
            }

            _hero.Gold -= 10;
            _hero.RestoreForTown();
            _ui.AppendNarrative("");
            _ui.AppendNarrative("You rest at the inn and recover your strength.");
            _ui.AppendNarrative($"You now have {_hero.CurrentHealth}/{_hero.MaxHealth} HP.");
            _playerRepo.SaveGameState(_hero);
            _ui.UpdateVisualMetricsDisplay();
        }

        private void StartDungeonRun()
        {
            QuestRepository.QuestSummary? quest = _questRepo.GetQuestSummary(_hero.ActiveStoryNode);
            _encountersRemainingInRun = EncounterTable.FightsPerRun(_hero.ActiveStoryNode);

            _ui.AppendNarrative("");
            _ui.AppendNarrative(quest != null
                ? $"You leave town and descend into the dungeon: {quest.Title}."
                : "You leave town and descend into the dungeon.");
            _ui.AppendNarrative("The air grows colder as the stone closes around you.");
            StartNextDungeonEncounter();
        }

        private void StartNextDungeonEncounter()
        {
            IReadOnlyList<Enemy> encounterGroup = EncounterTable.RollGroup(_hero.ActiveStoryNode);

            if (_encountersRemainingInRun > 0)
            {
                _ui.AppendNarrative("");
                _ui.AppendNarrative("Another shape stirs in the dark. The dungeon refuses to stay quiet.");
            }

            EnterCombat(encounterGroup);
        }

        private void HandleDefeatStart()
        {
            CurrentState = GameState.DefeatPrompt;
            _ui.AppendNarrative("");
            _ui.AppendNarrative("You fall to the dungeon floor, breath gone and vision dimming.");
            _ui.AppendNarrative("Continue your run? Type 1 / yes or 2 / no.");
            _ui.AppendNarrative("Choosing no deletes the current character profile and returns to the main menu.");
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
                    _playerRepo.SaveGameState(_hero);
                    _ui.AppendNarrative("");
                    _ui.AppendNarrative("Rise, hero. There is still much more for you to do.");
                    _ui.AppendNarrative("");
                    CurrentState = GameState.Town;
                    EnterTown();
                    break;

                case "no":
                case "n":
                case "2":
                    _playerRepo.DeleteProfile(_hero.CharacterId);
                    _ui.AppendNarrative("");
                    _ui.AppendNarrative("Your character is erased from the records of Oakhaven.");
                    _ui.ReturnToMainMenu();
                    break;

                default:
                    _ui.AppendNarrative("Please type yes or no.");
                    break;
            }
        }

        private void HandleSuccessfulFlee()
        {
            _ui.AppendNarrative("");
            _ui.AppendNarrative("You break away from the dungeon floor and retreat to Oakhaven before it can close.");
            _playerRepo.SaveGameState(_hero);
            EnterTown();
        }

        private void ProcessGameOverCommand()
        {
            _ui.ReturnToMainMenu();
        }

    }
}
