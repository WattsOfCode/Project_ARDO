/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `TextBar.cs` is a static helper that draws a meter out of block characters,
    like [██████····], for health, mana and experience.
 */
using System;

namespace ARDO.UI.Display
{
    public static class TextBar
    {
        public static string Render(int current, int max, int width = 10)
        {
            if (max <= 0)
                return "[" + new string('·', width) + "]";

            int filled = (int)Math.Round(width * Math.Clamp(current, 0, max) / (double)max);

            // Anything still alive shows at least one block.
            if (current > 0 && filled == 0)
            {
                filled = 1;
            }

            return "[" + new string('█', filled) + new string('·', width - filled) + "]";
        }
    }
}
