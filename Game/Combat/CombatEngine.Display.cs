/**
 * Name: John Deardorff
 * Date: 11/14/26
 * Project ARDO
 *
 * Class description:
 * `CombatEngine.Display.cs` draws the fight for the player: the health bars of both sides, the enemy on show,
    the round's announcements and the command hint.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using ARDO.Core.Abstracts;
using ARDO.Core.Enums;
using ARDO.Game.Enemies;
using ARDO.Game.Progression;
using ARDO.Game.Events;
using ARDO.Game.Inventory;
using ARDO.Game.Players;
using ARDO.UI.Display;

namespace ARDO.Game.Combat
{
    public partial class CombatEngine
    {
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
                Say("Alhuezo will not die quickly. Your blows only begin the story.", MessageTone.Dim);
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

        private static string Plural(int count, string one, string many) => $"{count} {(count == 1 ? one : many)}";
    }
}
