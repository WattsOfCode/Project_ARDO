/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `ItemListBox.cs` is a black list box that draws each line in its own color,
    so item lists can show rarity the same way the story panel does.
 */
using System.Drawing;
using System.Windows.Forms;
using ARDO.UI.Display;

namespace ARDO.UI.Controls
{
    public class ItemListBox : ListBox
    {
        private sealed record Line(string Text, Color Color)
        {
            public override string ToString() => Text;
        }

        public ItemListBox(float fontSize = 11f)
        {
            Dock = DockStyle.Fill;
            BackColor = Color.Black;
            ForeColor = Color.LightGreen;
            Font = UiFonts.Mono(fontSize);
            BorderStyle = BorderStyle.FixedSingle;
            DrawMode = DrawMode.OwnerDrawFixed;
            ItemHeight = Font.Height + 6;
            IntegralHeight = false;
            TabStop = false;
        }

        public void AddLine(string text, Color color)
        {
            Items.Add(new Line(text, color));
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= Items.Count)
                return;

            // Selection isn't used (commands are typed), so every row keeps the black background.
            using (var background = new SolidBrush(BackColor))
            {
                e.Graphics.FillRectangle(background, e.Bounds);
            }

            Color color = Items[e.Index] is Line line ? line.Color : ForeColor;
            TextRenderer.DrawText(e.Graphics, Items[e.Index].ToString(), Font, e.Bounds, color,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }
}
