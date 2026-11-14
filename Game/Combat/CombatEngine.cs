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
using ARDO.Core.Enums;
using ARDO.Data;
using ARDO.Game.Enemies;
using ARDO.Game.Progression;
using ARDO.Game.Events;
using ARDO.Game.Inventory;
using ARDO.Game.Players;
using ARDO.UI.Display;
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
        // The throne fights after the story (the rematch and the shadow) can't be fled either.
        private bool _noEscape;
        private int _finalBossRounds;
        private int _specialCooldownTurnsRemaining;
        private bool _awaitingSpecialChoice;
        private bool _awaitingItemChoice;
        private List<Consumable> _pendingConsumables = new();
        private Enemy? _shownEnemy;
        private int _targetIndex;
        private int _round;
        private bool _movesListed;
        // Quick Draw: the first special attack of the fight sets no cooldown.
        private bool _quickDrawReady;

        private enum PlayerActionOutcome
        {
            TurnSpent,
            Fled,
            NoTurnSpent
        }

        public bool IsCombatActive { get; private set; }

        public bool LastActionWasFleeSuccessful { get; private set; }

        public bool FinalBossEndingPlayed { get; private set; }

        // Results of the last won fight, for loot and bonus gold.
        public int LastGoldEarned { get; private set; }

        public int GuardiansDefeated { get; private set; }

        // The last won fight's XP and how many foes fell, for the run summary.
        public int LastXpEarned { get; private set; }

        public int LastFoesSlain { get; private set; }

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

                // The scripted first meeting with Alhuezo is a story beat, not a fight to scale.
                if (enemy is not Alhuezo { IsScripted: true })
                    WheelTurns.Strengthen(enemy, _player.WheelTurn);
            }

            _player.ClearGuard();
            IsCombatActive = true;
            _isFinalBossEncounter = _enemies.Any(enemy => enemy is Alhuezo { IsScripted: true });
            _noEscape = _isFinalBossEncounter || _enemies.Any(enemy => enemy is Alhuezo or ShadowSelf);
            _finalBossRounds = 0;
            FinalBossEndingPlayed = false;
            _specialCooldownTurnsRemaining = 0;
            _awaitingSpecialChoice = false;
            _awaitingItemChoice = false;
            _pendingConsumables.Clear();
            _shownEnemy = null;
            _targetIndex = 0;
            _round = 1;
            _movesListed = false;
            _quickDrawReady = _player.HasPerk(PerkId.QuickDraw);
            LastActionWasFleeSuccessful = false;
            LastGoldEarned = 0;
            LastXpEarned = 0;
            LastFoesSlain = 0;
            GuardiansDefeated = 0;

            Say("");
            Say("==========================================================", MessageTone.Header);
            Say($"COMBAT INITIATED: {_player.Name} vs {Plural(_enemies.Count, "foe", "foes")}", MessageTone.Header);
            Say("==========================================================", MessageTone.Header);

            Say(_enemies.Count == 1 ? "Something answers from the dark." : $"The dungeon answers with {_enemies.Count} shapes.");
            if (_isFinalBossEncounter)
            {
                Say("The deepest chamber hushes around you.");
                Say("Somewhere beyond the dark, Alhuezo, the God-King, waits with patient certainty.");
                Say("This is not just another fight. It is the end of the road you have been walking.");
            }
            else if (_enemies.Any(enemy => enemy is Alhuezo))
            {
                Say("The three keys hang cold at your belt. The throne is not empty, and this time it is not certain.");
                Say("Alhuezo rises. For the first time, he looks at you as if you might matter.");
            }
            else if (_enemies.Any(enemy => enemy is ShadowSelf))
            {
                Say("It moves when you move. It breathes when you breathe. It was always a step behind you.");
            }

            if (_player.Equipment.FirstWithEffect(ItemEffect.Warded) is GameItem ward)
            {
                _player.InvulnerabilityCharges = 1;
                Say($"Your {ward.Name} hums. The first blow against you will be halved.", MessageTone.Dim);
            }

            if (_player.Equipment.FirstWithEffect(ItemEffect.Bulwark) is GameItem shield)
            {
                _player.BulwarkReady = true;
                Say($"You set your {shield.Name}. The first heavy blow will break against it.", MessageTone.Dim);
            }

            UpdateBattlefieldDisplay();
        }

        public void ExecuteTurn(string playerChoice)
        {
            if (!IsCombatActive || !_player.IsAlive)
                return;

            Enemy? target = GetTarget();
            if (target == null)
            {
                ResolveEncounter();
                return;
            }

            string choice = (playerChoice ?? string.Empty).Trim().ToLowerInvariant();
            bool turnSpent;

            // A button or a typed word like "attack" while a menu is open drops the menu and does that instead.
            if ((_awaitingSpecialChoice || _awaitingItemChoice) && IsTopLevelWord(choice))
            {
                _awaitingSpecialChoice = false;
                _awaitingItemChoice = false;
                _pendingConsumables.Clear();
            }

            if (_awaitingSpecialChoice)
            {
                turnSpent = ProcessSpecialChoice(choice);
            }
            else if (_awaitingItemChoice)
            {
                turnSpent = ProcessItemChoice(choice);
            }
            else
            {
                PlayerActionOutcome outcome = ProcessPlayerAction(choice);
                if (outcome == PlayerActionOutcome.Fled)
                {
                    EndCombat();
                    return;
                }

                turnSpent = outcome == PlayerActionOutcome.TurnSpent;
            }

            if (turnSpent)
            {
                FinishRound();
            }
        }

        // After the player's action: the enemies answer, burns tick, guard wears off, cooldowns tick.
        private void FinishRound()
        {
            WriteAnnouncements();
            GatherSummons();

            if (_isFinalBossEncounter)
            {
                ResolveFinalBossTurn(_enemies.First(enemy => enemy is Alhuezo));
                return;
            }

            if (!HasLivingEnemies())
            {
                ResolveEncounter();
                return;
            }

            Say("");
            foreach (Enemy enemy in _enemies.Where(enemy => enemy.IsAlive))
            {
                int beforeHealth = _player.CurrentHealth;
                int beforeGold = _player.Gold;
                string line = enemy.TakeTurn(_player);

                MessageTone tone = MessageTone.Dim;
                if (_player.CurrentHealth < beforeHealth)
                    tone = MessageTone.Damage;
                else if (_player.Gold < beforeGold)
                    tone = MessageTone.Warning;

                Say(line, tone);

                if (_player.LastStandTriggered)
                {
                    _player.LastStandTriggered = false;
                    Say("That should have killed you. You refuse to fall. (Last Stand: 1 HP)", MessageTone.Gold);
                }

                if (!_player.IsAlive)
                {
                    break;
                }
            }

            GatherSummons();
            _player.ClearGuard();
            if (_player is Mage flowing && flowing.HasPerk(PerkId.ManaFlow) && flowing.IsAlive)
            {
                flowing.CurrentMana = Math.Min(flowing.MaxMana, flowing.CurrentMana + 6);
            }

            TickBurns();
            WriteAnnouncements();
            TickSpecialCooldown();
            _round++;

            if (!_player.IsAlive || !HasLivingEnemies())
            {
                ResolveEncounter();
                return;
            }

            UpdateBattlefieldDisplay();
        }

        // Enemies called in by others (a king's lackeys, the risen dead) join the fight.
        private void GatherSummons()
        {
            foreach (Enemy caller in _enemies.ToList())
            {
                foreach (Enemy called in caller.TakeSummons())
                {
                    called.CombatFloor = _player.ActiveStoryNode;
                    WheelTurns.Strengthen(called, _player.WheelTurn);
                    _enemies.Add(called);
                }
            }
        }

        private void TickBurns()
        {
            if (_player.IsAlive && _player.Burn.IsActive)
            {
                int burned = _player.TakeBurnDamage();
                Say($"The flames on you burn for {burned} damage.", MessageTone.Damage);
            }

            foreach (Enemy enemy in _enemies.Where(enemy => enemy.IsAlive && enemy.Burn.IsActive))
            {
                if (enemy.FeedsOnFire)
                {
                    int fed = enemy.Burn.Tick();
                    int before = enemy.CurrentHealth;
                    enemy.CurrentHealth = Math.Min(enemy.MaxHealth, enemy.CurrentHealth + fed);
                    Say($"The flames only feed {enemy.Name}: +{enemy.CurrentHealth - before} HP.", MessageTone.Warning);
                    continue;
                }

                int burned = enemy.Burn.Tick();
                enemy.TakeDamage(burned);
                Say(enemy.IsAlive
                    ? $"{enemy.Name} burns for {burned} damage."
                    : $"{enemy.Name} burns for {burned} damage and collapses in ash.", MessageTone.Gold);
            }

            foreach (Enemy enemy in _enemies.Where(enemy => enemy.IsAlive && enemy.Poison.IsActive))
            {
                int poisoned = enemy.Poison.Tick();
                enemy.TakeDamage(poisoned);
                Say(enemy.IsAlive
                    ? $"Poison eats at {enemy.Name} for {poisoned} damage."
                    : $"Poison eats at {enemy.Name} for {poisoned} damage, and it does not get up.", MessageTone.Gold);
            }
        }

        private void WriteAnnouncements()
        {
            foreach (Enemy enemy in _enemies)
            {
                string? message = enemy.TakeAnnouncement();
                if (message != null)
                {
                    Say(message, MessageTone.Warning);
                }
            }
        }

        // The command line under the story, kept current with the flee chance and the cooldown.
        private string CombatHint()
        {
            string special = _specialCooldownTurnsRemaining > 0 ? $"2 special (ready in {_specialCooldownTurnsRemaining})" : "2 special";
            string flee = _noEscape ? "no fleeing" : $"3 flee ({_player.FleeChancePercent}%)";
            string target = _enemies.Count(enemy => enemy.IsAlive) > 1 ? ", target N" : string.Empty;
            return $"Round {_round}: 1 attack, {special}, {flee}, 4 item{target}.";
        }

        private static bool IsTopLevelWord(string choice) =>
            choice is "attack" or "basic" or "strike" or "special" or "ability" or "flee" or "run" or "retreat"
                or "escape" or "item" or "use" or "potion" or "consumable";

        // Closes the special or item menu, if one is open. Returns false when neither was.
        public bool CancelMenu()
        {
            if (!IsCombatActive || (!_awaitingSpecialChoice && !_awaitingItemChoice))
                return false;

            ExecuteTurn("0");
            return true;
        }

        private void UpdateBattlefieldDisplay()
        {
            _form.SetCommandContext(CombatHint());
            _form.UpdateVisualMetricsDisplay();
            ShowActiveEnemy();

            Say("");
            Say($"--- Round {_round} " + new string('-', 48), MessageTone.Header);
            string hpBar = TextBar.Render(_player.CurrentHealth, _player.MaxHealth);
            Say(_player is Mage mage
                ? $"{_player.Name}  HP {hpBar} {_player.CurrentHealth}/{_player.MaxHealth}  Mana {mage.CurrentMana}/{mage.MaxMana}"
                : $"{_player.Name}  HP {hpBar} {_player.CurrentHealth}/{_player.MaxHealth}");

            if (_player.Burn.IsActive)
            {
                Say($"  You are burning: {_player.Burn.DamagePerTurn} a round, {Plural(_player.Burn.TurnsLeft, "round", "rounds")} left.", MessageTone.Damage);
            }

            Say("");
            Enemy? target = GetTarget();
            for (int i = 0; i < _enemies.Count; i++)
            {
                Enemy enemy = _enemies[i];
                string marker = ReferenceEquals(enemy, target) ? ">" : " ";
                string label = $"{marker} {i + 1}. {enemy.Name,-24}";

                if (enemy.IsDead)
                {
                    Say($"{label} (fallen)", MessageTone.Dim);
                    continue;
                }

                string status = $"{label} {TextBar.Render(enemy.CurrentHealth, enemy.MaxHealth)} {enemy.CurrentHealth}/{enemy.MaxHealth}";
                if (enemy.Burn.IsActive)
                {
                    status += "  burning";
                }

                if (enemy.Poison.IsActive)
                {
                    status += "  poisoned";
                }

                if (enemy.StatusNote is string note)
                {
                    status += "  " + note;
                }

                Say(status, ReferenceEquals(enemy, target) ? MessageTone.Command : MessageTone.Normal);
            }

            // Heavy blows are announced a turn ahead, so the player can guard or kill the attacker first.
            foreach (Enemy enemy in _enemies.Where(enemy => enemy.IsAlive && enemy.NextIntent == EnemyIntent.Heavy))
            {
                Say($"! {enemy.WindUpWarning}", MessageTone.Warning);
            }

            Say("----------------------------------------------------------", MessageTone.Header);
            if (_movesListed)
                return;

            // The full list once per fight; after that the buttons and the hint line carry it.
            _movesListed = true;
            Say("1. Basic Attack        2. Special Ability");
            Say(_noEscape
                ? "3. Flee (no escape)    4. Use Item"
                : $"3. Flee ({_player.FleeChancePercent}%)          4. Use Item");
            if (_enemies.Count(enemy => enemy.IsAlive) > 1)
            {
                Say("Type 'target 2' to switch targets, or 'attack 2' to hit enemy 2.", MessageTone.Dim);
            }

            if (_isFinalBossEncounter)
            {
                Say("The God-King will not die quickly. Your blows only begin the story.", MessageTone.Dim);
            }
        }

        private PlayerActionOutcome ProcessPlayerAction(string choice)
        {
            string[] parts = choice.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string command = parts.Length > 0 ? parts[0] : string.Empty;

            // "attack 2", "1 2" or "target 2": pick the enemy first.
            if (parts.Length > 1)
            {
                if (!TrySelectTarget(parts[1]))
                {
                    return PlayerActionOutcome.NoTurnSpent;
                }

                if (command is "target" or "t")
                {
                    UpdateBattlefieldDisplay();
                    return PlayerActionOutcome.NoTurnSpent;
                }
            }

            Enemy target = GetTarget()!;

            switch (command)
            {
                case "1":
                case "attack":
                case "basic":
                case "strike":
                    HitResult hit = _player.Strike(target, _player.BasicAttackDamage, canMiss: true);
                    Say(DescribePlayerBasicAttack(target, hit), ToneFor(hit));
                    if (!hit.Missed && _player.HasPerk(PerkId.Cutpurse))
                    {
                        int lifted = Random.Shared.Next(3, 7);
                        _player.Gold += lifted;
                        Say($"Your other hand comes away with {lifted} gold.", MessageTone.Gold);
                    }

                    return PlayerActionOutcome.TurnSpent;

                case "2":
                case "special":
                case "ability":
                    if (_specialCooldownTurnsRemaining > 0)
                    {
                        Say("Your special ability is still recovering. Choose another action.", MessageTone.Dim);
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
                    if (_noEscape)
                    {
                        Say(_enemies.Any(enemy => enemy is ShadowSelf)
                            ? "There is nowhere to run from your own shadow."
                            : "The throne closes the road behind you. There is no retreat from Alhuezo.", MessageTone.Warning);
                        return PlayerActionOutcome.NoTurnSpent;
                    }

                    Say($"{_player.Name} attempts to break away from combat and flee...");
                    if (_player.EscapeFromCombat())
                    {
                        Say("Success! You break free of the enemy's clutches.", MessageTone.Heal);
                        LastActionWasFleeSuccessful = true;
                        return PlayerActionOutcome.Fled;
                    }

                    // A failed escape costs the turn, so the enemies get to strike.
                    Say("Failed! The enemy blocks your exit path, trapping you in the encounter!", MessageTone.Damage);
                    return PlayerActionOutcome.TurnSpent;

                case "4":
                case "item":
                case "use":
                case "potion":
                case "consumable":
                    _pendingConsumables = _player.HeroInventory.GetItems()
                        .OfType<Consumable>()
                        .Where(item => ConsumableUse.CanUse(_player, item))
                        .ToList();

                    if (_pendingConsumables.Count == 0)
                    {
                        Say("You search your pack, but there is nothing you can use.", MessageTone.Dim);
                        return PlayerActionOutcome.NoTurnSpent;
                    }

                    _awaitingItemChoice = true;
                    ShowItemMenu();
                    return PlayerActionOutcome.NoTurnSpent;

                case "target":
                case "t":
                    Say("Choose a target by number, like 'target 2'.", MessageTone.Dim);
                    return PlayerActionOutcome.NoTurnSpent;

                default:
                    Say("Unknown combat command. Type 1 attack, 2 special, 3 flee, or 4 item.", MessageTone.Dim);
                    return PlayerActionOutcome.NoTurnSpent;
            }
        }

        private bool TrySelectTarget(string text)
        {
            if (int.TryParse(text, out int number) && number >= 1 && number <= _enemies.Count && _enemies[number - 1].IsAlive)
            {
                _targetIndex = number - 1;
                Say($"You turn toward {_enemies[_targetIndex].Name}.", MessageTone.Dim);
                return true;
            }

            Say($"There is no standing enemy {text}. Pick one from the list.", MessageTone.Dim);
            return false;
        }

        // Returns true when a move was used (the turn is spent). "2 3" means move 2 on enemy 3.
        private bool ProcessSpecialChoice(string choice)
        {
            if (choice is "back" or "0" or "cancel")
            {
                _awaitingSpecialChoice = false;
                Say("You hold back your special ability.", MessageTone.Dim);
                _form.SetCommandContext(CombatHint());
                return false;
            }

            string[] parts = choice.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            List<(string MoveName, string Type, string Description)> moves = _player.GetCombatMoves();
            if (parts.Length == 0 || !int.TryParse(parts[0], out int selection) || selection < 1 || selection > moves.Count)
            {
                Say($"Invalid special choice. Type 1-{moves.Count} or 0 to cancel.", MessageTone.Dim);
                ShowSpecialMenu();
                return false;
            }

            if (parts.Length > 1 && !TrySelectTarget(parts[1]))
            {
                return false;
            }

            if (_player is Mage mage && mage.CurrentMana < mage.CostOf(selection))
            {
                Say($"Not enough mana for {moves[selection - 1].MoveName}: it needs {mage.CostOf(selection)}, you have {mage.CurrentMana}.", MessageTone.Dim);
                Say("Choose another move, or 0 to cancel.", MessageTone.Dim);
                return false;
            }

            _awaitingSpecialChoice = false;
            Enemy target = GetTarget()!;
            string line;
            bool isAttack = moves[selection - 1].Type == "Attack";

            switch (_player)
            {
                case Mage caster:
                    line = selection switch
                    {
                        1 => caster.CastFirebolt(target),
                        2 => caster.CastLightningCascade(target, _enemies),
                        _ => caster.CastArcaneWard()
                    };
                    break;

                case Warrior warrior:
                    line = selection switch
                    {
                        1 => warrior.SwordStrike(target, _enemies),
                        2 => warrior.ShieldBash(target),
                        _ => warrior.DefensiveGuard()
                    };
                    break;

                case Archer archer:
                    line = selection switch
                    {
                        1 => archer.AimedShot(target),
                        2 => archer.Volley(_enemies),
                        _ => archer.PinningShot(target)
                    };
                    break;

                case Rogue rogue:
                    line = selection switch
                    {
                        1 => rogue.Backstab(target),
                        2 => rogue.PoisonBlade(target),
                        _ => rogue.SmokeStep()
                    };
                    break;

                default:
                    line = "Your class has no special combat style defined.";
                    break;
            }

            if (isAttack && _quickDrawReady)
            {
                _quickDrawReady = false;
                line += " Quick Draw: your next special is ready at once.";
            }
            else if (isAttack)
            {
                _specialCooldownTurnsRemaining = SpecialCooldownTurns;
            }

            Say(line, isAttack ? ToneFor(_player.LastHit) : MessageTone.Heal);
            return true;
        }

        // Returns true when an item was used (the turn is spent).
        private bool ProcessItemChoice(string choice)
        {
            if (choice is "back" or "0" or "cancel")
            {
                _awaitingItemChoice = false;
                _pendingConsumables.Clear();
                Say("You close the item pouch and keep fighting.", MessageTone.Dim);
                _form.SetCommandContext(CombatHint());
                return false;
            }

            if (!int.TryParse(choice, out int selection) || selection < 1 || selection > _pendingConsumables.Count)
            {
                Say($"Invalid item choice. Type 1-{_pendingConsumables.Count} or 0 to cancel.", MessageTone.Dim);
                ShowItemMenu();
                return false;
            }

            _awaitingItemChoice = false;

            Consumable consumable = _pendingConsumables[selection - 1];
            Say(ConsumableUse.Apply(_player, consumable), MessageTone.Heal);

            _player.HeroInventory.RemoveItem(consumable);
            _playerRepo.RemoveInventoryItem(_player.CharacterId, consumable.ItemId);
            _pendingConsumables.Clear();
            return true;
        }

        private void ResolveEncounter()
        {
            if (!HasLivingEnemies() && _enemies.Count > 0)
            {
                Say("");
                Say($"Victory. You defeated {Plural(_enemies.Count, "enemy", "enemies")}.", MessageTone.Gold);

                int totalXp = _player.WithXpBonus(_enemies.Sum(enemy => enemy.XpReward));
                int totalGold = _player.WithGoldBonus(_enemies.Sum(enemy => enemy.GoldReward));
                bool leveledUp = _player.GainExperience(totalXp);

                _player.Gold += totalGold;
                LastGoldEarned = totalGold;
                LastXpEarned = totalXp;
                LastFoesSlain = _enemies.Count;
                GuardiansDefeated = _enemies.Count(enemy => enemy.IsGuardian);

                Say($"Rewards earned: +{totalXp} XP | +{totalGold} gold.", MessageTone.Gold);
                if (leveledUp)
                {
                    Say($"A surge of power raises you to level {_player.Level}. A new perk waits to be chosen.", MessageTone.Gold);
                }
                else if (_player.HasPerk(PerkId.SecondWind))
                {
                    int recovered = _player.Heal(_player.MaxHealth / 10);
                    if (recovered > 0)
                        Say($"Second Wind: you recover {recovered} HP.", MessageTone.Heal);
                }

                _playerRepo.SaveGameState(_player);
            }
            else if (!_player.IsAlive)
            {
                Say("");
                Say("Defeat.", MessageTone.Damage);
            }

            EndCombat();
        }

        // The God-King's first fight is a scripted loss: two rounds of pressure, then the epilogue.
        private void ResolveFinalBossTurn(Enemy boss)
        {
            _finalBossRounds++;

            if (boss.CurrentHealth <= 0)
            {
                boss.CurrentHealth = 1;
            }

            if (_finalBossRounds <= FinalBossScriptedRounds)
            {
                Say("");
                Say($"[Phase {_finalBossRounds}] The God-King answers your strike with cold amusement.", MessageTone.Warning);
                Say("Your attack lands. The throne does not move.");

                int pressureDamage = Math.Max(1, boss.BaseDamage / 3);
                _player.TakeDamage(pressureDamage);
                if (_player.CurrentHealth <= 0)
                {
                    _player.CurrentHealth = 1;
                    Say("You stumble, but the God-King lets you keep breathing a little longer.", MessageTone.Damage);
                }

                Say($"The room shudders under a smaller, patient strike. You endure with {_player.CurrentHealth} HP remaining.", MessageTone.Damage);
                _player.ClearGuard();
                TickSpecialCooldown();
                _round++;
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

            Say("");
            Say("");
            Say("==========================================================", MessageTone.Header);
            Say("                         EPILOGUE", MessageTone.Header);
            Say("==========================================================", MessageTone.Header);
            Say("");
            Say("The dungeon goes still.");
            Say("The last clash has already passed, and what remains is only the weight of the throne room.");
            Say("");
            Say("The ringing in your ears fades into a heavy, suffocating silence.");
            Say("You are on the floor, the cold stone pressing against your cheek, your body refusing to move.");
            Say("Through a blurred gaze, you see the dark hem of Alhuezo's cloak slide into view.");
            Say("He isn't rushing. He isn't even breathing heavily.");
            Say("");
            Say("A soft, mocking sigh ripples through the quiet room.");
            Say("\"You spoke so loudly of your destiny. Yet here you lie, painting my floors with your blood.", MessageTone.Warning);
            Say("Take your farce out of my sight.\"", MessageTone.Warning);
            Say("");
            foreach ((string text, MessageTone tone) in WheelEndings.ThankYou())
            {
                Say(text, tone);
            }

            EndCombat();
        }

        private void EndCombat()
        {
            IsCombatActive = false;
            _player.ClearCombatEffects();
            _form.UpdateVisualMetricsDisplay();
        }

        private void TickSpecialCooldown()
        {
            if (_specialCooldownTurnsRemaining > 0)
            {
                _specialCooldownTurnsRemaining--;
            }
        }

        // Shows the current target's art, and switches it when the target changes.
        private void ShowActiveEnemy()
        {
            Enemy? target = GetTarget();
            if (target != null && !ReferenceEquals(target, _shownEnemy))
            {
                _shownEnemy = target;
                _form.ShowEnemy(target.Name, target.ArtFolder, target.ArtKey, target.ShadowTint);
            }
        }

        private void ShowSpecialMenu()
        {
            List<(string MoveName, string Type, string Description)> moves = _player.GetCombatMoves();
            _form.SetCommandContext($"Special: choose 1 to {moves.Count} (add a target, like '1 2'), or 0 / Esc to cancel.");
            Say("");
            Say("Choose a special move:", MessageTone.Header);

            for (int i = 0; i < moves.Count; i++)
            {
                Say($"{i + 1}. {moves[i].MoveName} - {moves[i].Description}");
            }

            Say("0. cancel", MessageTone.Dim);
        }

        private void ShowItemMenu()
        {
            _form.SetCommandContext($"Items: choose 1 to {_pendingConsumables.Count}, or 0 / Esc to cancel.");
            Say("");
            Say("Choose an item:", MessageTone.Header);

            for (int i = 0; i < _pendingConsumables.Count; i++)
            {
                Consumable consumable = _pendingConsumables[i];
                Say($"{i + 1}. {consumable.Name} - {consumable.UseText}");
            }

            Say("0. cancel", MessageTone.Dim);
        }

        private static string Plural(int count, string one, string many) => $"{count} {(count == 1 ? one : many)}";

        private string DescribePlayerBasicAttack(Enemy enemy, HitResult hit)
        {
            if (hit.Missed)
            {
                return _player is Archer
                    ? $"{_player.Name}'s arrow flies wide of {enemy.Name}."
                    : $"{_player.Name} swings at {enemy.Name} and misses.";
            }

            string line = _player switch
            {
                Mage => $"{_player.Name} snaps a quick arcane burst toward {enemy.Name} for {hit.Damage} damage.",
                Warrior => $"{_player.Name} cuts into {enemy.Name} for {hit.Damage} damage.",
                Archer => $"{_player.Name} puts an arrow in {enemy.Name} for {hit.Damage} damage.",
                Rogue => $"{_player.Name} darts in and slashes {enemy.Name} for {hit.Damage} damage.",
                _ => $"{_player.Name} lands a basic attack on {enemy.Name} for {hit.Damage} damage."
            };

            if (hit.Critical)
            {
                line = "Critical hit! " + line;
            }

            return string.IsNullOrEmpty(hit.Notes) ? line : $"{line} {hit.Notes}";
        }

        private static MessageTone ToneFor(HitResult hit)
        {
            if (hit.Missed)
                return MessageTone.Dim;

            return hit.Critical ? MessageTone.Gold : MessageTone.Normal;
        }

        private void Say(string text, MessageTone tone = MessageTone.Normal)
        {
            _form.AppendNarrative(text, tone);
        }

        private bool HasLivingEnemies()
        {
            return _enemies.Any(enemy => enemy.IsAlive);
        }

        // The chosen target, or the first one still standing if it has fallen.
        private Enemy? GetTarget()
        {
            // A foe no blow can reach gives way to one that can, if there is one.
            if (_targetIndex >= 0 && _targetIndex < _enemies.Count && _enemies[_targetIndex].IsAlive
                && _enemies[_targetIndex].Untouchable && _enemies.Any(enemy => enemy.IsAlive && !enemy.Untouchable))
            {
                _targetIndex = _enemies.FindIndex(enemy => enemy.IsAlive && !enemy.Untouchable);
            }

            if (_targetIndex >= 0 && _targetIndex < _enemies.Count && _enemies[_targetIndex].IsAlive)
            {
                return _enemies[_targetIndex];
            }

            _targetIndex = _enemies.FindIndex(enemy => enemy.IsAlive);
            return _targetIndex >= 0 ? _enemies[_targetIndex] : null;
        }
    }
}
