/**
 * Name: John Deardorff
 * Date: 11/14/26
 * Project ARDO
 *
 * Class description:
 * `GameStateManager.Events.cs` shows the dungeon and town events (rooms, shrines, the tavern, the wheel prompt) and
    carries out the choice the player makes in one.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using ARDO.Core.Abstracts;
using ARDO.Core.Enums;
using ARDO.Data.SQLite;
using ARDO.Game.Combat;
using ARDO.Game.Events;
using ARDO.Game.Progression;
using ARDO.UI.Controls;

namespace ARDO.Game
{
    public partial class GameStateManager
    {
        private void BeginEvent(DungeonEvent dungeonEvent)
        {
            CurrentState = GameState.DungeonEvent;
            _activeEvent = dungeonEvent;

            Say("");
            Say($"--- {dungeonEvent.Title} ---", MessageTone.Header);
            foreach (string line in dungeonEvent.Description)
            {
                Say(line);
            }

            Say("");
            for (int i = 0; i < dungeonEvent.Choices.Count; i++)
            {
                Say($"{i + 1}. {dungeonEvent.Choices[i].Label}");
            }

            ShowEventPrompt();
        }

        private void ShowEventPrompt()
        {
            if (_activeEvent != null)
            {
                _ui.SetCommandContext(ChoicePrompt(_activeEvent.Choices.Count));
            }
        }

        private static string ChoicePrompt(int count) => count == 2 ? "Choose 1 or 2." : $"Choose 1 to {count}.";

        private void ProcessEventChoice(string command)
        {
            DungeonEvent? dungeonEvent = _activeEvent;
            if (dungeonEvent == null)
            {
                ContinueDungeonRun();
                return;
            }

            if (!int.TryParse(command, out int choice) || choice < 1 || choice > dungeonEvent.Choices.Count)
            {
                Say(ChoicePrompt(dungeonEvent.Choices.Count), MessageTone.Dim);
                return;
            }

            _activeEvent = null;
            EventOutcome outcome = dungeonEvent.Choices[choice - 1].Resolve(_hero);

            foreach ((string text, MessageTone tone) in outcome.Lines)
            {
                Say(text, tone);
            }

            if (outcome.ItemUsedUp != null)
            {
                _hero.HeroInventory.RemoveItem(outcome.ItemUsedUp);
                _playerRepo.RemoveInventoryItem(_hero.CharacterId, outcome.ItemUsedUp.ItemId);
            }

            if (outcome.Reward != null)
            {
                GiveItem(outcome.Reward, "Inside you find:");
            }

            _playerRepo.SaveGameState(_hero);
            _ui.UpdateVisualMetricsDisplay();

            if (_townPrompt == TownPrompt.Talk)
            {
                // Another word, or back to the town.
                if (choice < dungeonEvent.Choices.Count)
                {
                    BeginEvent(TownTalk.Tavern(_hero, _timesTalked));
                    return;
                }

                _townPrompt = TownPrompt.None;
                CurrentState = GameState.Town;
                ShowTownMenu(arriving: false);
                return;
            }

            if (_townPrompt == TownPrompt.Wheel)
            {
                _townPrompt = TownPrompt.None;
                if (_hero.WheelEnding == WheelEnding.None)
                {
                    // Turned: arrive fresh, so Maren tells of the keys again.
                    _keyRumorShown = false;
                    EnterTown();
                    return;
                }

                CurrentState = GameState.Town;
                ShowTownMenu(arriving: false);
                return;
            }

            if (_townPrompt == TownPrompt.Perks)
            {
                if (_hero.PendingPerkPicks > 0)
                {
                    BeginEvent(PerkEvents.Choose(_hero));
                    return;
                }

                _townPrompt = TownPrompt.None;
                CurrentState = GameState.Town;
                ShowTownMenu();
                return;
            }

            if (outcome.ThroneFight != WheelEnding.None)
            {
                _throneFight = outcome.ThroneFight;
                Enemy foe = _throneFight == WheelEnding.SteppedOff
                    ? Game.Enemies.ShadowSelf.Of(_hero)
                    : Game.Enemies.Alhuezo.Unbound(_hero);
                EnterCombat(new[] { foe });
            }
            else if (outcome.GoHome)
            {
                _runOutcome = "Turned back";
                EnterTown();
            }
            else if (outcome.Guardian != null)
            {
                _fightingGuardian = true;
                EnterCombat(new[] { outcome.Guardian });
            }
            else if (outcome.ReturnToTown)
            {
                CompleteDungeonRun();
            }
            else if (outcome.PressOn)
            {
                _onSidePassage = true;
                _encountersRemainingInRun = 1;
                StartNextDungeonEncounter();
            }
            else if (outcome.StartsAmbush)
            {
                _inAmbush = true;
                _pendingEvent = outcome.FollowUp;
                EnterCombat(EncounterTable.RollGroup(_hero.ActiveStoryNode));
            }
            else if (outcome.FightNow)
            {
                StartNextDungeonEncounter();
            }
            else if (outcome.FollowUp != null)
            {
                _afterRoom |= outcome.RoomInsteadOfFight;
                BeginEvent(outcome.FollowUp);
            }
            else
            {
                ContinueDungeonRun();
            }
        }
    }
}
