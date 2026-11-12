/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `StatsPanel.cs` is a concrete WinForms control that displays the active hero's health, 
    mana, XP, level, and gold. It uses private UI fields and public update methods.
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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

        // The text as written; it is padded on the left to sit centered in the box.
        private string _rawText = string.Empty;
        private bool _scrollToEnd;

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
                WordWrap = false
            };

            _statsBox.Resize += (_, __) => Render();

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

            _rawText =
$@"================================
{HeaderLine(hero)}
================================

Health  {TextBar.Render(hero.CurrentHealth, hero.MaxHealth)} {hero.CurrentHealth}/{hero.MaxHealth}{manaLine}
XP      {TextBar.Render(hero.Experience, hero.ExperienceToNextLevel)} {hero.Experience}/{hero.ExperienceToNextLevel}

Weapon    {weaponLine}
Armor     {armorLine}{blessing}

Gold      {hero.Gold}

================================";
            _scrollToEnd = false;
            Render();
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
            _rawText =
$@"================================
 STATUS
================================

{message}

================================";
            _scrollToEnd = true;
            Render();
        }

        // Centers the whole block (not each line), so the bars and values stay lined up.
        private void Render()
        {
            string[] lines = _rawText.Replace("\r", "").Split('\n');
            int charWidth = Math.Max(1, TextRenderer.MeasureText("WWWWWWWWWW", _statsBox.Font, Size.Empty, TextFormatFlags.NoPadding).Width / 10);
            int fits = _statsBox.ClientSize.Width / charWidth;
            lines = lines.SelectMany(line => Wrap(line, Math.Max(20, fits - 1))).ToArray();
            int widest = lines.Max(line => line.Length);
            string pad = new string(' ', Math.Max(0, (fits - widest) / 2));

            _statsBox.Text = string.Join("\n", lines.Select(line => line.Length == 0 ? line : pad + line));
            _statsBox.SelectionStart = _scrollToEnd ? _statsBox.TextLength : 0;
            _statsBox.ScrollToCaret();
        }

        // Breaks a long line (like a placeholder sentence) at spaces so it fits the box.
        private static IEnumerable<string> Wrap(string line, int width)
        {
            while (line.Length > width)
            {
                int cut = line.LastIndexOf(' ', width);
                if (cut <= 0)
                    cut = width;

                yield return line[..cut].TrimEnd();
                line = line[cut..].TrimStart();
            }

            yield return line;
        }
    }
}
