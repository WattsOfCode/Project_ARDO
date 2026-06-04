/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `CombatEngine.cs` is a concrete combat controller that drives turn-based 
    encounters through polymorphic hero and enemy behavior. It uses public 
    combat methods, private battle state, and repository saves after fights, 
    while CRUD itself stays in the data layer.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using ARDO.Core.Abstracts;
using ARDO.Core.Interfaces;
using ARDO.Core.Entities;
using ARDO.Data.SQLite;
using ARDO.Game.Players;
using ARDO.UI.Forms;

namespace ARDO.Game.Combat
{
    public class CombatEngine
    {
        private readonly MainGameForm _form;
        private readonly PlayerRepository _playerRepo;
        private readonly List<Enemy> _enemies = new();
        private Hero _player = null!;
        private bool _isFinalBossEncounter;
        private int _finalBossRounds;
        private bool _finalBossEndingPlayed;
        private int _specialCooldownTurnsRemaining;
        private bool _awaitingSpecialChoice;
        private bool _awaitingItemChoice;
        private List<Consumable> _pendingConsumables = new();
        public bool LastActionWasFleeSuccessful { get; private set; }

        private enum PlayerActionOutcome
        {
            ContinueTurn,
            Fled,
            NoTurnSpent
        }

        public bool IsCombatActive { get; private set; }

        public bool FinalBossEndingPlayed => _finalBossEndingPlayed;

        public CombatEngine(MainGameForm form, PlayerRepository playerRepo)
        {
            _form = form ?? throw new ArgumentNullException(nameof(form));
            _playerRepo = playerRepo ?? throw new ArgumentNullException(nameof(playerRepo));
        }

        public void StartEncounter(Hero player, Enemy enemy)
        {
            StartEncounter(player, new[] { enemy });
        }

        public void StartEncounter(Hero player, IReadOnlyList<Enemy> enemies)
        {
            _player = player;
            _enemies.Clear();
            _enemies.AddRange(enemies?.Where(enemy => enemy != null) ?? Array.Empty<Enemy>());
            if (_enemies.Count == 0)
            {
                _enemies.Add(new ARDO.Game.Enemies.Goblin());
            }

            foreach (Enemy enemy in _enemies)
            {
                enemy.CombatFloor = _player.ActiveStoryNode;
            }

            IsCombatActive = true;
            _isFinalBossEncounter = _enemies.Any(enemy => enemy.Name.Contains("Alhuezo", StringComparison.OrdinalIgnoreCase));
            _finalBossRounds = 0;
            _finalBossEndingPlayed = false;
            _specialCooldownTurnsRemaining = 0;
            _awaitingSpecialChoice = false;
            _awaitingItemChoice = false;
            _pendingConsumables.Clear();
            LastActionWasFleeSuccessful = false;

            _form.SetCommandContext("Combat commands: 1 attack, 2 special, 3 flee, 4 item.");
            _form.AppendNarrative("");
            _form.AppendNarrative("==========================================================");
            _form.AppendNarrative($"COMBAT INITIATED: {_player.Name} vs {_enemies.Count} foe(s)");
            _form.AppendNarrative("==========================================================");
            ShowActiveEnemy();

            _form.AppendNarrative($"The dungeon answers with {_enemies.Count} presence(s).");
            if (_isFinalBossEncounter)
            {
                _form.AppendNarrative("The deepest chamber hushes around you.");
                _form.AppendNarrative("Somewhere beyond the dark, the First King waits with patient certainty.");
                _form.AppendNarrative("This is not just another fight. It is the end of the road you have been walking.");
            }

            if (_enemies.Count > 1)
            {
                _form.AppendNarrative($"Enemies assembled: {string.Join(", ", _enemies.Select(enemy => enemy.Name))}");
            }

            UpdateBattlefieldDisplay();
        }

        public void ExecuteTurn(string playerChoice)
        {
            if (!IsCombatActive || !_player.IsAlive || !HasLivingEnemies())
                return;

            Enemy? activeEnemy = GetActiveEnemy();
            if (activeEnemy == null)
            {
                ResolveEncounter();
                return;
            }

            if (_awaitingSpecialChoice)
            {
                if (!ProcessSpecialChoice(playerChoice, _player, activeEnemy))
                {
                    return;
                }

                _awaitingSpecialChoice = false;

                if (!HasLivingEnemies())
                {
                    ResolveEncounter();
                    return;
                }

                if (_isFinalBossEncounter)
                {
                    ResolveFinalBossTurn(activeEnemy);
                    return;
                }

                _form.AppendNarrative("");
                foreach (Enemy enemy in _enemies.Where(enemy => enemy.IsAlive))
                {
                    _form.AppendNarrative($"[{enemy.Name}'s turn]");
                    int beforeHealth = _player.CurrentHealth;
                    enemy.ExecuteAI(_player);
                    int dealt = Math.Max(0, beforeHealth - _player.CurrentHealth);
                    _form.AppendNarrative(DescribeEnemyAttack(enemy, dealt));

                    if (!_player.IsAlive)
                    {
                        break;
                    }
                }

                TickSpecialCooldown();

                if (!_player.IsAlive)
                {
                    ResolveEncounter();
                    return;
                }

                if (!HasLivingEnemies())
                {
                    ResolveEncounter();
                    return;
                }

                UpdateBattlefieldDisplay();
                return;
            }

            if (_awaitingItemChoice)
            {
                if (!ProcessItemChoice(playerChoice, _player))
                {
                    return;
                }

                _awaitingItemChoice = false;

                if (!HasLivingEnemies())
                {
                    ResolveEncounter();
                    return;
                }

                if (_isFinalBossEncounter)
                {
                    ResolveFinalBossTurn(activeEnemy);
                    return;
                }

                _form.AppendNarrative("");
                foreach (Enemy enemy in _enemies.Where(enemy => enemy.IsAlive))
                {
                    _form.AppendNarrative($"[{enemy.Name}'s turn]");
                    int beforeHealth = _player.CurrentHealth;
                    enemy.ExecuteAI(_player);
                    int dealt = Math.Max(0, beforeHealth - _player.CurrentHealth);
                    _form.AppendNarrative(DescribeEnemyAttack(enemy, dealt));

                    if (!_player.IsAlive)
                    {
                        break;
                    }
                }

                TickSpecialCooldown();

                if (!_player.IsAlive)
                {
                    ResolveEncounter();
                    return;
                }

                if (!HasLivingEnemies())
                {
                    ResolveEncounter();
                    return;
                }

                UpdateBattlefieldDisplay();
                return;
            }

            PlayerActionOutcome playerOutcome = ProcessPlayerAction(playerChoice, _player, activeEnemy);
            if (_awaitingSpecialChoice)
            {
                return;
            }

            if (playerOutcome == PlayerActionOutcome.NoTurnSpent)
            {
                return;
            }

            if (playerOutcome == PlayerActionOutcome.Fled)
            {
                EndCombat();
                return;
            }

            if (_isFinalBossEncounter)
            {
                ResolveFinalBossTurn(activeEnemy);
                return;
            }

            if (!HasLivingEnemies())
            {
                ResolveEncounter();
                return;
            }

            _form.AppendNarrative("");
            foreach (Enemy enemy in _enemies.Where(enemy => enemy.IsAlive))
            {
                _form.AppendNarrative($"[{enemy.Name}'s turn]");
                int beforeHealth = _player.CurrentHealth;
                enemy.ExecuteAI(_player);
                int dealt = Math.Max(0, beforeHealth - _player.CurrentHealth);
                _form.AppendNarrative(DescribeEnemyAttack(enemy, dealt));

                if (!_player.IsAlive)
                {
                    break;
                }
            }

            TickSpecialCooldown();

            if (!_player.IsAlive)
            {
                ResolveEncounter();
                return;
            }

            if (!HasLivingEnemies())
            {
                ResolveEncounter();
                return;
            }

            UpdateBattlefieldDisplay();
        }

        private void UpdateBattlefieldDisplay()
        {
            _form.UpdateVisualMetricsDisplay();

            _form.AppendNarrative("\n----------------------------------------------------------");
            if (_player is Mage mage)
            {
                _form.AppendNarrative($"{_player.Name} (HP: {_player.CurrentHealth}/{_player.MaxHealth} | Mana: {mage.CurrentMana}/{mage.MaxMana})");
            }
            else
            {
                _form.AppendNarrative($"{_player.Name} (HP: {_player.CurrentHealth}/{_player.MaxHealth})");
            }

            Enemy? activeEnemy = GetActiveEnemy();
            if (activeEnemy != null)
            {
                _form.AppendNarrative($"Target: {activeEnemy.Name} (HP: {activeEnemy.CurrentHealth}/{activeEnemy.MaxHealth})");
            }

            _form.AppendNarrative($"Enemies remaining: {_enemies.Count(enemy => enemy.IsAlive)}");
            _form.AppendNarrative("----------------------------------------------------------");
            _form.AppendNarrative("1. Basic Attack");
            _form.AppendNarrative("2. Execute Special Ability");
            _form.AppendNarrative("3. Attempt to Flee (40% chance)");
            _form.AppendNarrative("4. Use Item");
            if (_specialCooldownTurnsRemaining > 0)
            {
                _form.AppendNarrative($"Special ability cooldown: {_specialCooldownTurnsRemaining} turn(s)");
            }

            _form.AppendNarrative("\nChoose your action (1-4, or attack/special/flee/item):");
            if (_isFinalBossEncounter)
            {
                _form.AppendNarrative("The First King will not die quickly. Your blows only begin the story.");
            }
        }

        private PlayerActionOutcome ProcessPlayerAction(string choice, Hero player, Enemy enemy)
        {
            string normalizedChoice = choice.Trim().ToLowerInvariant();

            switch (normalizedChoice)
            {
                case "2":
                case "special":
                case "ability":
                    if (_specialCooldownTurnsRemaining > 0)
                    {
                        _form.AppendNarrative("Your special ability is still recovering. Choose another action.");
                        return PlayerActionOutcome.NoTurnSpent;
                    }

                    _awaitingSpecialChoice = true;
                    ShowSpecialMenu();
                    return PlayerActionOutcome.NoTurnSpent;

                case "3":
                case "flee":
                case "run":
                case "retreat":
                case "escape":
                    if (_isFinalBossEncounter)
                    {
                        _form.AppendNarrative("The throne closes the road behind you. There is no retreat from Alhuezo.");
                        return PlayerActionOutcome.NoTurnSpent;
                    }

                    _form.AppendNarrative($"{player.Name} attempts to break away from combat and flee...");
                    bool success = player.EscapeFromCombat();
                    if (success)
                    {
                        _form.AppendNarrative("Success! You escape the enemy's clutches and retreat to the safety of Oakhaven.");
                        LastActionWasFleeSuccessful = true;
                    }
                    else
                    {
                        _form.AppendNarrative("Failed! The enemy blocks your exit path, trapping you in the encounter!");
                    }
                    return success ? PlayerActionOutcome.Fled : PlayerActionOutcome.NoTurnSpent;

                case "4":
                case "item":
                case "use item":
                case "potion":
                case "consumable":
                    _pendingConsumables = player.HeroInventory.GetItems().OfType<Consumable>().ToList();
                    if (_pendingConsumables.Count == 0)
                    {
                        _form.AppendNarrative("You search your pack, but there is no consumable to use.");
                        return PlayerActionOutcome.NoTurnSpent;
                    }

                    _awaitingItemChoice = true;
                    ShowItemMenu();
                    return PlayerActionOutcome.NoTurnSpent;

                case "1":
                case "attack":
                case "basic":
                case "strike":
                default:
                    int beforeHealth = enemy.CurrentHealth;
                    player.Attack(enemy);
                    int damage = Math.Max(0, beforeHealth - enemy.CurrentHealth);
                    _form.AppendNarrative(DescribePlayerBasicAttack(player, enemy, damage));
                    return PlayerActionOutcome.ContinueTurn;
            }
        }

        private bool ProcessSpecialChoice(string choice, Hero player, Enemy enemy)
        {
            string normalizedChoice = choice.Trim().ToLowerInvariant();
            List<(string MoveName, string Type, string Description)> moves = player.GetCombatMoves();

            if (normalizedChoice is "back" or "0")
            {
                _form.AppendNarrative("Choose a class ability:");
                ShowSpecialMenu();
                UpdateBattlefieldDisplay();
                return false;
            }

            if (!int.TryParse(normalizedChoice, out int selection) || selection < 1 || selection > moves.Count)
            {
                _form.AppendNarrative($"Invalid special choice. Type 1-{moves.Count} or back.");
                ShowSpecialMenu();
                return false;
            }

            if (player is Mage mage)
            {
                switch (selection)
                {
                    case 1:
                        _form.AppendNarrative(mage.CastFirebolt(enemy));
                        _specialCooldownTurnsRemaining = 2;
                        break;

                    case 2:
                        _form.AppendNarrative(mage.CastLightningCascade(enemy));
                        _specialCooldownTurnsRemaining = 2;
                        break;

                    case 3:
                        _form.AppendNarrative(mage.CastArcaneWard());
                        break;
                }
            }
            else if (player is ARDO.Game.Players.Warrior warrior)
            {
                switch (selection)
                {
                    case 1:
                        _form.AppendNarrative(warrior.SwordStrike(enemy));
                        _specialCooldownTurnsRemaining = 2;
                        break;

                    case 2:
                        _form.AppendNarrative(warrior.ShieldBash(enemy));
                        _specialCooldownTurnsRemaining = 2;
                        break;

                    case 3:
                        _form.AppendNarrative(warrior.DefensiveGuard());
                        break;
                }
            }
            else
            {
                _form.AppendNarrative("Your class has no special combat style defined.");
            }

            return true;
        }

        private bool ProcessItemChoice(string choice, Hero player)
        {
            string normalizedChoice = choice.Trim().ToLowerInvariant();

            if (normalizedChoice is "back" or "0" or "cancel")
            {
                _form.AppendNarrative("You close the item pouch and keep fighting.");
                ShowItemMenu();
                return false;
            }

            if (!int.TryParse(normalizedChoice, out int selection) || selection < 1 || selection > _pendingConsumables.Count)
            {
                _form.AppendNarrative($"Invalid item choice. Type 1-{_pendingConsumables.Count} or back.");
                ShowItemMenu();
                return false;
            }

            Consumable consumable = _pendingConsumables[selection - 1];
            int healedAmount = Math.Max(1, consumable.HealAmount);
            int beforeHealth = player.CurrentHealth;
            player.CurrentHealth = Math.Min(player.MaxHealth, player.CurrentHealth + healedAmount);
            int actualHealed = player.CurrentHealth - beforeHealth;

            player.HeroInventory.RemoveItem(consumable);
            _playerRepo.RemoveInventoryItem(player.CharacterId, consumable.ItemId);
            _form.AppendNarrative($"{player.Name} uses {consumable.Name} and recovers {actualHealed} HP.");
            _pendingConsumables.Clear();
            return true;
        }

        private void ResolveEncounter()
        {
            if (!HasLivingEnemies() && _enemies.Count > 0)
            {
                _form.AppendNarrative("");
                _form.AppendNarrative($"Victory. You defeated {_enemies.Count} enemy(ies).");

                int totalXp = _enemies.Sum(enemy => enemy.XpReward);
                int totalGold = _enemies.Sum(enemy => enemy.GoldReward);
                bool leveledUp = _player.GainExperience(totalXp);

                _player.Gold += totalGold;
                _form.AppendNarrative($"Rewards earned: +{totalXp} XP | +{totalGold} gold.");
                if (leveledUp)
                {
                    _form.AppendNarrative($"A surge of power raises you to level {_player.Level}.");
                }

                _playerRepo.SaveGameState(_player, _player.ActiveStoryNode);
            }
            else if (!_player.IsAlive)
            {
                _form.AppendNarrative($"\nDefeat. You were struck down by {_enemies.FirstOrDefault(enemy => enemy.IsAlive)?.Name ?? "the enemy"}.");
            }

            EndCombat();
        }

        private void ResolveFinalBossTurn(Enemy activeEnemy)
        {
            _finalBossRounds++;

            if (_player.CurrentHealth <= 0)
            {
                _player.CurrentHealth = 1;
            }

            if (activeEnemy.CurrentHealth <= 0)
            {
                activeEnemy.CurrentHealth = 1;
            }

            if (_finalBossRounds <= 2)
            {
                _form.AppendNarrative("");
                _form.AppendNarrative($"[Phase {_finalBossRounds}] The First King answers your strike with cold amusement.");
                _form.AppendNarrative("Your blow lands. The throne does not move.");

                int pressureDamage = Math.Max(1, activeEnemy.BaseDamage / 3);
                _player.TakeDamage(pressureDamage);
                if (_player.CurrentHealth <= 0)
                {
                    _player.CurrentHealth = 1;
                    _form.AppendNarrative("You stumble, but the king lets you keep breathing a little longer.");
                }

                _form.AppendNarrative($"The room shudders under a smaller, patient strike. You endure with {_player.CurrentHealth} HP remaining.");
                TickSpecialCooldown();
                UpdateBattlefieldDisplay();
                return;
            }

            PlayFinalBossEnding();
        }

        private void PlayFinalBossEnding()
        {
            if (_finalBossEndingPlayed)
                return;

            _finalBossEndingPlayed = true;
            _player.CurrentHealth = 0;

            _form.AppendNarrative("");
            _form.AppendNarrative("");
            _form.AppendNarrative("==========================================================");
            _form.AppendNarrative("                         EPILOGUE");
            _form.AppendNarrative("==========================================================");
            _form.AppendNarrative("");
            _form.AppendNarrative("The dungeon goes still.");
            _form.AppendNarrative("The last clash has already passed, and what remains is only the weight of the throne room.");
            _form.AppendNarrative("");
            _form.AppendNarrative("The ringing in your ears fades into a heavy, suffocating silence.");
            _form.AppendNarrative("You are on the floor, the cold stone pressing against your cheek, your body refusing to move.");
            _form.AppendNarrative("Through a blurred gaze, you see the dark hem of their cloak slide into view.");
            _form.AppendNarrative("They aren't rushing. They aren't even breathing heavily.");
            _form.AppendNarrative("");
            _form.AppendNarrative("A soft, mocking sigh ripples through the quiet room.");
            _form.AppendNarrative("\"You spoke so loudly of your destiny. Yet here you lie, painting my floors with your blood.");
            _form.AppendNarrative("Take your farce out of my sight.\"");
            _form.AppendNarrative("");
            _form.AppendNarrative("T _____ _                 _                          ");
            _form.AppendNarrative("/__   \\ |__   __ _ _ __ | | __  _   _  ___  _   _   ");
            _form.AppendNarrative("  / /\\/ '_ \\ / _` | '_ \\| |/ / | | | |/ _ \\| | | |  ");
            _form.AppendNarrative(" / /  | | | | (_| | | | |   <  | |_| | (_) | |_| |  ");
            _form.AppendNarrative(" \\/   |_| |_|\\__,_|_| |_|_|\\_\\  \\__, |\\___/ \\__,_|  ");
            _form.AppendNarrative("                                |___/               ");
            _form.AppendNarrative("  __                    _                           ");
            _form.AppendNarrative(" / _| ___  _ __   _ __ | | __ _ _   _(_)_ __   __ _ ");
            _form.AppendNarrative("| |_ / _ \\| '__| | '_ \\| |/ _` | | | | | '_ \\ / _` |");
            _form.AppendNarrative("|  _| (_) | |    | |_) | | (_| | |_| | | | | | (_| |");
            _form.AppendNarrative("|_|  \\___/|_|    | .__/|_|\\__,_|\\__, |_|_| |_|\\__, |");
            _form.AppendNarrative("                 |_|            |___/         |___/ ");
            _form.AppendNarrative("");

            EndCombat("The epilogue is complete. Press any key to return to the start screen.");
        }

        private void EndCombat(string message = "Combat ended. Enter any input to continue your journey.")
        {
            IsCombatActive = false;
            _form.UpdateVisualMetricsDisplay();
            _form.AppendNarrative("");
            _form.AppendNarrative(message);
        }

        private void TickSpecialCooldown()
        {
            if (_specialCooldownTurnsRemaining > 0)
            {
                _specialCooldownTurnsRemaining--;
            }
        }

        private void ShowActiveEnemy()
        {
            Enemy? activeEnemy = GetActiveEnemy();
            if (activeEnemy != null)
            {
                _form.ShowEnemy(activeEnemy.Name, GetEnemySpriteKey(activeEnemy));
            }
        }

        private void ShowSpecialMenu()
        {
            _form.SetCommandContext("Special commands: choose 1 to 3, or back / cancel.");
            _form.AppendNarrative("");
            _form.AppendNarrative("Choose a class ability:");

            List<(string MoveName, string Type, string Description)> moves = _player.GetCombatMoves();
            for (int i = 0; i < moves.Count; i++)
            {
                var move = moves[i];
                _form.AppendNarrative($"{i + 1}. {move.MoveName} - {move.Description}");
            }

            _form.AppendNarrative("Choose 1, 2, 3, or back / cancel:");
        }

        private void ShowItemMenu()
        {
            _form.SetCommandContext("Item commands: choose 1 to " + _pendingConsumables.Count + ", or back / cancel.");
            _form.AppendNarrative("");
            _form.AppendNarrative("Choose a consumable item:");

            for (int i = 0; i < _pendingConsumables.Count; i++)
            {
                Consumable consumable = _pendingConsumables[i];
                _form.AppendNarrative($"{i + 1}. {consumable.Name} - heals {consumable.HealAmount} HP");
            }

            _form.AppendNarrative($"Choose 1-{_pendingConsumables.Count}, or back / cancel:");
        }

        private static string DescribePlayerBasicAttack(Hero player, Enemy enemy, int damage)
        {
            if (player is Mage)
            {
                return $"{player.Name} snaps a quick arcane burst toward {enemy.Name} for {damage} damage.";
            }

            if (player is ARDO.Game.Players.Warrior)
            {
                return $"{player.Name} cuts into {enemy.Name} with a sword strike for {damage} damage.";
            }

            return $"{player.Name} lands a basic attack on {enemy.Name} for {damage} damage.";
        }

        private static string DescribeEnemyAttack(Enemy enemy, int dealtDamage)
        {
            string attackLine = enemy.Name.ToLowerInvariant() switch
            {
                var name when name.Contains("goblin") => $"{enemy.Name} lunges with a jagged blade.",
                var name when name.Contains("skeleton") || name.Contains("skeletal") => $"{enemy.Name} rattles forward with a bone-sharp strike.",
                var name when name.Contains("fire imp") || name.Contains("fireimp") => $"{enemy.Name} hurls a burst of ember flame.",
                var name when name.Contains("wraith") => $"{enemy.Name} lashes out with a cold phantom swipe.",
                var name when name.Contains("hollow captain") => $"{enemy.Name} drives a brutal captain's cut toward you.",
                var name when name.Contains("alhuezo") => $"{enemy.Name} lifts the room with a king's crushing blow.",
                _ => $"{enemy.Name} attacks from the shadows."
            };

            if (dealtDamage <= 0)
            {
                return attackLine + " You absorb or deflect the blow.";
            }

            return $"{attackLine} It deals {dealtDamage} damage.";
        }

        private bool HasLivingEnemies()
        {
            return _enemies.Any(enemy => enemy.IsAlive);
        }

        private Enemy? GetActiveEnemy()
        {
            return _enemies.FirstOrDefault(enemy => enemy.IsAlive);
        }

        private static string GetEnemySpriteKey(Enemy enemy)
        {
            string lowerName = enemy.Name.ToLowerInvariant();

            if (lowerName.Contains("goblin"))
                return "goblin";

            if (lowerName.Contains("skeleton") || lowerName.Contains("skeletal"))
                return "skeleton";

            if (lowerName.Contains("fire imp") || lowerName.Contains("fireimp"))
                return "FireImp";

            if (lowerName.Contains("hollow captain"))
                return "HollowCaptain";

            if (lowerName.Contains("wraith"))
                return "WraithHerald";

            if (lowerName.Contains("alhuezo") || enemy is ARDO.Game.Enemies.Boss)
                return "Alhuezo";

            return "boss";
        }
    }
}
