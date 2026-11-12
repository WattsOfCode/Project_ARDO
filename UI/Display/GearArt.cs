/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `GearArt.cs` holds the gear screen picture: the shield, the armored figure and
    the sword stamped side by side onto one character grid, plus where each
    equipment slot's label belongs and what it says for the current hero.
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using ARDO.Core.Abstracts;
using ARDO.Core.Entities;
using ARDO.Core.Enums;
using ARDO.Interop;
using ARDO.UI.Controls;

namespace ARDO.UI.Display
{
    public static class GearArt
    {
        // Left labels point at the first mark on their row, right labels at the last.
        public sealed record SlotLabel(EquipmentSlot Slot, bool Left, int Row, string ItemText, string StatText, Color ItemColor);

        // Where each piece of art sits on the canvas (row, column).
        private static readonly (string File, int Row, int Col)[] Pieces =
        {
            ("armor-figure", 0, 23),
            ("shield", 21, 0),
            ("sword", 21, 72)
        };

        // Each slot's label sits beside the canvas row that shows that piece of armor.
        private static readonly (EquipmentSlot Slot, bool Left, int Row)[] SlotRows =
        {
            (EquipmentSlot.Helmet, true, 3),
            (EquipmentSlot.Shield, true, 30),
            (EquipmentSlot.Boots, true, 51),
            (EquipmentSlot.Chest, false, 10),
            (EquipmentSlot.Ring, false, 19),
            (EquipmentSlot.Weapon, false, 40)
        };

        private static string[]? _canvas;

        // The three art files stamped onto one grid. Built once; the art never changes.
        public static string[] Canvas
        {
            get
            {
                _canvas ??= BuildCanvas();
                return _canvas;
            }
        }

        public static List<SlotLabel> LabelsFor(Hero hero)
        {
            var labels = new List<SlotLabel>();
            string[] canvas = Canvas;

            foreach ((EquipmentSlot slot, bool left, int row) in SlotRows)
            {
                // Only happens if an art file is missing and the canvas came out short.
                if (row >= canvas.Length)
                    continue;

                GameItem? item = EquippedIn(hero, slot);
                labels.Add(new SlotLabel(
                    slot,
                    left,
                    row,
                    item?.Name ?? "empty",
                    item?.Summary ?? string.Empty,
                    item == null ? Color.LightGray : NarrativePanel.ColorFor(item.Rarity.ToTone())));
            }

            return labels;
        }

        private static GameItem? EquippedIn(Hero hero, EquipmentSlot slot) => hero.Equipment.ItemIn(slot);

        private static string[] BuildCanvas()
        {
            var art = new List<(string[] Lines, int Row, int Col)>();
            int height = 0, width = 0;
            foreach ((string file, int row, int col) in Pieces)
            {
                string[] pieceLines = AsciiArtLoader.LoadArt("Gear", file).Replace("\r", "").TrimEnd('\n').Split('\n');
                art.Add((pieceLines, row, col));
                height = Math.Max(height, row + pieceLines.Length);
                foreach (string l in pieceLines)
                {
                    width = Math.Max(width, col + l.Length);
                }
            }

            var grid = new char[height][];
            for (int r = 0; r < height; r++)
            {
                grid[r] = new string(' ', width).ToCharArray();
            }

            foreach ((string[] pieceLines, int row, int col) in art)
            {
                for (int r = 0; r < pieceLines.Length; r++)
                {
                    for (int c = 0; c < pieceLines[r].Length; c++)
                    {
                        if (pieceLines[r][c] != ' ')
                        {
                            grid[row + r][col + c] = pieceLines[r][c];
                        }
                    }
                }
            }

            var lines = new string[height];
            for (int r = 0; r < height; r++)
            {
                lines[r] = new string(grid[r]);
            }

            return lines;
        }
    }
}
