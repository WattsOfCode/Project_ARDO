/**
 * Name: John Deardorff
 * Date: 11/12/26
 * Project ARDO
 *
 * Class description:
 * `WheelEndings.cs` is a static holder for the closing text of the game: the two
    endings a finished hero can reach after the throne (turning the wheel with the
    keys, or a rogue stepping off it), and the "Thank you for playing" letters
    every ending closes with.
 */
using System.Collections.Generic;
using ARDO.Core.Abstracts;
using ARDO.Core.Enums;

namespace ARDO.Game.Events
{
    public static class WheelEndings
    {
        public static IEnumerable<(string Text, MessageTone Tone)> For(WheelEnding ending, Hero hero)
        {
            return ending == WheelEnding.SteppedOff ? SteppedOff(hero) : TurnedTheWheel(hero);
        }

        private static IEnumerable<(string, MessageTone)> TurnedTheWheel(Hero hero)
        {
            foreach (var line in Header("THE WHEEL TURNS"))
                yield return line;

            yield return ("Alhuezo goes down to one knee, and the great wheel above the throne shudders,", MessageTone.Normal);
            yield return ("caught between the way it was made to turn and the way he forced it.", MessageTone.Normal);
            yield return ("", MessageTone.Normal);
            yield return ("\"I invented this world's time,\" he says, almost gently. \"Every turn of it was mine.\"", MessageTone.Warning);
            yield return ($"\"This world's direction and movement aren't yours to claim,\" {hero.Name} answers.", MessageTone.Command);
            yield return ("", MessageTone.Normal);
            yield return ("Bone, ember, silver: the three keys fit the hub. You put your weight against the spokes.", MessageTone.Normal);
            yield return ("The wheel groans, slows, and for the first time since the Beginning, turns forward.", MessageTone.Normal);
            yield return ("", MessageTone.Normal);
            yield return ("Far above, in Oakhaven, a child is born soft and fragile, and this time that is only the start.", MessageTone.Normal);
            yield return ("It will grow. It will rise from the dust, and knit itself over the old stone, and age into the light.", MessageTone.Normal);
            yield return ("", MessageTone.Normal);
            yield return ("When you climb out of the Maw, Maren is waiting at the inn door. She doesn't ask. She already knows.", MessageTone.Heal);
            yield return ("", MessageTone.Normal);

            foreach (var line in ThankYou())
                yield return line;
        }

        private static IEnumerable<(string, MessageTone)> SteppedOff(Hero hero)
        {
            foreach (var line in Header("OFF THE WHEEL"))
                yield return line;

            yield return ($"The shadow falls without a sound. Then it is only {hero.Name}'s own shadow again,", MessageTone.Normal);
            yield return ("stretched long across the floor, a step behind, the way shadows are supposed to be.", MessageTone.Normal);
            yield return ("", MessageTone.Normal);
            yield return ("Beyond it there is no throne. Only the back of the great wheel, its turning hub,", MessageTone.Normal);
            yield return ("and a gap between the spokes just wide enough for someone who has spent a life slipping through gaps.", MessageTone.Normal);
            yield return ("", MessageTone.Normal);
            yield return ("You don't turn the wheel. You step off it.", MessageTone.Gold);
            yield return ("", MessageTone.Normal);
            yield return ("On the far side of the hall, Alhuezo never looks up. His wheel keeps running backward for everyone else.", MessageTone.Normal);
            yield return ("For you, for the first time, the years run forward.", MessageTone.Normal);
            yield return ("", MessageTone.Normal);
            yield return ("You walk out of the Maw one day older. Tomorrow you will be older still, and so will every day after.", MessageTone.Heal);
            yield return ("It is the only thing you have ever stolen that you mean to keep.", MessageTone.Heal);
            yield return ("", MessageTone.Normal);

            foreach (var line in ThankYou())
                yield return line;
        }

        private static IEnumerable<(string, MessageTone)> Header(string title)
        {
            int pad = System.Math.Max(0, (58 - title.Length) / 2);
            yield return ("", MessageTone.Normal);
            yield return ("==========================================================", MessageTone.Header);
            yield return (new string(' ', pad) + title, MessageTone.Header);
            yield return ("==========================================================", MessageTone.Header);
            yield return ("", MessageTone.Normal);
        }

        // "Thank you for playing", in the same letters every ending closes with.
        public static IEnumerable<(string Text, MessageTone Tone)> ThankYou()
        {
            string[] art =
            {
                " _____ _                 _                          ",
                "/__   \\ |__   __ _ _ __ | | __  _   _  ___  _   _   ",
                "  / /\\/ '_ \\ / _` | '_ \\| |/ / | | | |/ _ \\| | | |  ",
                " / /  | | | | (_| | | | |   <  | |_| | (_) | |_| |  ",
                " \\/   |_| |_|\\__,_|_| |_|_|\\_\\  \\__, |\\___/ \\__,_|  ",
                "                                |___/               ",
                "  __                    _                           ",
                " / _| ___  _ __   _ __ | | __ _ _   _(_)_ __   __ _ ",
                "| |_ / _ \\| '__| | '_ \\| |/ _` | | | | | '_ \\ / _` |",
                "|  _| (_) | |    | |_) | | (_| | |_| | | | | | (_| |",
                "|_|  \\___/|_|    | .__/|_|\\__,_|\\__, |_|_| |_|\\__, |",
                "                 |_|            |___/         |___/ "
            };

            foreach (string line in art)
                yield return (line, MessageTone.Gold);

            yield return ("", MessageTone.Normal);
        }
    }
}
