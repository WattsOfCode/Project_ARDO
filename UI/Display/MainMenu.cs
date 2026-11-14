/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `MainMenu.cs` is a concrete menu controller that manages new-game, load-game,
    and exit flow from the start screen. It uses stateful methods and repository
    calls to create or load profiles, while CRUD stays in the data layer.
 */
using System;
using System.Data;
using ARDO.Core.Abstracts;
using ARDO.Data;
using ARDO.UI.Controls;
using ARDO.UI.Forms;

namespace ARDO.UI.Display
{
    public class MainMenu
    {
        private readonly MainGameForm _form;
        private readonly IPlayerRepository _playerRepo;

        private enum MenuState { MainOptions, CreatingCharacter, LoadingSlot }
        private MenuState _currentState = MenuState.MainOptions;

        private DataTable? _cachedSlots;

        public event Action<Hero>? CharacterSelected;

        public MainMenu(MainGameForm form, IPlayerRepository playerRepo)
        {
            _form = form ?? throw new ArgumentNullException(nameof(form));
            _playerRepo = playerRepo ?? throw new ArgumentNullException(nameof(playerRepo));
        }

        public void Show()
        {
            _currentState = MenuState.MainOptions;

            _form.ClearNarrative();
            WriteHeader("ARDO", "Advanced RPG Display and Operations");
            _form.AppendNarrative("");
            _form.AppendNarrative("1. New game    2. Load game    3. Exit");
            _form.SetCommandContext("Type 1 new game, 2 load game or 3 exit.");
        }

        // The new-hero screen takes over the story panel until a hero is made.
        public void BeginNewGame()
        {
            _currentState = MenuState.CreatingCharacter;
            _form.ClearNarrative();

            var panel = new CharacterCreationPanel(_form.SetCommandContext);
            panel.HeroChosen += (name, classType) => CreateHero(name, classType, panel.HardMode);
            panel.CloseRequested += (_, __) => _form.Close();
            _form.ShowOverlay(panel);
        }

        public void BeginLoadGame()
        {
            _currentState = MenuState.LoadingSlot;
            _form.ClearNarrative();
            WriteHeader("ARDO", "Load Game");
            DisplayLoadSlots();
        }

        public void HandleInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return;

            switch (_currentState)
            {
                case MenuState.MainOptions:
                    ProcessMainOptions(input.Trim());
                    break;
                case MenuState.LoadingSlot:
                    ProcessLoadSlotChoice(input.Trim());
                    break;
            }
        }

        private void ProcessMainOptions(string choice)
        {
            if (choice == "1")
            {
                BeginNewGame();
            }
            else if (choice == "2")
            {
                BeginLoadGame();
            }
            else if (choice == "3")
            {
                System.Windows.Forms.Application.Exit();
            }
            else
            {
                _form.AppendNarrative("Type 1 for a new game, 2 to load, or 3 to exit.");
            }
        }

        private void CreateHero(string name, string classType, bool hardMode)
        {
            int newId = _playerRepo.CreatePlayer(name, classType, hardMode);
            Hero? loadedHero = _playerRepo.LoadFullProfile(newId);

            if (loadedHero != null)
            {
                CharacterSelected?.Invoke(loadedHero);
            }
        }

        private void DisplayLoadSlots()
        {
            _cachedSlots = _playerRepo.GetLoadSlots();

            if (_cachedSlots == null || _cachedSlots.Rows.Count == 0)
            {
                Show();
                _form.AppendNarrative("");
                _form.AppendNarrative("There are no saved heroes yet. Type 1 to make one.");
                return;
            }

            _currentState = MenuState.LoadingSlot;
            _form.AppendNarrative("");

            for (int i = 0; i < _cachedSlots.Rows.Count; i++)
            {
                DataRow row = _cachedSlots.Rows[i];
                string fate = Convert.ToInt32(row["wheel_ending"]) switch
                {
                    1 => "  (turned the wheel)",
                    2 => "  (stepped off the wheel)",
                    _ => IsFinished(row) ? "  (fell to Alhuezo)" : string.Empty
                };
                int turn = Convert.ToInt32(row["wheel_turn"]);
                string turnText = turn > 0 ? $"  Turn {turn}" : string.Empty;
                string hardText = Convert.ToInt32(row["hard_mode"]) == 1 ? "  [Hard]" : string.Empty;
                _form.AppendNarrative($"{i + 1}. {row["name"],-18} Level {row["level"],-3} {row["class_type"]}{hardText}{turnText}{fate}");
            }

            _form.SetCommandContext($"Load: pick a hero 1 to {_cachedSlots.Rows.Count}, or 0 to go back.");
        }

        private void ProcessLoadSlotChoice(string choice)
        {
            int slotCount = _cachedSlots?.Rows.Count ?? 0;

            if (choice == "0")
            {
                Show();
                return;
            }

            if (int.TryParse(choice, out int selection) && _cachedSlots != null && selection > 0 && selection <= slotCount)
            {
                int targetCharId = Convert.ToInt32(_cachedSlots.Rows[selection - 1]["character_id"]);
                Hero? loadedHero = _playerRepo.LoadFullProfile(targetCharId);

                if (loadedHero != null)
                {
                    CharacterSelected?.Invoke(loadedHero);
                }
                else
                {
                    _form.AppendNarrative($"That save couldn't be loaded. Pick 1 to {slotCount}, or 0 to go back.");
                }
            }
            else
            {
                _form.AppendNarrative($"Pick a hero from 1 to {slotCount}, or 0 to go back.");
            }
        }

        private static bool IsFinished(DataRow row) => Convert.ToInt32(row["story_complete"]) == 1;

        // The story panel centers anything between two "===" rules.
        private void WriteHeader(string title, string subtitle)
        {
            _form.AppendNarrative("==================================================");
            _form.AppendNarrative(title);
            _form.AppendNarrative(subtitle);
            _form.AppendNarrative("==================================================");
        }
    }
}
