/**
 * Name: John Deardorff
 * Date: 11/14/26
 * Project ARDO
 *
 * Class description:
 * `CombatEngine.PlayerActions.cs` reads the hero's move in a fight: attacking, picking a target, the special and item
    menus, and how a hero's blow is told.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using ARDO.Core.Abstracts;
using ARDO.Core.Entities;
using ARDO.Core.Enums;
using ARDO.Game.Enemies;
using ARDO.Game.Events;
using ARDO.Game.Inventory;
using ARDO.Game.Players;

namespace ARDO.Game.Combat
{
    public partial class CombatEngine
    {
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

        private void TickSpecialCooldown()
        {
            if (_specialCooldownTurnsRemaining > 0)
            {
                _specialCooldownTurnsRemaining--;
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
    }
}
