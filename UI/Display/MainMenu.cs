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
using ARDO.Data.SQLite;
using ARDO.UI.Forms;

namespace ARDO.UI.Display
{
    public class MainMenu
    {
        private readonly MainGameForm _form;
        private readonly PlayerRepository _playerRepo;

        private enum MenuState { MainOptions, NamingCharacter, ChoosingClass, LoadingSlot }
        private MenuState _currentState = MenuState.MainOptions;

        private string _pendingName = string.Empty;
        private DataTable? _cachedSlots;

        public event Action<Hero>? CharacterSelected;

        public MainMenu(MainGameForm form, PlayerRepository playerRepo)
        {
            _form = form ?? throw new ArgumentNullException(nameof(form));
            _playerRepo = playerRepo ?? throw new ArgumentNullException(nameof(playerRepo));
        }

        public void Show()
        {
            _currentState = MenuState.MainOptions;

            _form.ClearNarrative();
            WriteCenteredHeader("ARDO", "Advanced RPG Display and Operations");
            WriteCenteredOptions(
                "1. Create New Character Profile",
                "2. Load Existing Character Save Slot",
                "3. Exit Application");
            _form.AppendNarrative("");
            _form.AppendNarrative("Select an option by typing 1, 2, or 3:");
        }

        public void BeginNewGame()
        {
            _currentState = MenuState.NamingCharacter;
            _form.ClearNarrative();
            WriteCenteredHeader("ARDO", "Character Setup");
            _form.AppendNarrative("Enter your character's name:");
        }

        public void BeginLoadGame()
        {
            _currentState = MenuState.LoadingSlot;
            _form.ClearNarrative();
            WriteCenteredHeader("ARDO", "Load Game");
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
                case MenuState.NamingCharacter:
                    ProcessCharacterNaming(input.Trim());
                    break;
                case MenuState.ChoosingClass:
                    ProcessClassChoice(input.Trim());
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
                _currentState = MenuState.NamingCharacter;
                _form.AppendNarrative("");
                _form.AppendNarrative("=== Character Setup ===");
                _form.AppendNarrative("Enter your character's name:");
            }
            else if (choice == "2")
            {
                DisplayLoadSlots();
            }
            else if (choice == "3")
            {
                System.Windows.Forms.Application.Exit();
            }
            else
            {
                _form.AppendNarrative("Invalid option. Type 1 for New Game, 2 for Load Game, or 3 to Exit.");
            }
        }

        private void ProcessCharacterNaming(string name)
        {
            _pendingName = string.IsNullOrWhiteSpace(name) ? "Nameless Hero" : name;
            _currentState = MenuState.ChoosingClass;

            _form.AppendNarrative($"\nName confirmed: {_pendingName}");
            _form.AppendNarrative("\nChoose your class:");
            _form.AppendNarrative("1. Warrior (higher health and defense)");
            _form.AppendNarrative("2. Mage (higher mana and spell damage)");
            _form.AppendNarrative("Select a class by typing 1 or 2:");
        }

        private void ProcessClassChoice(string choice)
        {
            string classType;
            if (choice == "1") classType = "Warrior";
            else if (choice == "2") classType = "Mage";
            else
            {
                _form.AppendNarrative("Invalid class choice. Type 1 for Warrior or 2 for Mage:");
                return;
            }

            _form.AppendNarrative($"\nCreating a new {classType} named {_pendingName}...");

            int newId = _playerRepo.CreatePlayer(_pendingName, classType);
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
                _form.AppendNarrative("\nNo save profiles were found in ardo.db.");
                _form.AppendNarrative("Type 1 to create a new character or 3 to exit.");
                Show();
                return;
            }

            _currentState = MenuState.LoadingSlot;
            _form.AppendNarrative("\n=== Choose a Save Profile ===");

            for (int i = 0; i < _cachedSlots.Rows.Count; i++)
            {
                DataRow row = _cachedSlots.Rows[i];
                _form.AppendNarrative($"{i + 1}. [ID: {row["character_id"]}] {row["name"]} - level {row["level"]} {row["class_type"]}");
            }

            _form.AppendNarrative($"\nSelect a save slot (1-{_cachedSlots.Rows.Count}) or type 0 to go back:");
        }

        private void ProcessLoadSlotChoice(string choice)
        {
            if (choice == "0")
            {
                _form.AppendNarrative("\nReturning to the main menu.");
                Show();
                return;
            }

            if (int.TryParse(choice, out int selection) && _cachedSlots != null && selection > 0 && selection <= _cachedSlots.Rows.Count)
            {
                int slotCount = _cachedSlots.Rows.Count;
                int targetCharId = Convert.ToInt32(_cachedSlots.Rows[selection - 1]["character_id"]);
                Hero? loadedHero = _playerRepo.LoadFullProfile(targetCharId);

                if (loadedHero != null)
                {
                    CharacterSelected?.Invoke(loadedHero);
                }
                else
                {
                    _form.AppendNarrative($"Unable to load that profile. Type 1-{slotCount} or 0 to go back:");
                }
            }
            else
            {
                int slotCount = _cachedSlots?.Rows.Count ?? 0;
                _form.AppendNarrative($"Invalid save slot selection. Type 1-{slotCount} or 0 to go back:");
            }
        }

        private void WriteCenteredHeader(string title, string subtitle)
        {
            _form.AppendNarrative("==================================================");
            _form.AppendNarrative(CenterText(title));
            _form.AppendNarrative(CenterText(subtitle));
            _form.AppendNarrative("==================================================");
        }

        private void WriteCenteredOptions(params string[] options)
        {
            foreach (string option in options)
            {
                _form.AppendNarrative(CenterText(option));
            }
        }

        private static string CenterText(string text, int width = 50)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length >= width)
                return text;

            int padding = Math.Max(0, (width - text.Length) / 2);
            return new string(' ', padding) + text;
        }
    }
}
