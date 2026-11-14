/**
 * Name: John Deardorff
 * Date: 11/14/26
 * Project ARDO
 *
 * Class description:
 * `GameStateManager.Dungeon.cs` runs a trip into the dungeon: starting it, the fights in a row, what a won fight
    pays, fleeing, the guardian at the end, the wheel endings, and the run summary read
    out when the trip is over.
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

namespace ARDO.Game
{
    public partial class GameStateManager
    {
        private void StartDungeonRun()
        {
            QuestRepository.QuestSummary? quest = _questRepo.GetQuestSummary(_hero.ActiveStoryNode);
            _encountersRemainingInRun = EncounterTable.FightsPerRun(_hero.ActiveStoryNode);
            _onSidePassage = false;
            _inAmbush = false;
            _afterRoom = false;
            _pendingEvent = null;
            _guardianFaced = false;
            _fightingGuardian = false;
            DeepFloor? deep = DeepFloors.Get(_hero.ActiveStoryNode);
            _run = new RunTally(_hero, deep?.Title ?? quest?.Title ?? $"Floor {_hero.ActiveStoryNode}");

            if (deep != null)
            {
                Say("");
                Say($"You leave town and take the long stair down to {deep.Title}.");
                foreach (string line in deep.Intro)
                    Say(line, MessageTone.Dim);

                StartNextDungeonEncounter();
                return;
            }

            Say("");
            Say(quest != null
                ? $"You leave town and descend into the dungeon: {quest.Title}."
                : "You leave town and descend into the dungeon.");
            Say("The air grows colder as the stone closes around you.", MessageTone.Dim);
            StartNextDungeonEncounter();
        }

        private void StartNextDungeonEncounter()
        {
            IReadOnlyList<Enemy> encounterGroup = EncounterTable.RollGroup(_hero.ActiveStoryNode);
            bool crowded = WheelTurns.BringsExtraFoe(_hero.WheelTurn);
            if (crowded)
            {
                encounterGroup = encounterGroup.Concat(EncounterTable.RollGroup(_hero.ActiveStoryNode).Take(1)).ToList();
            }

            Say("");
            Say(_onSidePassage
                ? "Something huge unfolds from the dark of the passage."
                : "Another shape stirs in the dark. The dungeon refuses to stay quiet.", MessageTone.Warning);

            if (crowded)
                Say("The dark is crowded this turn of the wheel. One more shape follows the rest.", MessageTone.Warning);

            EnterCombat(encounterGroup);
        }

        private void ProcessCombatCommand(string command)
        {
            if (!_combatEngine.IsCombatActive)
                return;

            _combatEngine.ExecuteTurn(command);

            if (_combatEngine.IsCombatActive)
                return;

            if (_combatEngine.LastActionWasFleeSuccessful)
            {
                HandleSuccessfulFlee();
                return;
            }

            if (!_hero.IsAlive)
            {
                if (_combatEngine.FinalBossEndingPlayed)
                {
                    CurrentState = GameState.GameOver;

                    // The story is over for this hero: the load list marks it, and the throne is sealed to them.
                    _hero.StoryComplete = true;
                    _hero.RestoreForTown();
                    _playerRepo.SaveGameState(_hero);
                    EndRunTally("Fell before the throne", always: true);
                    _ui.SetCommandContext("Type anything and press Enter to return to the start screen.");
                    Say("The epilogue is complete.", MessageTone.Dim);
                    return;
                }

                HandleDefeatStart();
                return;
            }

            _run?.AddVictory(_combatEngine.LastFoesSlain, _combatEngine.LastXpEarned);

            if (_throneFight != WheelEnding.None)
            {
                PlayWheelEnding(_throneFight);
                return;
            }

            HandlePostCombatVictory();
            ContinueDungeonRun();
        }

        // What comes after a won fight or a finished event.
        private void ContinueDungeonRun()
        {
            if (_hero.PendingPerkPicks > 0)
            {
                BeginEvent(PerkEvents.Choose(_hero));
                return;
            }

            if (_pendingEvent != null)
            {
                DungeonEvent next = _pendingEvent;
                _pendingEvent = null;
                BeginEvent(next);
                return;
            }

            if (_encountersRemainingInRun > 0)
            {
                if (_afterRoom || _onSidePassage)
                {
                    _afterRoom = false;
                    StartNextDungeonEncounter();
                    return;
                }

                BeginEvent(PassageEvents.Fork());
                return;
            }

            if (!_onSidePassage && !_guardianFaced && _hero.ActiveStoryNode < EncounterTable.FinalQuestId)
            {
                _guardianFaced = true;
                BeginEvent(GuardianEvents.Approach(_hero.ActiveStoryNode, _hero.WheelTurn));
                return;
            }

            // A finished hero may find this floor's key of the wheel once the fights are done.
            if (!_onSidePassage && !_keyOfferedThisRun && _hero.StoryComplete && ThroneStillSealed
                && _hero.ActiveStoryNode < EncounterTable.FinalQuestId && !_hero.HasWheelKey(_hero.ActiveStoryNode))
            {
                _keyOfferedThisRun = true;
                if (Random.Shared.Next(100) < KeyEventChancePercent)
                {
                    BeginEvent(WheelEvents.KeyFor(_hero.ActiveStoryNode));
                    return;
                }
            }

            if (!_onSidePassage && (_hero.ActiveStoryNode < EncounterTable.FinalQuestId || FloorTiers.IsDeep(_hero.ActiveStoryNode)))
            {
                BeginEvent(DungeonEvents.SidePassage());
                return;
            }

            CompleteDungeonRun();
        }

        private void HandlePostCombatVictory()
        {
            int floor = _hero.FloorTier;

            GameItem? drop = LootGenerator.RollEncounterDrop(floor, _hero);
            if (drop != null)
            {
                GiveItem(drop, "Loot acquired:");
            }

            for (int i = 0; i < _combatEngine.GuardiansDefeated; i++)
            {
                GiveItem(LootGenerator.RollGear(floor, RarityType.Rare), "The guardian leaves something behind:");
            }

            if (_fightingGuardian)
            {
                _fightingGuardian = false;
                RewardGuardianKill(floor);
            }

            WearGear();

            if (_onSidePassage)
            {
                _hero.Gold += _combatEngine.LastGoldEarned;
                Say($"The side passage pays double: +{_combatEngine.LastGoldEarned} more gold.", MessageTone.Gold);
                GiveItem(LootGenerator.RollGear(floor + 1, RarityType.Rare), "Deep in the passage you find:");
            }

            // An ambush is an extra fight; it doesn't count toward the run.
            if (_inAmbush)
            {
                _inAmbush = false;
            }
            else
            {
                _encountersRemainingInRun = Math.Max(0, _encountersRemainingInRun - 1);
            }

            _playerRepo.SaveGameState(_hero);
            _ui.UpdateVisualMetricsDisplay();
        }

        // The first time a hero beats a floor's guardian, it leaves a trophy worth having.
        private void RewardGuardianKill(int floor)
        {
            Say("");
            Say("The guardian falls, and the floor goes quiet in a way it has not been for a long time.", MessageTone.Gold);
            if (_hero.HasSlainGuardian(floor))
                return;

            _hero.GuardiansSlain |= 1 << (floor - 1);
            GiveItem(LootGenerator.RollGear(floor + 1, RarityType.Epic), "A trophy of the first kill:");
        }

        // Gear that wears out (the starting staff) loses a use with every won fight, and breaks at the last.
        private void WearGear()
        {
            foreach (GameItem worn in _hero.Equipment.Worn.Where(item => item.WearsOut).ToList())
            {
                worn.UsesLeft--;
                if (worn.UsesLeft > 0)
                {
                    _playerRepo.SaveItemUses(_hero.CharacterId, worn);
                    if (worn.UsesLeft <= 2)
                        Say($"Your {worn.Name} creaks. It has {worn.UsesLeft} more {(worn.UsesLeft == 1 ? "fight" : "fights")} in it.", MessageTone.Warning);
                    continue;
                }

                _hero.Equipment.Unequip(worn);
                _hero.HeroInventory.RemoveItem(worn);
                _playerRepo.RemoveInventoryItem(_hero.CharacterId, worn.ItemId);
                _playerRepo.SaveEquipment(_hero);
                Say($"Your {worn.Name} splinters and breaks. Time to find a real weapon.", MessageTone.Warning);
            }
        }

        // Adds an item to the pack (if there is room), saves it, and wears it if it's an upgrade.
        private void GiveItem(GameItem item, string intro)
        {
            Say("");
            if (!_hero.HeroInventory.AddItem(item))
            {
                GameItem? cheapest = _hero.HeroInventory.GetItems()
                    .Where(carried => !carried.IsEquipped && !_hero.Equipment.Worn.Contains(carried))
                    .OrderBy(carried => carried.Value)
                    .FirstOrDefault();

                if (cheapest == null || cheapest.Value >= item.Value)
                {
                    Say($"You find {item}, but your pack is full and everything in it is worth more. You leave it behind.", MessageTone.Dim);
                    return;
                }

                _hero.HeroInventory.RemoveItem(cheapest);
                _playerRepo.RemoveInventoryItem(_hero.CharacterId, cheapest.ItemId);
                _hero.HeroInventory.AddItem(item);
                Say($"Your pack is full, so you leave the {cheapest.Name} behind to make room.", MessageTone.Dim);
            }

            _playerRepo.SaveInventoryItem(_hero.CharacterId, item);
            _run?.AddItem();
            Say($"{intro} {item} ({item.Summary})", item.Rarity.ToTone());

            if (_hero.Equipment.IsUpgrade(item))
            {
                _hero.Equipment.Equip(item);
                _playerRepo.SaveEquipment(_hero);
                Say($"You equip the {item.Name}.", MessageTone.Heal);
            }
        }

        private void HandleSuccessfulFlee()
        {
            Say("");
            Say("You break away from the dungeon floor and retreat to Oakhaven before it can close.");

            // The floor was already cleared before the side passage, so fleeing from it still counts.
            if (_onSidePassage)
            {
                CompleteDungeonRun();
                return;
            }

            _playerRepo.SaveGameState(_hero);
            _runOutcome = "Fled";
            EnterTown();
        }

        private void CompleteDungeonRun()
        {
            QuestRepository.QuestSummary? quest = _questRepo.GetQuestSummary(_hero.ActiveStoryNode);
            if (DeepFloors.Get(_hero.ActiveStoryNode) is DeepFloor deep)
            {
                Say("");
                Say($"The road falls quiet behind you. You climb the long way back from {deep.Title} to Oakhaven.");
            }
            else if (quest != null)
            {
                Say("");
                Say("The floor falls quiet behind you, and you climb back to Oakhaven.");
                if (!_hero.StoryComplete)
                    _hero.ActiveStoryNode = Math.Min(EncounterTable.FinalQuestId, _hero.ActiveStoryNode + 1);
            }

            _playerRepo.SaveGameState(_hero);
            _runOutcome = "Floor cleared";
            EnterTown();
        }

        // Reads out the trip just ended. A trip with nothing in it is skipped, unless it ended in death or an ending.
        private IReadOnlyList<(string Text, MessageTone Tone)> EndRunTally(string outcome, bool always = false)
        {
            RunTally? run = _run;
            _run = null;
            _runOutcome = "Returned to Oakhaven";
            if (run == null || (!always && run.IsEmpty(_hero)))
                return Array.Empty<(string, MessageTone)>();

            var lines = run.Summary(_hero, outcome).ToList();
            foreach ((string text, MessageTone tone) in lines)
            {
                Say(text, tone);
            }

            return lines;
        }

        // The rematch or the shadow is won: the ending plays, it's saved, and the hero goes back to the start screen.
        private void PlayWheelEnding(WheelEnding ending)
        {
            _throneFight = WheelEnding.None;
            _hero.WheelEnding = ending;
            _hero.RestoreForTown();
            _playerRepo.SaveGameState(_hero);
            _ui.UpdateVisualMetricsDisplay();

            foreach ((string text, MessageTone tone) in WheelEndings.For(ending, _hero))
            {
                Say(text, tone);
            }

            EndRunTally(ending == WheelEnding.SteppedOff ? "Stepped off the wheel" : "Guided the wheel as the throne called to you", always: true);
            CurrentState = GameState.GameOver;
            _ui.SetCommandContext("Type anything and press Enter to return to the start screen.");
            Say("The story is complete.", MessageTone.Dim);
        }
    }
}
