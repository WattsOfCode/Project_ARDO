/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `RichTextSpacing.cs` is a small Win32 interop helper. WinForms' RichTextBox has no
    line-spacing property, so this sends the rich edit control a paragraph-format
    message to space the story text out.
 */
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ARDO.Interop
{
    public static class RichTextSpacing
    {
        private const int EM_SETPARAFORMAT = 0x0447;
        private const uint PFM_SPACEAFTER = 0x00000080;
        private const uint PFM_LINESPACING = 0x00000100;
        private const byte LineSpacingMultiple = 5; // dyLineSpacing / 20 = spacing in lines

        [StructLayout(LayoutKind.Sequential)]
        private struct PARAFORMAT2
        {
            public int cbSize;
            public uint dwMask;
            public short wNumbering;
            public short wReserved;
            public int dxStartIndent;
            public int dxRightIndent;
            public int dxOffset;
            public short wAlignment;
            public short cTabCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public int[] rgxTabs;
            public int dySpaceBefore;
            public int dySpaceAfter;
            public int dyLineSpacing;
            public short sStyle;
            public byte bLineSpacingRule;
            public byte bOutlineLevel;
            public short wShadingWeight;
            public short wShadingStyle;
            public short wNumberingStart;
            public short wNumberingStyle;
            public short wNumberingTab;
            public short wBorderSpace;
            public short wBorderWidth;
            public short wBorders;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref PARAFORMAT2 lParam);

        // Sets the line height (1.0 = single spacing) and a small gap after each paragraph, in points.
        // Applies to the whole box; text appended later picks up the same paragraph format.
        public static void Apply(RichTextBox box, double lineHeight, int pointsAfter)
        {
            if (!box.IsHandleCreated)
                return;

            int selectionStart = box.SelectionStart;
            int selectionLength = box.SelectionLength;

            var format = new PARAFORMAT2
            {
                cbSize = Marshal.SizeOf<PARAFORMAT2>(),
                dwMask = PFM_LINESPACING | PFM_SPACEAFTER,
                rgxTabs = new int[32],
                dyLineSpacing = (int)Math.Round(lineHeight * 20),
                bLineSpacingRule = LineSpacingMultiple,
                dySpaceAfter = pointsAfter * 20
            };

            box.SelectAll();
            SendMessage(box.Handle, EM_SETPARAFORMAT, IntPtr.Zero, ref format);
            box.Select(selectionStart, selectionLength);
        }
    }
}
