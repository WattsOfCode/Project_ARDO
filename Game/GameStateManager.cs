using System;
using System.Collections.Generic;
using ARDO.Core.Abstracts;
using ARDO.Core.Entities;
using ARDO.Core.Enums;
using ARDO.Data.SQLite;
using ARDO.Game.Combat;
using ARDO.Game.Inventory;
using ARDO.Game.Enemies;
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
        private readonly List<GameItem> _recentLoot = new();
        private bool _shopOpen;
        private int _encountersRemainingInRun;

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
            _shopOpen = false;
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
            if (_shopOpen)
            {
                ProcessShopCommand(command);
                return;
            }

            bool showTownMenu = true;

            switch (command)
            {
                case "1":
                case "buy":
                case "shop":
                    _shopOpen = true;
                    ShowShopMenu();
                    showTownMenu = false;
                    break;

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
                    _ui.ReturnToMainMenu();
                    return;

                case "5":
                case "loot":
                case "inventory":
                    ShowLootInventory();
                    break;

                default:
                    _ui.AppendNarrative("Unknown town command. Available options: 1 buy, 2 rest, 3 dungeon, 4 save, 5 loot.");
                    break;
            }

            if (CurrentState == GameState.Town && showTownMenu)
            {
                _ui.AppendNarrative("");
                ShowTownMenu();
            }
        }

        private void ProcessShopCommand(string command)
        {
            switch (command)
            {
                case "1":
                case "weapon":
                    PurchaseShopItem(
                        40,
                        new Weapon("Iron Blade", "A sturdy blade from the Oakhaven merchant.", RarityType.Common, 40, 10),
                        "The merchant sharpens your edge. Attack rises by 10.",
                        () => _hero.WeaponBonusDamage += 10);
                    break;

                case "2":
                case "armor":
                    PurchaseShopItem(
                        30,
                        new Armor("Reinforced Mail", "Armor fit for the next descent.", RarityType.Common, 30, 5, EquipmentSlot.Chest),
                        "Your armor is reinforced. Defense rises by 5.",
                        () => _hero.ShieldBonusDefense += 5);
                    break;

                case "3":
                case "tonic":
                case "potion":
                    PurchaseShopItem(
                        50,
                        new Consumable("Tonic of Hardening", "A one-time draught that steadies the body.", RarityType.Common, 50, 25),
                        "You buy a tonic of hardening. Invulnerability charges rise by 2.",
                        () => _hero.InvulnerabilityCharges += 2);
                    break;

                case "4":
                case "back":
                    _shopOpen = false;
                    _ui.AppendNarrative("You step away from the merchant and return to the town square.");
                    break;

                default:
                    _ui.AppendNarrative("Invalid shop choice. Available options: 1 weapon, 2 armor, 3 tonic, 4 back.");
                    return;
            }

            _ui.UpdateVisualMetricsDisplay();
            _playerRepo.SaveGameState(_hero, _hero.ActiveStoryNode);

            if (_shopOpen)
            {
                ShowShopMenu();
            }
            else
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
                _recentLoot.Add(loot);
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
            _ui.AppendNarrative("");
            _ui.AppendNarrative("Town options:");
            _ui.AppendNarrative("1. Buy supplies");
            _ui.AppendNarrative("2. Rest at the inn");
            _ui.AppendNarrative("3. Return to the dungeon");
            _ui.AppendNarrative("4. Save game");
            _ui.AppendNarrative("5. View loot and inventory");
            _ui.AppendNarrative("Type 1 / buy / shop, 2 / rest, 3 / dungeon / return, 4 / save, or 5 / loot / inventory:");
        }

        private void ShowShopMenu()
        {
            _ui.AppendNarrative("");
            _ui.AppendNarrative("Merchant stock:");
            _ui.AppendNarrative("1. Sharpen weapon - 40 gold");
            _ui.AppendNarrative("2. Reinforce armor - 30 gold");
            _ui.AppendNarrative("3. Buy tonic - 50 gold");
            _ui.AppendNarrative("4. Back to town");
            _ui.AppendNarrative("Type 1 / weapon, 2 / armor, 3 / tonic, or 4 / back:");
        }

        private void ShowLootInventory()
        {
            _ui.AppendNarrative("");
            _ui.AppendNarrative("Loot and inventory:");

            if (_recentLoot.Count > 0)
            {
                _ui.AppendNarrative("Recent loot:");
                for (int i = 0; i < _recentLoot.Count; i++)
                {
                    GameItem item = _recentLoot[i];
                    _ui.AppendNarrative($"  {i + 1}. {item}");
                }
                _ui.AppendNarrative("");
            }

            List<GameItem> items = _hero.HeroInventory.GetItems();
            if (items.Count == 0)
            {
                _ui.AppendNarrative("Your pack is empty.");
            }
            else
            {
                for (int i = 0; i < items.Count; i++)
                {
                    GameItem item = items[i];
                    string equipped = item.IsEquipped ? " [equipped]" : string.Empty;
                    string consumable = item.ItemType == ItemType.Consumable ? $" [consumable: {item.HealAmount} heal]" : string.Empty;
                    _ui.AppendNarrative($"  {i + 1}. {item.Name}{equipped}{consumable}");
                }
            }

            _ui.AppendNarrative("");
            _ui.AppendNarrative("Type 1 / buy / shop, 2 / rest, 3 / dungeon / return, 4 / save, or 5 / loot / inventory:");
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

        private void PurchaseShopItem(int cost, GameItem item, string successMessage, Action upgradeAction)
        {
            if (_hero.Gold < cost)
            {
                _ui.AppendNarrative("You do not have enough gold for that purchase.");
                return;
            }

            _hero.Gold -= cost;
            _hero.HeroInventory.AddItem(item);
            _playerRepo.SaveInventoryItem(_hero.CharacterId, item);
            upgradeAction();
            _ui.AppendNarrative("");
            _ui.AppendNarrative(successMessage);
            _ui.AppendNarrative($"Added to inventory: {item.Name}");
            _playerRepo.SaveGameState(_hero, _hero.ActiveStoryNode);
        }
    }
}
