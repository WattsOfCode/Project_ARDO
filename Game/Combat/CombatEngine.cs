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
using ARDO.Core.Entities;
using ARDO.Data;
using ARDO.Game.Enemies;
using ARDO.Game.Players;
using ARDO.UI.Forms;

namespace ARDO.Game.Combat
{
    public class CombatEngine
    {
        private const int SpecialCooldownTurns = 2;
        private const int FinalBossScriptedRounds = 2;

        private readonly MainGameForm _form;
        private readonly IPlayerRepository _playerRepo;
        private readonly List<Enemy> _enemies = new();
        private Hero _player = null!;
        private bool _isFinalBossEncounter;
        private int _finalBossRounds;
        private int _specialCooldownTurnsRemaining;
        private bool _awaitingSpecialChoice;
        private bool _awaitingItemChoice;
        private List<Consumable> _pendingConsumables = new();
        private Enemy? _shownEnemy;

        private enum PlayerActionOutcome
        {
            TurnSpent,
            Fled,
            NoTurnSpent
        }

        public bool IsCombatActive { get; private set; }

        public bool LastActionWasFleeSuccessful { get; private set; }

        public bool FinalBossEndingPlayed { get; private set; }

        public CombatEngine(MainGameForm form, IPlayerRepository playerRepo)
        {
            _form = form ?? throw new ArgumentNullException(nameof(form));
            _playerRepo = playerRepo ?? throw new ArgumentNullException(nameof(playerRepo));
        }

        public void StartEncounter(Hero player, IReadOnlyList<Enemy> enemies)
        {
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _enemies.Clear();
            _enemies.AddRange(enemies?.Where(enemy => enemy != null) ?? Array.Empty<Enemy>());
            if (_enemies.Count == 0)
            {
                _enemies.Add(new Goblin());
            }

            foreach (Enemy enemy in _enemies)
            {
                enemy.CombatFloor = _player.ActiveStoryNode;
            }

            _player.ClearGuard();
            IsCombatActive = true;
            _isFinalBossEncounter = _enemies.Any(enemy => enemy is Alhuezo);
            _finalBossRounds = 0;
            FinalBossEndingPlayed = false;
            _specialCooldownTurnsRemaining = 0;
            _awaitingSpecialChoice = false;
            _awaitingItemChoice = false;
            _pendingConsumables.Clear();
            _shownEnemy = null;
            LastActionWasFleeSuccessful = false;

            _form.AppendNarrative("");
            _form.AppendNarrative("==========================================================");
            _form.AppendNarrative($"COMBAT INITIATED: {_player.Name} vs {_enemies.Count} foe(s)");
            _form.AppendNarrative("==========================================================");

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
            if (!IsCombatActive || !_player.IsAlive)
                return;

            Enemy? activeEnemy = GetActiveEnemy();
            if (activeEnemy == null)
            {
                ResolveEncounter();
                return;
            }

            string choice = (playerChoice ?? string.Empty).Trim().ToLowerInvariant();
            bool turnSpent;

            if (_awaitingSpecialChoice)
            {
                turnSpent = ProcessSpecialChoice(choice, activeEnemy);
            }
            else if (_awaitingItemChoice)
            {
                turnSpent = ProcessItemChoice(choice);
            }
            else
            {
                PlayerActionOutcome outcome = ProcessPlayerAction(choice, activeEnemy);
                if (outcome == PlayerActionOutcome.Fled)
                {
                    EndCombat();
                    return;
                }

                turnSpent = outcome == PlayerActionOutcome.TurnSpent;
            }

            if (turnSpent)
            {
                FinishRound(activeEnemy);
            }
        }

        // After the player's action: the enemies answer, guard wears off, cooldowns tick.
        private void FinishRound(Enemy activeEnemy)
        {
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

            _player.ClearGuard();
            TickSpecialCooldown();

            if (!_player.IsAlive || !HasLivingEnemies())
            {
                ResolveEncounter();
                return;
            }

            UpdateBattlefieldDisplay();
        }

        private void UpdateBattlefieldDisplay()
        {
            _form.SetCommandContext("Combat commands: 1 attack, 2 special, 3 flee, 4 item.");
            _form.UpdateVisualMetricsDisplay();
            ShowActiveEnemy();

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
            _form.AppendNarrative(_isFinalBossEncounter
                ? "3. Attempt to Flee (there is no escape here)"
                : $"3. Attempt to Flee ({Hero.FleeChancePercent}% chance; a failed attempt costs your turn)");
            _form.AppendNarrative("4. Use Item");
            if (_specialCooldownTurnsRemaining > 0)
            {
                _form.AppendNarrative($"Special ability cooldown: {_specialCooldownTurnsRemaining} turn(s)");
            }

            _form.AppendNarrative("\nChoose your action: 1 attack, 2 special, 3 flee, 4 item.");
            if (_isFinalBossEncounter)
            {
                _form.AppendNarrative("The First King will not die quickly. Your blows only begin the story.");
            }
        }

        private PlayerActionOutcome ProcessPlayerAction(string choice, Enemy enemy)
        {
            switch (choice)
            {
                case "1":
                case "attack":
                case "basic":
                case "strike":
                    int beforeHealth = enemy.CurrentHealth;
                    _player.Attack(enemy);
                    int damage = Math.Max(0, beforeHealth - enemy.CurrentHealth);
                    _form.AppendNarrative(DescribePlayerBasicAttack(enemy, damage));
                    return PlayerActionOutcome.TurnSpent;

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

                    _form.AppendNarrative($"{_player.Name} attempts to break away from combat and flee...");
                    if (_player.EscapeFromCombat())
                    {
                        _form.AppendNarrative("Success! You escape the enemy's clutches and retreat to the safety of Oakhaven.");
                        LastActionWasFleeSuccessful = true;
                        return PlayerActionOutcome.Fled;
                    }

                    // A failed escape costs the turn, so the enemies get to strike.
                    _form.AppendNarrative("Failed! The enemy blocks your exit path, trapping you in the encounter!");
                    return PlayerActionOutcome.TurnSpent;

                case "4":
                case "item":
                case "use item":
                case "potion":
                case "consumable":
                    _pendingConsumables = _player.HeroInventory.GetItems().OfType<Consumable>().ToList();
                    if (_pendingConsumables.Count == 0)
                    {
                        _form.AppendNarrative("You search your pack, but there is no consumable to use.");
                        return PlayerActionOutcome.NoTurnSpent;
                    }

                    _awaitingItemChoice = true;
                    ShowItemMenu();
                    return PlayerActionOutcome.NoTurnSpent;

                default:
                    _form.AppendNarrative("Unknown combat command. Type 1 attack, 2 special, 3 flee, or 4 item.");
                    return PlayerActionOutcome.NoTurnSpent;
            }
        }

        // Returns true when a move was used (the turn is spent).
        private bool ProcessSpecialChoice(string choice, Enemy enemy)
        {
            if (choice is "back" or "0" or "cancel")
            {
                _awaitingSpecialChoice = false;
                _form.AppendNarrative("You hold back your special ability.");
                UpdateBattlefieldDisplay();
                return false;
            }

            List<(string MoveName, string Type, string Description)> moves = _player.GetCombatMoves();
            if (!int.TryParse(choice, out int selection) || selection < 1 || selection > moves.Count)
            {
                _form.AppendNarrative($"Invalid special choice. Type 1-{moves.Count} or 0 to cancel.");
                ShowSpecialMenu();
                return false;
            }

            if (_player is Mage mage && mage.CurrentMana < Mage.ManaCostFor(selection))
            {
                _form.AppendNarrative($"Not enough mana for {moves[selection - 1].MoveName}: it needs {Mage.ManaCostFor(selection)}, you have {mage.CurrentMana}.");
                _form.AppendNarrative("Choose another move, or 0 to cancel.");
                return false;
            }

            _awaitingSpecialChoice = false;

            switch (_player)
            {
                case Mage caster:
                    switch (selection)
                    {
                        case 1:
                            _form.AppendNarrative(caster.CastFirebolt(enemy));
                            _specialCooldownTurnsRemaining = SpecialCooldownTurns;
                            break;
                        case 2:
                            _form.AppendNarrative(caster.CastLightningCascade(enemy));
                            _specialCooldownTurnsRemaining = SpecialCooldownTurns;
                            break;
                        case 3:
                            _form.AppendNarrative(caster.CastArcaneWard());
                            break;
                    }
                    break;

                case Warrior warrior:
                    switch (selection)
                    {
                        case 1:
                            _form.AppendNarrative(warrior.SwordStrike(enemy));
                            _specialCooldownTurnsRemaining = SpecialCooldownTurns;
                            break;
                        case 2:
                            _form.AppendNarrative(warrior.ShieldBash(enemy));
                            _specialCooldownTurnsRemaining = SpecialCooldownTurns;
                            break;
                        case 3:
                            _form.AppendNarrative(warrior.DefensiveGuard());
                            break;
                    }
                    break;

                default:
                    _form.AppendNarrative("Your class has no special combat style defined.");
                    break;
            }

            return true;
        }

        // Returns true when an item was used (the turn is spent).
        private bool ProcessItemChoice(string choice)
        {
            if (choice is "back" or "0" or "cancel")
            {
                _awaitingItemChoice = false;
                _pendingConsumables.Clear();
                _form.AppendNarrative("You close the item pouch and keep fighting.");
                UpdateBattlefieldDisplay();
                return false;
            }

            if (!int.TryParse(choice, out int selection) || selection < 1 || selection > _pendingConsumables.Count)
            {
                _form.AppendNarrative($"Invalid item choice. Type 1-{_pendingConsumables.Count} or 0 to cancel.");
                ShowItemMenu();
                return false;
            }

            _awaitingItemChoice = false;

            Consumable consumable = _pendingConsumables[selection - 1];
            int healedAmount = Math.Max(1, consumable.HealAmount);
            int beforeHealth = _player.CurrentHealth;
            _player.CurrentHealth = Math.Min(_player.MaxHealth, _player.CurrentHealth + healedAmount);
            int actualHealed = _player.CurrentHealth - beforeHealth;

            _player.HeroInventory.RemoveItem(consumable);
            _playerRepo.RemoveInventoryItem(_player.CharacterId, consumable.ItemId);
            _form.AppendNarrative($"{_player.Name} uses {consumable.Name} and recovers {actualHealed} HP.");
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

                _playerRepo.SaveGameState(_player);
            }
            else if (!_player.IsAlive)
            {
                _form.AppendNarrative($"\nDefeat. You were struck down by {GetActiveEnemy()?.Name ?? "the enemy"}.");
            }

            EndCombat();
        }

        // The First King's fight is a scripted loss: two rounds of pressure, then the epilogue.
        private void ResolveFinalBossTurn(Enemy activeEnemy)
        {
            _finalBossRounds++;

            if (activeEnemy.CurrentHealth <= 0)
            {
                activeEnemy.CurrentHealth = 1;
            }

            if (_finalBossRounds <= FinalBossScriptedRounds)
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
                _player.ClearGuard();
                TickSpecialCooldown();
                UpdateBattlefieldDisplay();
                return;
            }

            PlayFinalBossEnding();
        }

        private void PlayFinalBossEnding()
        {
            if (FinalBossEndingPlayed)
                return;

            FinalBossEndingPlayed = true;
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

            EndCombat();
        }

        private void EndCombat()
        {
            IsCombatActive = false;
            _player.ClearGuard();
            _form.UpdateVisualMetricsDisplay();
        }

        private void TickSpecialCooldown()
        {
            if (_specialCooldownTurnsRemaining > 0)
            {
                _specialCooldownTurnsRemaining--;
            }
        }

        // Shows the current target's art, and switches it when the previous target falls.
        private void ShowActiveEnemy()
        {
            Enemy? activeEnemy = GetActiveEnemy();
            if (activeEnemy != null && !ReferenceEquals(activeEnemy, _shownEnemy))
            {
                _shownEnemy = activeEnemy;
                _form.ShowEnemy(activeEnemy.Name, activeEnemy.ArtKey);
            }
        }

        private void ShowSpecialMenu()
        {
            List<(string MoveName, string Type, string Description)> moves = _player.GetCombatMoves();
            _form.SetCommandContext($"Special commands: choose 1 to {moves.Count}, or 0 to cancel.");
            _form.AppendNarrative("");
            _form.AppendNarrative("Choose a special move:");

            for (int i = 0; i < moves.Count; i++)
            {
                _form.AppendNarrative($"{i + 1}. {moves[i].MoveName} - {moves[i].Description}");
            }

            _form.AppendNarrative("0. cancel");
        }

        private void ShowItemMenu()
        {
            _form.SetCommandContext($"Item commands: choose 1 to {_pendingConsumables.Count}, or 0 to cancel.");
            _form.AppendNarrative("");
            _form.AppendNarrative("Choose a consumable item:");

            for (int i = 0; i < _pendingConsumables.Count; i++)
            {
                Consumable consumable = _pendingConsumables[i];
                _form.AppendNarrative($"{i + 1}. {consumable.Name} - heals {consumable.HealAmount} HP");
            }

            _form.AppendNarrative("0. cancel");
        }

        private string DescribePlayerBasicAttack(Enemy enemy, int damage)
        {
            return _player switch
            {
                Mage => $"{_player.Name} snaps a quick arcane burst toward {enemy.Name} for {damage} damage.",
                Warrior => $"{_player.Name} cuts into {enemy.Name} with a sword strike for {damage} damage.",
                _ => $"{_player.Name} lands a basic attack on {enemy.Name} for {damage} damage."
            };
        }

        private static string DescribeEnemyAttack(Enemy enemy, int dealtDamage)
        {
            string attackLine = $"{enemy.Name} {enemy.AttackLine}";

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
    }
}
