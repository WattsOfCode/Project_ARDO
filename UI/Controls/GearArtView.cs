/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `GearArtView.cs` paints the gear picture. The art is drawn at the largest
    font that fits between the two label columns; the slot labels use their own
    readable font, with a dotted leader from each label to its piece of armor.
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ARDO.UI.Display;

namespace ARDO.UI.Controls
{
    public class GearArtView : Control
    {
        private const TextFormatFlags Flags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
        private const int LeftLabelChars = 14;
        private const int RightLabelChars = 22;
        private const int Gap = 10;

        private readonly Font _nameFont = UiFonts.Mono(9.5f, FontStyle.Bold);
        private readonly Font _labelFont = UiFonts.Mono(9f);
        private Font? _artFont;
        private Size _fittedFor;
        private int _fittedWidth;
        private List<GearArt.SlotLabel> _labels = new();

        public GearArtView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Color.Black;
            Dock = DockStyle.Fill;
            TabStop = false;
        }

        public void SetLabels(List<GearArt.SlotLabel> labels)
        {
            _labels = labels;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            string[] canvas = GearArt.Canvas;
            if (canvas.Length == 0 || Width <= 0 || Height <= 0)
                return;

            Graphics g = e.Graphics;
            int leftWidth = LabelColumnWidth(g, left: true);
            int rightWidth = LabelColumnWidth(g, left: false);
            int columns = canvas[0].Length;

            Font artFont = ArtFont(g, canvas, Width - leftWidth - rightWidth - Gap * 2);
            int artWidth = TextWidth(g, canvas[0], artFont);
            float artChar = artWidth / (float)columns;
            int artLine = artFont.Height;
            int artHeight = artLine * canvas.Length;

            // Center the art, and the labels with it, in the space left between the columns.
            int artX = leftWidth + Gap + Math.Max(0, (Width - leftWidth - rightWidth - Gap * 2 - artWidth) / 2);
            int artY = Math.Max(0, (Height - artHeight) / 2);

            for (int r = 0; r < canvas.Length; r++)
            {
                TextRenderer.DrawText(g, canvas[r], artFont, new Point(artX, artY + r * artLine), Color.LightGreen, Flags);
            }

            int rightX = artX + artWidth + Gap;
            int nameHeight = _nameFont.Height;
            int labelHeight = _labelFont.Height;

            using var leader = new Pen(Color.LightGray) { DashStyle = DashStyle.Dot };
            foreach (GearArt.SlotLabel label in _labels)
            {
                string row = canvas[label.Row];
                int rowMiddle = artY + label.Row * artLine + artLine / 2;
                int top = rowMiddle - nameHeight / 2;
                int x = label.Left ? artX - Gap - leftWidth : rightX;
                string name = label.Slot.ToString();

                TextRenderer.DrawText(g, name, _nameFont, new Point(x, top), Color.Gold, Flags);
                TextRenderer.DrawText(g, Fit(label.ItemText, label.Left), _labelFont, new Point(x, top + nameHeight), label.ItemColor, Flags);
                TextRenderer.DrawText(g, Fit(label.StatText, label.Left), _labelFont, new Point(x, top + nameHeight + labelHeight), Color.WhiteSmoke, Flags);

                int nameWidth = TextRenderer.MeasureText(g, name, _nameFont, Size.Empty, Flags).Width;
                if (label.Left)
                {
                    int firstMark = Math.Max(0, row.Length - row.TrimStart().Length);
                    g.DrawLine(leader, x + nameWidth + 6, rowMiddle, artX + (int)(firstMark * artChar) - 4, rowMiddle);
                }
                else
                {
                    int lastMark = row.TrimEnd().Length;
                    g.DrawLine(leader, artX + (int)(lastMark * artChar) + 4, rowMiddle, rightX - 6, rowMiddle);
                }
            }
        }

        private static string Fit(string text, bool left)
        {
            int max = left ? LeftLabelChars : RightLabelChars;
            return text.Length <= max ? text : text[..(max - 1)] + "…";
        }

        private static int TextWidth(Graphics g, string text, Font font)
        {
            return TextRenderer.MeasureText(g, text.Replace(' ', 'W'), font, Size.Empty, Flags).Width;
        }

        // As wide as the longest text in that column's labels.
        private int LabelColumnWidth(Graphics g, bool left)
        {
            int width = 0;
            foreach (GearArt.SlotLabel label in _labels)
            {
                if (label.Left != left)
                    continue;

                width = Math.Max(width, TextWidth(g, label.Slot.ToString(), _nameFont) + 24);
                width = Math.Max(width, TextWidth(g, Fit(label.ItemText, left), _labelFont));
                width = Math.Max(width, TextWidth(g, Fit(label.StatText, left), _labelFont));
            }

            return width;
        }

        // The largest art font that fits; refit only when the control or the labels change size.
        private Font ArtFont(Graphics g, string[] canvas, int width)
        {
            if (_artFont != null && _fittedFor == Size && _fittedWidth == width)
                return _artFont;

            float chosen = 4f;
            for (float size = 14f; size > 4f; size -= 0.25f)
            {
                using Font trial = UiFonts.Mono(size);
                if (TextWidth(g, canvas[0], trial) <= width && trial.Height * canvas.Length <= Height)
                {
                    chosen = size;
                    break;
                }
            }

            _artFont?.Dispose();
            _artFont = UiFonts.Mono(chosen);
            _fittedFor = Size;
            _fittedWidth = width;
            return _artFont;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _nameFont.Dispose();
                _labelFont.Dispose();
                _artFont?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
