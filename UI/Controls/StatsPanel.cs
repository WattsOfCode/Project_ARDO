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
using ARDO.Core.Entities;
using ARDO.Game.Inventory;
using ARDO.Game.Players;
using ARDO.Interop;
using ARDO.UI.Display;

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
                Font = UiFonts.Mono(9.5f),
                ScrollBars = RichTextBoxScrollBars.Vertical,
                WordWrap = true
            };

            DarkScrollbars.Apply(_statsBox);
            Controls.Add(_statsBox);
        }

        public void UpdateStats(Hero hero)
        {
            string manaLine = hero is Mage mage
                ? $"\nMana    {TextBar.Render(mage.CurrentMana, mage.MaxMana)} {mage.CurrentMana}/{mage.MaxMana}"
                : string.Empty;

            Weapon? weapon = hero.Equipment.EquippedWeapon;
            int pieces = hero.Equipment.ArmorPiecesWorn;
            string weaponLine = weapon == null ? "none" : $"{weapon.Name}\n          {weapon.Summary}";
            string armorLine = pieces == 0
                ? "none"
                : $"+{hero.Equipment.DefenseBonus} DEF\n          {pieces} of {EquipmentManager.ArmorSlots.Length} slots filled";
            string blessing = hero.RunAttackBonus != 0 ? $"\nBlessing  {hero.RunAttackBonus:+#;-#} ATK this run" : string.Empty;

            _statsBox.Text =
$@"================================
{HeaderLine(hero)}
================================

Health  {TextBar.Render(hero.CurrentHealth, hero.MaxHealth)} {hero.CurrentHealth}/{hero.MaxHealth}{manaLine}
XP      {TextBar.Render(hero.Experience, hero.ExperienceToNextLevel)} {hero.Experience}/{hero.ExperienceToNextLevel}

Weapon    {weaponLine}
Armor     {armorLine}{blessing}

Gold      {hero.Gold}

================================";
            _statsBox.SelectionStart = 0;
            _statsBox.ScrollToCaret();
        }

        // Name on the left, level and class pushed right to the edge of the rule.
        private const int RuleWidth = 32;

        private static string HeaderLine(Hero hero)
        {
            string name = " " + hero.Name;
            string right = $"Level {hero.Level} {hero.GetType().Name}";
            if (name.Length + 2 + right.Length > RuleWidth)
                right = $"Lv {hero.Level} {hero.GetType().Name}";

            int gap = System.Math.Max(2, RuleWidth - name.Length - right.Length);
            return name + new string(' ', gap) + right;
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
