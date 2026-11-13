/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `AsciiArtView.cs` paints one ASCII picture, centered, as large as fits the
    control. Small pictures are drawn as text at the largest font that fits.
    Detailed ones too big for a readable font are drawn once at a normal size
    and the picture is scaled down to fit, so they keep their true shape.
    Used for the portrait panel and the class cards on the creation screen.
 */
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using ARDO.UI.Display;

namespace ARDO.UI.Controls
{
    public class AsciiArtView : Control
    {
        private const TextFormatFlags Flags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;

        // Below this the letters stop reading as letters, so the picture is scaled as an image instead.
        private const float SmallestTextFont = 5f;
        private const float RenderFont = 8f;

        private string[] _lines = Array.Empty<string>();
        public static readonly Color DefaultArtColor = Color.LightGreen;

        // A hero's shadow: the same picture in blood red.
        public static readonly Color ShadowArtColor = Color.FromArgb(255, 95, 85);

        private Color _artColor = DefaultArtColor;
        private Font? _font;
        private Size _fittedFor;
        private bool _fitted;
        private Bitmap? _rendered;

        public AsciiArtView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Color.Black;
            Dock = DockStyle.Fill;
            TabStop = false;
        }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Color ArtColor
        {
            get => _artColor;
            set
            {
                if (_artColor == value)
                    return;
                _artColor = value;
                _rendered?.Dispose();
                _rendered = null;
                Invalidate();
            }
        }

        public void SetArt(string art)
        {
            _lines = art.Replace("\r", "").TrimEnd('\n').Split('\n');
            _font?.Dispose();
            _font = null;
            _fitted = false;
            _rendered?.Dispose();
            _rendered = null;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_lines.Length == 0 || Width <= 0 || Height <= 0)
                return;

            Graphics g = e.Graphics;
            Font? font = FitFont(g);
            if (font != null)
            {
                DrawText(g, font, Width, Height);
                return;
            }

            // Too detailed for a readable font: draw it once at a normal size, then scale the picture to fit.
            Bitmap picture = Rendered(g);
            float scale = Math.Min((float)Width / picture.Width, (float)Height / picture.Height);
            int w = Math.Max(1, (int)(picture.Width * scale));
            int h = Math.Max(1, (int)(picture.Height * scale));
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // Shrinking averages the thin letters with the black around them. Lift the brightness back up and
            // re-tint it to the art colour, so the font smoothing's coloured fringes don't wash it out to grey.
            float r = _artColor.R / 255f * ScaledBrightness, gr = _artColor.G / 255f * ScaledBrightness, b = _artColor.B / 255f * ScaledBrightness;
            using var brighten = new ImageAttributes();
            brighten.SetColorMatrix(new ColorMatrix(new[]
            {
                new[] { 0.30f * r, 0.30f * gr, 0.30f * b, 0f, 0f },
                new[] { 0.59f * r, 0.59f * gr, 0.59f * b, 0f, 0f },
                new[] { 0.11f * r, 0.11f * gr, 0.11f * b, 0f, 0f },
                new[] { 0f, 0f, 0f, 1f, 0f },
                new[] { 0f, 0f, 0f, 0f, 1f }
            }));
            g.DrawImage(picture, new Rectangle((Width - w) / 2, (Height - h) / 2, w, h),
                0, 0, picture.Width, picture.Height, GraphicsUnit.Pixel, brighten);
        }

        // Measured against the art colour's own brightness (light green is about 0.8).
        private const float ScaledBrightness = 2.4f;

        private void DrawText(Graphics g, Font font, int width, int height)
        {
            int artWidth = TextRenderer.MeasureText(g, Widest, font, Size.Empty, Flags).Width;
            int x = Math.Max(0, (width - artWidth) / 2);
            int y = Math.Max(0, (height - font.Height * _lines.Length) / 2);

            for (int r = 0; r < _lines.Length; r++)
            {
                TextRenderer.DrawText(g, _lines[r], font, new Point(x, y + r * font.Height), _artColor, Flags);
            }
        }

        private string Widest => new string('W', _lines.Max(line => line.Length));

        // The largest readable font that fits, or null when even the smallest is too big.
        // Refit only when the size or the picture changes.
        private Font? FitFont(Graphics g)
        {
            if (_fitted && _fittedFor == Size)
                return _font;

            _font?.Dispose();
            _font = null;
            _fitted = true;
            _fittedFor = Size;

            for (float size = 12f; size >= SmallestTextFont; size -= 0.25f)
            {
                using Font trial = UiFonts.Mono(size);
                if (TextRenderer.MeasureText(g, Widest, trial, Size.Empty, Flags).Width <= Width &&
                    trial.Height * _lines.Length <= Height)
                {
                    _font = UiFonts.Mono(size);
                    break;
                }
            }

            return _font;
        }

        private Bitmap Rendered(Graphics g)
        {
            if (_rendered != null)
                return _rendered;

            using Font font = UiFonts.Mono(RenderFont);
            int width = Math.Max(1, TextRenderer.MeasureText(g, Widest, font, Size.Empty, Flags).Width);
            int height = Math.Max(1, font.Height * _lines.Length);
            _rendered = new Bitmap(width, height);
            using Graphics bg = Graphics.FromImage(_rendered);
            bg.Clear(Color.Black);
            DrawText(bg, font, width, height);
            return _rendered;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _font?.Dispose();
                _rendered?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
