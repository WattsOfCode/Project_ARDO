/**
 * Name: John Deardorff
 * Date: 11/14/26
 * Project ARDO
 *
 * Class description:
 * `CombatEngine.Resolution.cs` ends a fight: a win with its XP and gold, a loss, and the scripted throne fight with
    Alhuezo and its ending.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using ARDO.Core.Abstracts;
using ARDO.Core.Enums;
using ARDO.Game.Enemies;
using ARDO.Game.Progression;
using ARDO.Game.Events;
using ARDO.Game.Players;
using ARDO.UI.Display;

namespace ARDO.Game.Combat
{
    public partial class CombatEngine
    {
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

        // Alhuezo's first fight is a scripted loss: two rounds of pressure, then the epilogue.
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
                Say($"[Phase {_finalBossRounds}] Alhuezo answers your strike with cold amusement.", MessageTone.Warning);
                Say("Your attack lands. The throne does not move.");

                int pressureDamage = Math.Max(1, boss.BaseDamage / 3);
                _player.TakeDamage(pressureDamage);
                if (_player.CurrentHealth <= 0)
                {
                    _player.CurrentHealth = 1;
                    Say("You stumble, but Alhuezo lets you keep breathing a little longer.", MessageTone.Damage);
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
    }
}
