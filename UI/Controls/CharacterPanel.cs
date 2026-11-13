/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `CharacterPanel.cs` is a concrete WinForms control that shows hero and enemy 
    portraits or ASCII art. It uses private UI fields and public display methods.
 */
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
            ShowPlayer(hero.Name, hero.ArtKey);
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

            // Portraits read as tone, not letters, so the font shrinks as far as it takes to show the whole picture.
            int textWidth = 0;
            while (fontSize > 0.5f)
            {
                using Font testFont =
                    new Font("Consolas", fontSize);

                Size textSize =
                    TextRenderer.MeasureText(
                        g,
                        new string('W', longestLength),
                        testFont);
                textWidth = textSize.Width;

                // Tall portraits have to fit the height too, now the stats panel sits below.
                if (textSize.Width < _asciiDisplay.ClientSize.Width - 20 &&
                    testFont.Height * lines.Length <= _asciiDisplay.ClientSize.Height)
                {
                    break;
                }

                fontSize -= fontSize > 3f ? 0.25f : 0.1f;
            }

            _asciiDisplay.Font =
                new Font("Consolas", fontSize);

            // Center the picture in the panel.
            _asciiDisplay.SelectAll();
            _asciiDisplay.SelectionIndent = Math.Max(0, (_asciiDisplay.ClientSize.Width - textWidth) / 2);
            _asciiDisplay.Select(0, 0);
        }

        partial void InitializeComponent();
    }
}
