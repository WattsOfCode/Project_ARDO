using System.Drawing;
using System.Windows.Forms;

namespace ARDO.UI.Controls
{
    public partial class NarrativePanel : UserControl
    {
        private RichTextBox _narrativeBox = null!;

        public NarrativePanel()
        {
            BuildInterface();
        }

        private void BuildInterface()
        {
            BackColor = Color.Black;
            Dock = DockStyle.Fill;
            BorderStyle = BorderStyle.FixedSingle;

            _narrativeBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                DetectUrls = false,
                TabStop = false,

                BackColor = Color.Black,
                ForeColor = Color.LightGreen,

                BorderStyle = BorderStyle.None,

                Font = new Font("Consolas", 10),

                ScrollBars = RichTextBoxScrollBars.Vertical
            };

            Controls.Add(_narrativeBox);
        }

        public void AppendNarrative(string text)
        {
            _narrativeBox.AppendText(text + Environment.NewLine);

            _narrativeBox.SelectionStart = _narrativeBox.Text.Length;
            _narrativeBox.ScrollToCaret();
        }

        public void ClearNarrative()
        {
            _narrativeBox.Clear();
        }
    }
}