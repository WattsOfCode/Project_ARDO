/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `ItemListBox.cs` is a black list box that draws each line in its own color,
    so item lists can show rarity the same way the story panel does.
 */
using System;
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

        // The row the player clicked, so a panel can put its command in the box.
        public event Action<int>? RowClicked;

        private static readonly Color SelectedRow = Color.FromArgb(0, 52, 0);

        public void AddLine(string text, Color color)
        {
            Items.Add(new Line(text, color));
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= Items.Count)
                return;

            // A clicked row gets a dark green band; the rest stay black.
            bool selected = (e.State & DrawItemState.Selected) != 0;
            using (var background = new SolidBrush(selected ? SelectedRow : BackColor))
            {
                e.Graphics.FillRectangle(background, e.Bounds);
            }

            Color color = Items[e.Index] is Line line ? line.Color : ForeColor;
            TextRenderer.DrawText(e.Graphics, Items[e.Index].ToString(), Font, e.Bounds, color,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            int index = IndexFromPoint(e.Location);
            if (index >= 0 && index < Items.Count)
            {
                RowClicked?.Invoke(index);
            }
        }
    }
}
