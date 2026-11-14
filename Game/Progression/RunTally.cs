/**
 * Name: John Deardorff
 * Date: 11/13/26
 * Project ARDO
 *
 * Class description:
 * `RunTally.cs` keeps count of one trip into the dungeon, from leaving town to
    coming back (or not): fights won, foes slain, XP and gold earned, levels
    gained, items found, and any guardian or key taken. When the trip ends the
    tally is read out as a short summary.
 */
using System.Collections.Generic;
using ARDO.Core.Abstracts;
using ARDO.Core.Enums;

namespace ARDO.Game.Progression
{
    public sealed class RunTally
    {
        private readonly int _startLevel;
        private readonly int _startGold;
        private readonly int _startKeys;
        private readonly int _startGuardians;

        public string Place { get; }
        public int WheelTurn { get; }
        public int FightsWon { get; private set; }
        public int FoesSlain { get; private set; }
        public int XpEarned { get; private set; }
        public int ItemsFound { get; private set; }

        public RunTally(Hero hero, string place)
        {
            Place = place;
            WheelTurn = hero.WheelTurn;
            _startLevel = hero.Level;
            _startGold = hero.Gold;
            _startKeys = hero.WheelKeys;
            _startGuardians = hero.GuardiansSlain;
        }

        public void AddVictory(int foes, int xp)
        {
            FightsWon++;
            FoesSlain += foes;
            XpEarned += xp;
        }

        public void AddItem() => ItemsFound++;

        // Nothing worth reading out: the hero walked in and straight back out.
        public bool IsEmpty(Hero hero) => FightsWon == 0 && ItemsFound == 0 && hero.Gold == _startGold && hero.WheelKeys == _startKeys;

        public IEnumerable<(string Text, MessageTone Tone)> Summary(Hero hero, string outcome)
        {
            int gold = hero.Gold - _startGold;
            int levels = hero.Level - _startLevel;

            yield return ("", MessageTone.Normal);
            yield return ("---------------------- RUN SUMMARY ----------------------", MessageTone.Header);
            yield return (WheelTurn > 0 ? $"{Place}, wheel turn {WheelTurn}: {outcome}" : $"{Place}: {outcome}", MessageTone.Header);
            yield return ($"Fights won   {FightsWon,-6}  Foes slain   {FoesSlain}", MessageTone.Normal);
            yield return ($"XP earned    {XpEarned,-6}  Gold         {(gold >= 0 ? "+" : "")}{gold}", MessageTone.Gold);
            yield return ($"Items found  {ItemsFound,-6}  Level        {(levels > 0 ? $"{_startLevel} to {hero.Level}" : $"{hero.Level}")}", MessageTone.Normal);

            if ((hero.GuardiansSlain & ~_startGuardians) != 0)
                yield return ("First kill of this floor's guardian.", MessageTone.Gold);

            if ((hero.WheelKeys & ~_startKeys) != 0)
                yield return ($"A key of the wheel found ({hero.WheelKeyCount} of 3).", MessageTone.Gold);

            yield return ("----------------------------------------------------------", MessageTone.Header);
        }
    }
}
