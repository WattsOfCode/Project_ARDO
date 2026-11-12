/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `StatsPanel.cs` is a concrete WinForms control that displays the active hero's health, 
    mana, XP, level, and gold. It uses private UI fields and public update methods.
 */
using System.Drawing;
using System.Windows.Forms;
using ARDO.Core.Abstracts;
using ARDO.Game.Players;

namespace ARDO.UI.Controls
{
    public partial class StatsPanel : UserControl
    {
        private RichTextBox _statsBox = null!;

        public StatsPanel()
        {
            BuildInterface();
        }

        private void BuildInterface()
        {
            BackColor = Color.Black;
            Dock = DockStyle.Fill;
            BorderStyle = BorderStyle.FixedSingle;
            MinimumSize = new Size(320, 0);

            _statsBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                DetectUrls = false,
                TabStop = false,
                BorderStyle = BorderStyle.None,
                BackColor = Color.Black,
                ForeColor = Color.White,
                Font = new Font("Consolas", 9, FontStyle.Regular),
                ScrollBars = RichTextBoxScrollBars.Vertical,
                WordWrap = true
            };

            Controls.Add(_statsBox);
        }

        public void UpdateStats(Hero hero)
        {
            string manaLine = hero is Mage mage
                ? $"\nMana:     {mage.CurrentMana}/{mage.MaxMana}"
                : string.Empty;

            string weapon = hero.Equipment.EquippedWeapon?.Name ?? "none";
            string armor = hero.Equipment.EquippedArmor?.Name ?? "none";

            _statsBox.Text =
$@"================================
 HERO STATUS
================================

Name:     {hero.Name}

Level:    {hero.Level}
XP:       {hero.Experience}/{hero.ExperienceToNextLevel}

Health:   {hero.CurrentHealth}/{hero.MaxHealth}{manaLine}

Weapon:   {weapon} (+{hero.WeaponDamage} ATK)
Armor:    {armor} (+{hero.Equipment.DefenseBonus} DEF)

Gold:     {hero.Gold}

================================";
            _statsBox.SelectionStart = _statsBox.TextLength;
            _statsBox.ScrollToCaret();
        }

        public void ShowPlaceholder(string message)
        {
            _statsBox.Text =
$@"================================
 STATUS
================================

{message}

================================";
            _statsBox.SelectionStart = _statsBox.TextLength;
            _statsBox.ScrollToCaret();
        }
    }
}
