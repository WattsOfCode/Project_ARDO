/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `AsciiArtView.cs` paints one ASCII picture, centered, at the largest font
    that fits the control in both width and height. Used for the class cards on
    the character creation screen.
 */
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ARDO.UI.Display;

namespace ARDO.UI.Controls
{
    public class AsciiArtView : Control
    {
        private const TextFormatFlags Flags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;

        private string[] _lines = Array.Empty<string>();
        private readonly Color _artColor = Color.LightGreen;
        private Font? _font;
        private Size _fittedFor;

        public AsciiArtView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Color.Black;
            Dock = DockStyle.Fill;
            TabStop = false;
        }

        public void SetArt(string art)
        {
            _lines = art.Replace("\r", "").TrimEnd('\n').Split('\n');
            _font?.Dispose();
            _font = null;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_lines.Length == 0 || Width <= 0 || Height <= 0)
                return;

            Graphics g = e.Graphics;
            Font font = FitFont(g);
            string widest = new string('W', _lines.Max(line => line.Length));
            int artWidth = TextRenderer.MeasureText(g, widest, font, Size.Empty, Flags).Width;
            int x = Math.Max(0, (Width - artWidth) / 2);
            int y = Math.Max(0, (Height - font.Height * _lines.Length) / 2);

            for (int r = 0; r < _lines.Length; r++)
            {
                TextRenderer.DrawText(g, _lines[r], font, new Point(x, y + r * font.Height), _artColor, Flags);
            }
        }

        // The largest font that fits; refit only when the size or the picture changes.
        private Font FitFont(Graphics g)
        {
            if (_font != null && _fittedFor == Size)
                return _font;

            string widest = new string('W', _lines.Max(line => line.Length));
            float chosen = 3f;
            for (float size = 12f; size > 3f; size -= 0.25f)
            {
                using Font trial = UiFonts.Mono(size);
                if (TextRenderer.MeasureText(g, widest, trial, Size.Empty, Flags).Width <= Width &&
                    trial.Height * _lines.Length <= Height)
                {
                    chosen = size;
                    break;
                }
            }

            _font?.Dispose();
            _font = UiFonts.Mono(chosen);
            _fittedFor = Size;
            return _font;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _font?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
