/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `NarrativePanel.cs` is a concrete WinForms control that displays scrolling
    story text and command feedback. Lines are colored by their tone and revealed
    a few at a time, so combat reads like a sequence instead of a wall of text.
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ARDO.Core.Enums;
using ARDO.Interop;
using ARDO.UI.Display;

namespace ARDO.UI.Controls
{
    public partial class NarrativePanel : UserControl
    {
        private const int RevealIntervalMs = 25;
        private const float TextSize = 11f;
        private const double LineHeight = 1.2;
        private const int PointsAfterLine = 1;

        // ARDO's original scheme: black background, light green story text, gold titles, sky-blue for the player.
        private static readonly Color Background = Color.Black;

        private readonly Queue<(string Text, MessageTone Tone)> _pending = new();
        private readonly System.Windows.Forms.Timer _revealTimer = new() { Interval = RevealIntervalMs };

        private RichTextBox _narrativeBox = null!;
        private Font _regularFont = null!;
        private Font _headerFont = null!;

        public NarrativePanel()
        {
            BuildInterface();
            _revealTimer.Tick += (_, __) => RevealNextLines();
        }

        private void BuildInterface()
        {
            BackColor = Background;
            Dock = DockStyle.Fill;
            BorderStyle = BorderStyle.FixedSingle;

            _regularFont = UiFonts.Mono(TextSize);
            _headerFont = UiFonts.Mono(TextSize, FontStyle.Bold);

            // RichTextBox has no padding of its own, so it sits inside a padded panel.
            var margin = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Background,
                Padding = new Padding(24, 16, 12, 12)
            };

            _narrativeBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                DetectUrls = false,
                TabStop = false,
                BackColor = Background,
                ForeColor = ColorFor(MessageTone.Normal),
                BorderStyle = BorderStyle.None,
                Font = _regularFont,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };

            _narrativeBox.HandleCreated += (_, __) => RichTextSpacing.Apply(_narrativeBox, LineHeight, PointsAfterLine);

            DarkScrollbars.Apply(_narrativeBox);

            margin.Controls.Add(_narrativeBox);
            Controls.Add(margin);
        }

        public void AppendNarrative(string text, MessageTone tone = MessageTone.Normal)
        {
            // Before the window exists (and in tests) there is nothing to animate, so write straight away.
            if (!IsHandleCreated)
            {
                WriteLine(text ?? string.Empty, tone);
                return;
            }

            _pending.Enqueue((text ?? string.Empty, tone));
            if (!_revealTimer.Enabled)
            {
                _revealTimer.Start();
            }
        }

        // Shows everything still waiting, e.g. when the player types the next command.
        public void FlushPending()
        {
            _revealTimer.Stop();
            while (_pending.Count > 0)
            {
                var (text, tone) = _pending.Dequeue();
                WriteLine(text, tone);
            }
        }

        // One page up (-1) or down (1), for PageUp / PageDown in the command box.
        public void ScrollPage(int direction)
        {
            const int WM_VSCROLL = 0x0115, SB_PAGEUP = 2, SB_PAGEDOWN = 3;
            SendMessage(_narrativeBox.Handle, WM_VSCROLL, (IntPtr)(direction < 0 ? SB_PAGEUP : SB_PAGEDOWN), IntPtr.Zero);
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        public void ClearNarrative()
        {
            _revealTimer.Stop();
            _pending.Clear();
            _narrativeBox.Clear();
            RichTextSpacing.Apply(_narrativeBox, LineHeight, PointsAfterLine);
        }

        private void RevealNextLines()
        {
            // Long blocks (the intro, the epilogue) speed up so they never feel slow.
            int linesThisTick = _pending.Count > 40 ? 6 : _pending.Count > 15 ? 2 : 1;

            while (linesThisTick > 0 && _pending.Count > 0)
            {
                var (text, tone) = _pending.Dequeue();
                WriteLine(text, tone);

                // Blank lines are free; they don't use up a reveal step.
                if (text.Length > 0)
                {
                    linesThisTick--;
                }
            }

            if (_pending.Count == 0)
            {
                _revealTimer.Stop();
            }
        }

        private void WriteLine(string text, MessageTone tone)
        {
            if (tone == MessageTone.Normal)
            {
                tone = GuessTone(text);
            }

            _narrativeBox.SelectionStart = _narrativeBox.TextLength;
            _narrativeBox.SelectionLength = 0;
            _narrativeBox.SelectionColor = ColorFor(tone);
            _narrativeBox.SelectionFont = tone == MessageTone.Header ? _headerFont : _regularFont;
            _narrativeBox.SelectedText = text + "\n";

            _narrativeBox.SelectionStart = _narrativeBox.TextLength;
            _narrativeBox.ScrollToCaret();
        }

        // Banner rules and echoed commands get their look without every caller asking for it.
        private static MessageTone GuessTone(string text)
        {
            if (text.StartsWith("===") || text.StartsWith("---"))
                return MessageTone.Header;

            if (text.StartsWith("> "))
                return MessageTone.Command;

            return MessageTone.Normal;
        }

        internal static Color ColorFor(MessageTone tone)
        {
            return tone switch
            {
                MessageTone.Dim => Color.LightGray,
                MessageTone.Header => Color.Gold,
                MessageTone.Command => Color.LightSkyBlue,
                MessageTone.Damage => Color.Salmon,
                MessageTone.Heal => Color.SpringGreen,
                MessageTone.Gold => Color.Gold,
                MessageTone.Warning => Color.Orange,
                MessageTone.Common => Color.WhiteSmoke,
                MessageTone.Uncommon => Color.LimeGreen,
                MessageTone.Rare => Color.CornflowerBlue,
                MessageTone.Epic => Color.MediumOrchid,
                MessageTone.Legendary => Color.DarkOrange,
                _ => Color.LightGreen
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _revealTimer.Dispose();
                _regularFont.Dispose();
                _headerFont.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
