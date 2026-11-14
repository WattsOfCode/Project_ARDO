/**
 * Name: John Deardorff
 * Date: 11/14/26
 * Project ARDO
 *
 * Class description:
 * `IGameView.cs` is the view contract the game's controllers (GameStateManager
    and CombatEngine) talk to. The game window implements it, so the game flow
    only knows what it can show (story text, the hero, an enemy, a full screen),
    never how the window draws it.
 */
using ARDO.Core.Abstracts;
using ARDO.Core.Enums;
using ARDO.UI.Controls;

namespace ARDO.Game
{
    public interface IGameView
    {
        // The story panel.
        void AppendNarrative(string message, MessageTone tone = MessageTone.Normal);

        void ClearNarrative();

        // The line above the command box, and putting the cursor back in it.
        void SetCommandContext(string message);

        void FocusCommandInput();

        // The portrait panel and the stats beside it.
        void ShowPlayer(Hero hero);

        void ShowEnemy(string name, string folder, string spriteFileKey, bool shadowTint);

        void UpdateVisualMetricsDisplay();

        // A full screen laid over the story (shop, pack, gear, guide, defeat), and back to the story.
        void ShowOverlay(IOverlayPanel panel);

        void RestoreNarrativePanel();

        // Leaving the hero: to the new-hero screen, or back to the title.
        void StartNewHero();

        void ReturnToMainMenu();
    }
}
