/**
 * Name: John Deardorff
 * Date: 11/14/26
 * Project ARDO
 *
 * Class description:
 * `GameStateManager.Town.cs` is the town half of the game-flow controller: arriving in Oakhaven, its menu and
    commands, resting at the inn, perks chosen in town, and the town a finished hero
    comes back to.
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
using ARDO.Game.Progression;

namespace ARDO.Game
{
    public partial class GameStateManager
    {
        public void EnterTown()
        {
            EndRunTally(_runOutcome);
            CurrentState = GameState.Town;
            _encountersRemainingInRun = 0;
            _onSidePassage = false;
            _inAmbush = false;
            _afterRoom = false;
            _pendingEvent = null;
            _guardianFaced = false;
            _fightingGuardian = false;
            _activeEvent = null;
            _choosingFloor = false;
            _townPrompt = TownPrompt.None;
            _throneFight = WheelEnding.None;
            _keyOfferedThisRun = false;
            _hero.EndDungeonRun();

            // A finished hero only borrows a floor for a run; in town they stand at the end of the story.
            if (_hero.StoryComplete)
                _hero.ActiveStoryNode = EncounterTable.FinalQuestId;

            _ui.ShowPlayer(_hero);
            _ui.UpdateVisualMetricsDisplay();
            Say("");
            Say("==========================================================", MessageTone.Header);
            Say("                        OAKHAVEN", MessageTone.Header);
            Say("==========================================================", MessageTone.Header);
            Say("");

            if (_hero.StoryComplete)
            {
                ShowFinishedHeroTown();
                ShowTownMenuOrPerks();
                return;
            }

            QuestRepository.QuestSummary? quest = _questRepo.GetQuestSummary(_hero.ActiveStoryNode);
            var npc = _questRepo.GetNpcForQuest(_hero.ActiveStoryNode);

            // The quest's story and its NPC play on the first arrival for that quest;
            // later visits (after fleeing, or a fall) just name the quest.
            bool firstArrival = _introShownForQuest != _hero.ActiveStoryNode;
            _introShownForQuest = _hero.ActiveStoryNode;

            if (quest != null)
            {
                Say($"Quest {_hero.ActiveStoryNode}: {quest.Title}", MessageTone.Gold);
            }

            if (quest != null && firstArrival)
            {
                Say(quest.NarrativeIntro);

                // The first floor's intro already says the way down is open.
                if (quest.IsFinalNode)
                    Say("The final chamber lies beneath the town. He waits there, beyond mercy.");
                else if (_hero.ActiveStoryNode > 1)
                    Say("Another section of the dungeon has opened. Clear it, return, and press deeper.");
            }

            if (npc != null && firstArrival)
            {
                Say("");
                Say($"[{npc.Value.Role}] {npc.Value.Name}: \"{npc.Value.Dialogue}\"", MessageTone.Dim);
            }

            ShowTownMenuOrPerks();
        }

        // A hero with perks still to choose picks them before the town menu comes up.
        private void ShowTownMenuOrPerks()
        {
            if (_hero.PendingPerkPicks > 0)
            {
                _townPrompt = TownPrompt.Perks;
                BeginEvent(PerkEvents.Choose(_hero));
                return;
            }

            ShowTownMenu();
        }

        private void ShowPerks()
        {
            Say("");
            Say("--- PERKS ---", MessageTone.Header);
            if (_hero.Perks.Count == 0)
            {
                Say("No perks yet. Every level-up offers three to choose from.", MessageTone.Dim);
            }

            foreach (IGrouping<PerkId, PerkId> taken in _hero.Perks.GroupBy(perk => perk))
            {
                PerkInfo info = PerkCatalog.Get(taken.Key);
                string times = taken.Count() > 1 ? $" x{taken.Count()}" : string.Empty;
                Say($"{info.Name}{times}: {info.Description}");
            }

            if (_hero.PendingPerkPicks > 0)
            {
                Say(_hero.PendingPerkPicks == 1 ? "1 perk still to choose." : $"{_hero.PendingPerkPicks} perks still to choose.", MessageTone.Gold);
            }
        }

        // The town description only on arrival; after that, just the choices.
        private void ShowTownMenu(bool arriving = true)
        {
            _ui.SetCommandContext($"Town: pick 1 to {TownChoiceCount} from the menu, or type i / g for pack and gear, perks, or help.");
            Say("");
            if (arriving)
            {
                Say("Oakhaven settles around you. The merchant waits nearby, the inn is warm, and the dungeon still looms below.");
            }

            Say($"1 Shop  2 Rest ({RestCost}g)  3 Dungeon  4 Save  5 Menu  6 Talk{(CanTurnAgain ? "  7 Turn wheel" : string.Empty)}", MessageTone.Dim);
        }

        private void ProcessTownCommand(string command)
        {
            if (_choosingFloor)
            {
                ProcessFloorChoice(command);
                return;
            }

            bool showTownMenu = true;

            switch (command)
            {
                case "1":
                case "buy":
                case "shop":
                    OpenTownShop();
                    return;

                case "2":
                case "rest":
                    RestAtTown();
                    break;

                case "3":
                case "dungeon":
                case "return":
                    if (_hero.StoryComplete)
                    {
                        ShowFloorChoice();
                        return;
                    }

                    StartDungeonRun();
                    showTownMenu = false;
                    break;

                case "4":
                case "save":
                    _playerRepo.SaveGameState(_hero);
                    Say("Game saved.", MessageTone.Dim);
                    Say("");
                    return;

                case "6":
                case "talk":
                    _townPrompt = TownPrompt.Talk;
                    BeginEvent(TownTalk.Tavern(_hero, _timesTalked));
                    return;

                case "7":
                case "turn":
                case "wheel":
                    if (_hero.WheelEnding == WheelEnding.None)
                        break;

                    _townPrompt = TownPrompt.Wheel;
                    BeginEvent(WheelEvents.TurnAgain(_hero));
                    return;

                case "5":
                case "exit":
                case "mainmenu":
                case "menu":
                    Say("");
                    Say("You leave Oakhaven and return to the main menu.");
                    _ui.ReturnToMainMenu();
                    return;

                default:
                    Say($"Pick 1 to {TownChoiceCount} from the town menu, or i / g for your pack and gear.", MessageTone.Dim);
                    break;
            }

            if (CurrentState == GameState.Town && showTownMenu)
            {
                ShowTownMenu(arriving: false);
            }
        }

        private void RestAtTown()
        {
            if (_hero.Gold < RestCost)
            {
                Say($"You do not have enough gold to rest at the inn ({RestCost}g).", MessageTone.Dim);
                return;
            }

            _hero.Gold -= RestCost;
            _hero.RestoreForTown();
            Say("");
            Say("You rest at the inn and recover your strength.", MessageTone.Heal);
            Say($"You now have {_hero.CurrentHealth}/{_hero.MaxHealth} HP.", MessageTone.Heal);
            _playerRepo.SaveGameState(_hero);
            _ui.UpdateVisualMetricsDisplay();
        }

        // The throne door matters only until the hero has ended things one way or the other.
        private bool ThroneStillSealed => _hero.WheelEnding == WheelEnding.None;

        // After an ending the hero may turn the wheel again.
        private bool CanTurnAgain => _hero.WheelEnding != WheelEnding.None;

        private int TownChoiceCount => CanTurnAgain ? 7 : 6;

        private int RestCost => WheelTurns.RestCost(_hero.WheelTurn);

        private void ShowFinishedHeroTown()
        {
            switch (_hero.WheelEnding)
            {
                case WheelEnding.TurnedTheWheel:
                    Say("You turned the wheel. Floors 1 to 3 are still yours to walk.", MessageTone.Gold);
                    Say($"Or type 7 to turn it again, to Turn {_hero.WheelTurn + 1}: harder floors, richer pay.", MessageTone.Dim);
                    return;

                case WheelEnding.SteppedOff:
                    Say("You stepped off the wheel. Floors 1 to 3 are still yours to walk.", MessageTone.Gold);
                    Say($"Or type 7 to climb back on, to Turn {_hero.WheelTurn + 1}: harder floors, richer pay.", MessageTone.Dim);
                    return;
            }

            if (_hero.WheelTurn > 0)
            {
                Say($"Turn {_hero.WheelTurn} of the wheel. The throne is sealed again, and floors 1 to 3 wait.", MessageTone.Gold);
                foreach (WheelTwist twist in WheelTurns.InForce(_hero.WheelTurn))
                {
                    Say($"  {twist.Name}: {twist.Description}", MessageTone.Warning);
                }

                if (_hero.WheelTurn > WheelTurns.LastTwistTurn)
                    Say($"  {WheelTurns.GrindLine(_hero.WheelTurn)}", MessageTone.Warning);
            }
            else
            {
                Say("Your story is told. The throne is sealed, but floors 1 to 3 are still yours to walk.", MessageTone.Gold);
            }
            Say($"Keys of the wheel: {_hero.WheelKeyCount} of 3.", MessageTone.Dim);
            if (DeepFloors.OpenFor(_hero).Any())
                Say($"Deeper roads: {string.Join(", ", DeepFloors.OpenFor(_hero).Select(road => $"{road.Label} {road.Title}"))}.", MessageTone.Dim);

            // Maren points the way once a session.
            if (_keyRumorShown)
                return;

            _keyRumorShown = true;
            Say("");

            // On a new turn of the wheel she has noticed the dark stirring before she gets to the keys.
            if (_hero.WheelTurn > 0)
                Say("[Inn Keeper] Maren: \"The monsters seem to be getting more riled up in the depths. I wonder what's going on.\"", MessageTone.Dim);

            string rumor = _hero.HasAllWheelKeys
                ? "Three keys, and you found every one. Whatever they were made to open, it is waiting on you now."
                : "Old miners swear there are three keys lost down there, one on each floor. If you go looking, look where the dead keep things.";
            Say($"[Inn Keeper] Maren: \"{rumor}\"", MessageTone.Dim);
            if (_hero.CanPickLocks)
            {
                Say("[Inn Keeper] Maren: \"Then again, anything locked three times over usually has a fourth way in.\"", MessageTone.Dim);
            }
        }
    }
}
