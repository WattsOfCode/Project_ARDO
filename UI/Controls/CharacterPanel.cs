using System;
using System.Drawing;
using System.Windows.Forms;
using ARDO.Core.Abstracts;
using ARDO.Interop;
using ARDO.Game.Players;

namespace ARDO.UI.Controls
{
    public partial class CharacterPanel : UserControl
    {
        private RichTextBox _asciiDisplay = null!;
        private Label _characterName = null!;

        private string _rawAsciiArt = string.Empty;

        public CharacterPanel()
        {
            InitializeComponent();

            BuildInterface();
        }

        private void BuildInterface()
        {
            BackColor = Color.Black;
            Dock = DockStyle.Fill;
            BorderStyle = BorderStyle.FixedSingle;

            _characterName = new Label
            {
                Text = "Character Display",

                Dock = DockStyle.Top,
                Height = 30,

                TextAlign = ContentAlignment.MiddleCenter,

                BackColor = Color.Black,
                ForeColor = Color.White,

                Font = new Font("Consolas", 12, FontStyle.Bold)
            };

            _asciiDisplay = new RichTextBox
            {
                Dock = DockStyle.Fill,

                ReadOnly = true,

                BorderStyle = BorderStyle.None,

                BackColor = Color.Black,
                ForeColor = Color.LightGreen,

                Font = new Font("Consolas", 10),

                ScrollBars = RichTextBoxScrollBars.None,

                WordWrap = false,

                DetectUrls = false,

                TabStop = false
            };

            Controls.Clear();

            Controls.Add(_asciiDisplay);
            Controls.Add(_characterName);

            Resize += (s, e) => FitFontToScreen();
        }

        public void UpdateCharacterName(string name)
        {
            _characterName.Text = name;
        }

        public void UpdateAsciiArt(string category, string fileName)
        {
            _rawAsciiArt = AsciiArtLoader.LoadArt(category, fileName);

            _asciiDisplay.Text = _rawAsciiArt;

            FitFontToScreen();
        }

        public void ShowPlayer(Hero hero)
        {
            if (hero is Mage)
            {
                ShowPlayer(hero.Name, "mage");
                return;
            }

            ShowPlayer(hero.Name, "warrior");
        }

        public void ShowPlayer(string name)
        {
            ShowPlayer(name, "warrior");
        }

        private void ShowPlayer(string name, string spriteKey)
        {
            _characterName.Text = name;
            UpdateAsciiArt("Heroes", spriteKey);
        }

        public void ShowEnemy(string name, string enemyFile)
        {
            _characterName.Text = name;

            UpdateAsciiArt("Enemies", enemyFile);
        }

        private void FitFontToScreen()
        {
            if (string.IsNullOrWhiteSpace(_rawAsciiArt))
                return;

            if (_asciiDisplay.ClientSize.Width <= 0)
                return;

            string[] lines = _rawAsciiArt.Split('\n');

            int longestLength = 0;

            foreach (string line in lines)
            {
                if (line.Length > longestLength)
                {
                    longestLength = line.Length;
                }
            }

            float fontSize = 12f;

            using Graphics g = _asciiDisplay.CreateGraphics();

            while (fontSize > 4f)
            {
                using Font testFont =
                    new Font("Consolas", fontSize);

                Size textSize =
                    TextRenderer.MeasureText(
                        g,
                        new string('W', longestLength),
                        testFont);

                if (textSize.Width <
                    _asciiDisplay.ClientSize.Width - 20)
                {
                    break;
                }

                fontSize -= 0.5f;
            }

            _asciiDisplay.Font =
                new Font("Consolas", fontSize);
        }

        partial void InitializeComponent();
    }
}
