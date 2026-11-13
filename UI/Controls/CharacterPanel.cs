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
        private AsciiArtView _asciiDisplay = null!;
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

            // Fits any size of picture: as text when it can be read, scaled as a picture when it is very detailed.
            _asciiDisplay = new AsciiArtView();

            Controls.Clear();

            Controls.Add(_asciiDisplay);
            Controls.Add(_characterName);

        }

        public void UpdateCharacterName(string name)
        {
            _characterName.Text = name;
        }

        public void UpdateAsciiArt(string category, string fileName)
        {
            _rawAsciiArt = AsciiArtLoader.LoadArt(category, fileName);

            _asciiDisplay.SetArt(_rawAsciiArt);
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

        partial void InitializeComponent();
    }
}
