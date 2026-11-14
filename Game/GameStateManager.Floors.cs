/**
 * Name: John Deardorff
 * Date: 11/14/26
 * Project ARDO
 *
 * Class description:
 * `GameStateManager.Floors.cs` lists the floors a hero can go down to (the story floors and the deeper roads) and
    reads the floor the player picks.
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
        // Finished heroes pick any floor before the throne.
        private void ShowFloorChoice()
        {
            _choosingFloor = true;
            _ui.SetCommandContext(FloorPrompt);
            Say("");
            Say("Which floor will you walk?");
            for (int floor = 1; floor < EncounterTable.FinalQuestId; floor++)
            {
                foreach (DeepFloor road in DeepFloors.Before(_hero, floor))
                    Say($"{road.Label}. {road.Title} (on the road to {road.LeadsToName})", MessageTone.Warning);

                QuestRepository.QuestSummary? quest = _questRepo.GetQuestSummary(floor);
                Say($"{floor}. {quest?.Title ?? $"Floor {floor}"}");
            }

            foreach (DeepFloor road in DeepFloors.Before(_hero, EncounterTable.FinalQuestId))
                Say($"{road.Label}. {road.Title} (on the road to {road.LeadsToName})", MessageTone.Warning);

            if (ThroneStillSealed)
            {
                Say($"{EncounterTable.FinalQuestId}. The Sealed Throne ({_hero.WheelKeyCount} of 3 keys)");
            }

            Say("0. Stay in town", MessageTone.Dim);
        }

        private static string Times(int count) => count switch
        {
            1 => "once",
            2 => "twice",
            _ => $"{count} times"
        };

        private string FloorPrompt
        {
            get
            {
                string roads = DeepFloors.OpenFor(_hero).Any() ? $", {string.Join(", ", DeepFloors.OpenFor(_hero).Select(road => road.Label))}" : string.Empty;
                return ThroneStillSealed
                    ? $"Pick a floor 1 to 3{roads}, 4 for the sealed throne, or 0 to stay in town."
                    : $"Pick a floor 1 to 3{roads}, or 0 to stay in town.";
            }
        }

        private void ProcessFloorChoice(string command)
        {
            if (command == "0")
            {
                _choosingFloor = false;
                ShowTownMenu(arriving: false);
                return;
            }

            if (command == $"{EncounterTable.FinalQuestId}" && ThroneStillSealed)
            {
                _choosingFloor = false;
                Say("");
                _run = new RunTally(_hero, "The Sealed Throne");
                Say("You take the long stair down, past every floor you have already cleared.");
                BeginEvent(WheelEvents.SealedDoor(_hero));
                return;
            }

            if (DeepFloors.ByLabel(command) is DeepFloor road && DeepFloors.OpenFor(_hero).Contains(road))
            {
                _choosingFloor = false;
                _hero.ActiveStoryNode = road.Id;
                StartDungeonRun();
                return;
            }

            if (!int.TryParse(command, out int floor) || floor < 1 || floor >= EncounterTable.FinalQuestId)
            {
                Say(FloorPrompt, MessageTone.Dim);
                return;
            }

            _choosingFloor = false;
            _hero.ActiveStoryNode = floor;
            StartDungeonRun();
        }
    }
}
