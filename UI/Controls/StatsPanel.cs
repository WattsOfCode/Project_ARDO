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

        public void UpdateStats(
            string name,
            int level,
            int experience,
            int experienceToNextLevel,
            int currentHealth,
            int maxHealth,
            int currentMana,
            int maxMana,
            int gold)
        {
            string manaLine = maxMana > 0
                ? $"\nMana:     {currentMana}/{maxMana}"
                : string.Empty;

            _statsBox.Text =
$@"================================
 HERO STATUS
================================

Name:     {name}

Level:    {level}
XP:       {experience}/{experienceToNextLevel}

Health:   {currentHealth}/{maxHealth}{manaLine}

Gold:     {gold}

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
