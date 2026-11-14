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
    This file holds the battle state and the round loop; the hero's moves, the
    battlefield readout and the end of a fight are in CombatEngine.PlayerActions,
    .Display and .Resolution.
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
using ARDO.Game.Inventory;
using ARDO.Game.Players;

namespace ARDO.Game.Combat
{
    public partial class CombatEngine
    {
        private const int SpecialCooldownTurns = 2;
        private const int FinalBossScriptedRounds = 2;

        private readonly IGameView _form;
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

        public CombatEngine(IGameView form, IPlayerRepository playerRepo)
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
                enemy.CombatFloor = _player.FloorTier;
                enemy.Allies = _enemies;

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
                Say("Somewhere beyond the dark, Alhuezo waits with patient certainty.");
                Say("This is not just another fight. It is the end of the road you have been walking.");
                Say("Every story about this throne ends the same way. You will not win today, and that is not the end of your story.", MessageTone.Dim);
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
                    called.CombatFloor = _player.FloorTier;
                    called.Allies = _enemies;
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
