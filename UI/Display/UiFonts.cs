/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `UiFonts.cs` picks the monospace font the text panels use: Cascadia Mono when it
    is installed (it comes with Windows 11), otherwise Consolas.
 */
using System;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;

namespace ARDO.UI.Display
{
    public static class UiFonts
    {
        private static readonly Lazy<string> MonoFamilyName = new(() =>
        {
            using var installed = new InstalledFontCollection();
            string[] preferred = { "Cascadia Mono", "Consolas" };

            return preferred.FirstOrDefault(name =>
                installed.Families.Any(family => family.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                ?? FontFamily.GenericMonospace.Name;
        });

        public static Font Mono(float size, FontStyle style = FontStyle.Regular)
        {
            return new Font(MonoFamilyName.Value, size, style);
        }
    }
}
