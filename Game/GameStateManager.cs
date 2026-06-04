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
using ARDO.Data.SQLite;
using ARDO.Game.Combat;
using ARDO.Game.Inventory;
using ARDO.Game.Enemies;
using ARDO.UI.Controls;
using ARDO.UI.Forms;

namespace ARDO.Game
{
    public class GameStateManager
    {
        private readonly MainGameForm _ui;
        private readonly Hero _hero;
        private readonly PlayerRepository _playerRepo;
        private readonly QuestRepository _questRepo;
        private readonly CombatEngine _combatEngine;
        private int _encountersRemainingInRun;
        private bool _townShopOpen;

        public GameState CurrentState { get; private set; } = GameState.Town;

        public GameStateManager(MainGameForm ui, Hero hero, PlayerRepository playerRepo)
        {
            _ui = ui;
            _hero = hero;
            _playerRepo = playerRepo;
            _questRepo = new QuestRepository(new DatabaseConfig());

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

        public void EnterCombat(Enemy enemy)
        {
            CurrentState = GameState.Combat;
            _combatEngine.StartEncounter(_hero, enemy);
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
                    _playerRepo.SaveGameState(_hero, _hero.ActiveStoryNode);
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
                    _ui.AppendNarrative("Unknown town command. Check the input panel for the available actions.");
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
                        _ui.AppendNarrative("Press any key to return to the start screen.");
                        return;
                    }

                    HandleDefeatStart();
                    return;
                }

                if (HandlePostCombatVictory())
                {
                    return;
                }

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

        private bool HandlePostCombatVictory()
        {
            GameItem? loot = LootGenerator.GenerateLoot();

            if (loot != null)
            {
                _ui.AppendNarrative("");
                _hero.HeroInventory.AddItem(loot);
                _playerRepo.SaveInventoryItem(_hero.CharacterId, loot);
                _ui.AppendNarrative($"Loot acquired: {loot.Name}");
            }

            QuestRepository.QuestSummary? quest = _questRepo.GetQuestSummary(_hero.ActiveStoryNode);
            if (quest != null && quest.IsFinalNode)
            {
                _ui.AppendNarrative("");
                _ui.AppendNarrative("The final chamber should have ended the world.");
                _ui.AppendNarrative("The First King falls silent, and the cavern keeps your last breath as a relic.");
                _playerRepo.SaveGameState(_hero, _hero.ActiveStoryNode);
                CurrentState = GameState.GameOver;
                return true;
            }

            _encountersRemainingInRun = Math.Max(0, _encountersRemainingInRun - 1);

            _playerRepo.SaveGameState(_hero, _hero.ActiveStoryNode);
            return false;
        }

        private void CompleteDungeonRun()
        {
            QuestRepository.QuestSummary? quest = _questRepo.GetQuestSummary(_hero.ActiveStoryNode);
            if (quest != null)
            {
                _ui.AppendNarrative("");
                _ui.AppendNarrative("The dungeon section collapses behind you. Town will mark the next descent.");
                _hero.ActiveStoryNode++;
                _ui.AppendNarrative($"Your journey advances to quest node {_hero.ActiveStoryNode}.");
            }

            _playerRepo.SaveGameState(_hero, _hero.ActiveStoryNode);
            EnterTown();
        }

        private void ShowTownMenu()
        {
            _ui.SetCommandContext("Town commands: 1 buy from shop, 2 rest at inn, 3 enter dungeon, 4 save, 5 exit.");
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

            if (_hero is ARDO.Game.Players.Mage mage)
            {
                mage.CurrentMana = mage.MaxMana;
            }

            _hero.IsRestingInSafety = true;
            _ui.AppendNarrative("");
            _ui.AppendNarrative("You rest at the inn and recover your strength.");
            _ui.AppendNarrative($"You now have {_hero.CurrentHealth}/{_hero.MaxHealth} HP.");
            _playerRepo.SaveGameState(_hero, _hero.ActiveStoryNode);
            _ui.UpdateVisualMetricsDisplay();
        }

        private void StartDungeonRun()
        {
            QuestRepository.QuestSummary? quest = _questRepo.GetQuestSummary(_hero.ActiveStoryNode);
            _encountersRemainingInRun = GetEncounterCountForQuest(_hero.ActiveStoryNode);

            _ui.AppendNarrative("");
            _ui.AppendNarrative(quest != null
                ? $"You leave town and descend into the dungeon: {quest.Title}."
                : "You leave town and descend into the dungeon.");
            _ui.AppendNarrative("The air grows colder as the stone closes around you.");
            StartNextDungeonEncounter();
        }

        private void StartNextDungeonEncounter()
        {
            IReadOnlyList<Enemy> encounterGroup = _questRepo.GetEncounterGroupForQuest(_hero.ActiveStoryNode);

            if (_encountersRemainingInRun > 0)
            {
                _ui.AppendNarrative("");
                _ui.AppendNarrative("Another shape stirs in the dark. The dungeon refuses to stay quiet.");
            }

            EnterCombat(encounterGroup);
        }

        private static int GetEncounterCountForQuest(int questId)
        {
            return questId switch
            {
                1 => 2,
                2 => 3,
                3 => 3,
                4 => 1,
                _ => 2
            };
        }

        private void HandleDefeatStart()
        {
            CurrentState = GameState.DefeatPrompt;
            _ui.AppendNarrative("");
            _ui.AppendNarrative("You fall to the dungeon floor, breath gone and vision dimming.");
            _ui.AppendNarrative("Would you like to continue? Type 1 / yes or 2 / no.");
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
                    if (_hero is ARDO.Game.Players.Mage mage)
                    {
                        mage.CurrentMana = mage.MaxMana;
                    }

                    _hero.IsRestingInSafety = true;
                    _playerRepo.SaveGameState(_hero, _hero.ActiveStoryNode);
                    _ui.AppendNarrative("");
                    _ui.AppendNarrative("Rise hero there is still much more for you to do.");
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
            _playerRepo.SaveGameState(_hero, _hero.ActiveStoryNode);
            EnterTown();
        }

        private void ProcessGameOverCommand()
        {
            _ui.ReturnToMainMenu();
        }

    }
}
