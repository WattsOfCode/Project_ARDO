/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `IOverlayPanel.cs` is the contract for full screens that take over the story
    area (shop, inventory, gear). While one is open, typed commands go to it
    instead of the game, and it raises CloseRequested when the player leaves.
 */
using System;

namespace ARDO.UI.Controls
{
    public interface IOverlayPanel
    {
        // Shown above the command box while the panel is open.
        string CommandHint { get; }

        event EventHandler? CloseRequested;

        void ProcessCommand(string command);
    }
}
