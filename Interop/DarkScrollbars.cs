/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `DarkScrollbars.cs` is a small Win32 interop helper that gives a control the
    Windows dark theme, so its scrollbars match the black panels instead of
    showing up bright white.
 */
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ARDO.Interop
{
    public static class DarkScrollbars
    {
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(System.IntPtr hWnd, string? subAppName, string? subIdList);

        public static void Apply(Control control)
        {
            if (control.IsHandleCreated)
            {
                SetWindowTheme(control.Handle, "DarkMode_Explorer", null);
            }
            else
            {
                control.HandleCreated += (_, __) => SetWindowTheme(control.Handle, "DarkMode_Explorer", null);
            }
        }
    }
}
