/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `CharacterPanel.cs` is a concrete WinForms control that shows hero and enemy 
    portraits or ASCII art. It uses private UI fields and public display methods.
    A hard-mode hero's portrait carries a small skull badge underneath.
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

        // A small skull under the portrait of a hard-mode hero.
        private Panel _hardModeBadge = null!;

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

            _hardModeBadge = BuildHardModeBadge();

            Controls.Clear();

            Controls.Add(_asciiDisplay);
            Controls.Add(_hardModeBadge);
            Controls.Add(_characterName);

        }

        private static Panel BuildHardModeBadge()
        {
            var badge = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 92,
                BackColor = Color.Black,
                Visible = false
            };

            var skull = new AsciiArtView { ArtColor = Color.LightGray };
            skull.SetArt(AsciiArtLoader.LoadArt("Badges", "skull"));

            var caption = new Label
            {
                Text = "HARD MODE",
                Dock = DockStyle.Bottom,
                Height = 20,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Black,
                ForeColor = Color.Gold,
                Font = new Font("Consolas", 9, FontStyle.Bold)
            };

            badge.Controls.Add(skull);
            badge.Controls.Add(caption);
            return badge;
        }

        // Used when no hero is loaded, so the badge goes too.
        public void UpdateCharacterName(string name)
        {
            _characterName.Text = name;
            _hardModeBadge.Visible = false;
        }

        public void UpdateAsciiArt(string category, string fileName)
        {
            _rawAsciiArt = AsciiArtLoader.LoadArt(category, fileName);

            _asciiDisplay.SetArt(_rawAsciiArt);
        }

        public void ShowPlayer(Hero hero)
        {
            ShowPlayer(hero.Name, hero.ArtKey);
            _hardModeBadge.Visible = hero.HardMode;
        }

        public void ShowPlayer(string name)
        {
            ShowPlayer(name, "warrior");
        }

        private void ShowPlayer(string name, string spriteKey)
        {
            _characterName.Text = name;
            _hardModeBadge.Visible = false;
            _asciiDisplay.ArtColor = AsciiArtView.DefaultArtColor;
            UpdateAsciiArt("Heroes", spriteKey);
        }

        public void ShowEnemy(string name, string folder, string enemyFile, bool shadowTint)
        {
            _characterName.Text = name;
            _hardModeBadge.Visible = false;

            _asciiDisplay.ArtColor = shadowTint ? AsciiArtView.ShadowArtColor : AsciiArtView.DefaultArtColor;
            UpdateAsciiArt(folder, enemyFile);
        }

        partial void InitializeComponent();
    }
}
