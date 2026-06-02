using System;
using ARDO.Core.Abstracts;
using ARDO.Data.SQLite;
using ARDO.UI.Forms;

namespace ARDO.UI.Display
{
    public class TownHub
    {
        private readonly MainGameForm _form;
        private readonly Hero _activePlayer;
        private readonly PlayerRepository _playerRepo;

        public TownHub(MainGameForm form, Hero activePlayer, PlayerRepository playerRepo)
        {
            _form = form ?? throw new ArgumentNullException(nameof(form));
            _activePlayer = activePlayer ?? throw new ArgumentNullException(nameof(activePlayer));
            _playerRepo = playerRepo ?? throw new ArgumentNullException(nameof(playerRepo));
        }

        public void ShowOptions()
        {
            _form.AppendNarrative("==========================================================");
            _form.AppendNarrative("                   WELCOME TO OAKHAVEN");
            _form.AppendNarrative("==========================================================");

            _form.UpdateVisualMetricsDisplay();

            _form.AppendNarrative("\nWhere would you like to go?");
            _form.AppendNarrative("1. Rest at the inn (restore HP and mana, 10 gold)");
            _form.AppendNarrative("2. Visit the blacksmith (gain +10 attack, 40 gold)");
            _form.AppendNarrative("3. Visit the armor guard (gain +5 defense, 30 gold)");
            _form.AppendNarrative("4. Visit the apothecary (gain 2 invulnerability charges, 50 gold)");
            _form.AppendNarrative("5. Save game progress");
            _form.AppendNarrative("6. Return to the wilderness");
            _form.AppendNarrative("\nSelect an option (1-6):");
        }

        public void HandleInput(string hubChoice)
        {
            if (string.IsNullOrWhiteSpace(hubChoice)) return;

            switch (hubChoice.Trim())
            {
                case "1":
                    if (_activePlayer.Gold >= 10)
                    {
                        _activePlayer.Gold -= 10;
                        _activePlayer.CurrentHealth = _activePlayer.MaxHealth;

                        if (HasProperty(_activePlayer, "CurrentMana"))
                        {
                            dynamic dynamicMage = _activePlayer;
                            dynamicMage.CurrentMana = dynamicMage.MaxMana;
                        }

                        _form.AppendNarrative("You rest at the inn and fully recover.");
                    }
                    else
                    {
                        _form.AppendNarrative("You do not have enough gold for the inn.");
                    }
                    break;

                case "2":
                    if (_activePlayer.Gold >= 40)
                    {
                        _activePlayer.Gold -= 40;
                        _activePlayer.WeaponBonusDamage += 10;
                        _form.AppendNarrative("The blacksmith sharpens your weapon. Attack increases by 10.");
                    }
                    else _form.AppendNarrative("You do not have enough gold for the blacksmith.");
                    break;

                case "3":
                    if (_activePlayer.Gold >= 30)
                    {
                        _activePlayer.Gold -= 30;
                        _activePlayer.ShieldBonusDefense += 5;
                        _form.AppendNarrative("Your gear is reinforced. Defense increases by 5.");
                    }
                    else _form.AppendNarrative("You do not have enough gold for the armor guard.");
                    break;

                case "4":
                    if (_activePlayer.Gold >= 50)
                    {
                        _activePlayer.Gold -= 50;
                        _activePlayer.InvulnerabilityCharges += 2;
                        _form.AppendNarrative("You gain 2 invulnerability charges.");
                    }
                    else _form.AppendNarrative("You do not have enough gold for the apothecary.");
                    break;

                case "5":
                    _playerRepo.SaveGameState(_activePlayer, _activePlayer.ActiveStoryNode);
                    _form.AppendNarrative("[SYSTEM] Game state saved to SQLite.");
                    break;

                case "6":
                    _form.AppendNarrative("You travel back into the wilderness.");
                    _activePlayer.IsRestingInSafety = false;
                    break;

                default:
                    _form.AppendNarrative("Invalid choice. Please select 1 through 6.");
                    break;
            }

            _form.UpdateVisualMetricsDisplay();
            _form.AppendNarrative("\nSelect another option or continue onward:");
        }

        private static bool HasProperty(object obj, string propertyName)
        {
            return obj.GetType().GetProperty(propertyName) != null;
        }
    }
}
