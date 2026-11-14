/**
 * Name: John Deardorff
 * Date: 11/14/26
 * Project ARDO
 *
 * Class description:
 * `GameStateManager.Panels.cs` opens and closes the screens laid over the story: the shop, the pack, the gear
    figure and the quick guide.
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
        private void OpenTownShop()
        {
            if (_openPanel is ShopPanel)
                return;

            ShowPanel(new ShopPanel(_hero, _playerRepo, _ui.UpdateVisualMetricsDisplay));
        }

        private void OpenInventory()
        {
            switch (CurrentState)
            {
                case GameState.Combat:
                    // Mid-fight the pack is the potion pouch, and using one costs the turn.
                    ProcessCombatCommand("item");
                    return;

                case GameState.Town:
                case GameState.DungeonEvent:
                    if (_openPanel is not InventoryPanel)
                    {
                        ShowPanel(new InventoryPanel(_hero, _playerRepo, _ui.UpdateVisualMetricsDisplay));
                    }
                    return;

                default:
                    Say("Your pack can wait.", MessageTone.Dim);
                    return;
            }
        }

        private void OpenGear()
        {
            switch (CurrentState)
            {
                case GameState.Combat:
                    Say("There's no time to change gear mid-fight. Your potions are under Inventory.", MessageTone.Dim);
                    return;

                case GameState.Town:
                case GameState.DungeonEvent:
                    if (_openPanel is not GearPanel)
                    {
                        ShowPanel(new GearPanel(_hero, _playerRepo, _ui.UpdateVisualMetricsDisplay));
                    }
                    return;

                default:
                    Say("Your gear can wait.", MessageTone.Dim);
                    return;
            }
        }

        // Opening a screen while another is open replaces it.
        private void ShowPanel(IOverlayPanel panel)
        {
            _openPanel = panel;
            panel.CloseRequested += (_, __) => ClosePanel(panel);
            _ui.ShowOverlay(panel);
        }

        private void ClosePanel(IOverlayPanel panel)
        {
            if (!ReferenceEquals(_openPanel, panel))
                return;

            _openPanel = null;
            _ui.RestoreNarrativePanel();
            _ui.UpdateVisualMetricsDisplay();
            Say("");
            Say(panel switch
            {
                ShopPanel => "You return from the town shop and step back into Oakhaven.",
                HelpPanel => "You put the guide away.",
                _ => "You close your pack and look up."
            });

            if (CurrentState == GameState.Town)
                ShowTownMenu(arriving: false);
            else
                ShowEventPrompt();

            _ui.FocusCommandInput();
        }
    }
}
