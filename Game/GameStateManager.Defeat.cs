/**
 * Name: John Deardorff
 * Date: 11/14/26
 * Project ARDO
 *
 * Class description:
 * `GameStateManager.Defeat.cs` handles a hero's fall: the defeat screen with its two cards, rising again for gold,
    letting the hero go, and hard mode's erase on death.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using ARDO.Core.Abstracts;
using ARDO.Core.Entities;
using ARDO.Core.Enums;
using ARDO.Data.SQLite;
using ARDO.Game.Combat;
using ARDO.Game.Events;
using ARDO.Game.Inventory;
using ARDO.Game.Progression;
using ARDO.UI.Controls;

namespace ARDO.Game
{
    public partial class GameStateManager
    {
        private const int RiseCost = 50;

        // The fallen hero chooses on a screen of two cards: rise again, or let go and start a new hero.
        private void HandleDefeatStart()
        {
            CurrentState = GameState.DefeatPrompt;
            Say("");
            Say("You fall to the dungeon floor, breath gone and vision dimming.", MessageTone.Damage);
            var summary = EndRunTally("Fell in the dark", always: true);
            Say("");
            if (_hero.HardMode)
            {
                // Hard mode: the save goes now, so quitting can't bring the hero back.
                _playerRepo.DeleteProfile(_hero.CharacterId);
                Say($"Hard mode: {_hero.Name} will not rise again. 2 Start a new hero", MessageTone.Warning);
            }
            else
            {
                Say($"1 Rise again in Oakhaven ({RiseCost} gold)   2 Start a new hero");
                Say("Starting a new hero erases this hero's save for good.", MessageTone.Warning);
            }

            var panel = new DefeatPanel(_hero, RiseCost, summary.Select(line => line.Text));
            panel.Decided += ResolveDefeat;
            panel.HintChanged += _ui.SetCommandContext;
            _openPanel = panel;
            _ui.ShowOverlay(panel);
        }

        // Typed straight to the game (the screen asks twice before erasing; this is its answer).
        private void ProcessDefeatPrompt(string command)
        {
            switch (command)
            {
                case "yes":
                case "y":
                case "1":
                    ResolveDefeat(true);
                    break;

                case "2":
                    ResolveDefeat(false);
                    break;

                default:
                    Say("Type 1 to rise again, or 2 to start a new hero.", MessageTone.Dim);
                    break;
            }
        }

        private void ResolveDefeat(bool rise)
        {
            if (CurrentState != GameState.DefeatPrompt)
                return;

            if (rise && _hero.HardMode)
            {
                Say("In hard mode the dead stay dead. Type 2 to start a new hero.", MessageTone.Dim);
                return;
            }

            if (_openPanel is DefeatPanel)
            {
                _openPanel = null;
                _ui.RestoreNarrativePanel();
            }

            switch (rise)
            {
                case true:
                    _hero.Gold = Math.Max(0, _hero.Gold - RiseCost);
                    _hero.RestoreForTown();
                    Say("");
                    Say("Rise, hero. There is still much more for you to do.", MessageTone.Gold);
                    Say("");

                    // Dying in the side passage still counts the floor as cleared.
                    if (_onSidePassage)
                    {
                        CompleteDungeonRun();
                        break;
                    }

                    _playerRepo.SaveGameState(_hero);
                    EnterTown();
                    break;

                case false:
                    _playerRepo.DeleteProfile(_hero.CharacterId);
                    Say("");
                    Say("Your character is erased from the records of Oakhaven.", MessageTone.Damage);
                    _ui.StartNewHero();
                    break;
            }
        }

        private void ProcessGameOverCommand()
        {
            _ui.ReturnToMainMenu();
        }
    }
}
