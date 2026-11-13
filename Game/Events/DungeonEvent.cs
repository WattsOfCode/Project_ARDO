/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `DungeonEvent.cs` holds the small types behind the choices that come up between
    fights: the event itself, each numbered choice, and what a choice leads to.
 */
using System;
using System.Collections.Generic;
using ARDO.Core.Abstracts;
using ARDO.Core.Entities;
using ARDO.Core.Enums;

namespace ARDO.Game.Events
{
    public sealed record EventChoice(string Label, Func<Hero, EventOutcome> Resolve);

    public sealed class DungeonEvent
    {
        public DungeonEvent(string title, IReadOnlyList<string> description, IReadOnlyList<EventChoice> choices)
        {
            Title = title;
            Description = description;
            Choices = choices;
        }

        public string Title { get; }

        public IReadOnlyList<string> Description { get; }

        public IReadOnlyList<EventChoice> Choices { get; }
    }

    public sealed class EventOutcome
    {
        public List<(string Text, MessageTone Tone)> Lines { get; } = new();

        // An item to hand to the hero, and an item the hero gave up.
        public GameItem? Reward { get; set; }
        public GameItem? ItemUsedUp { get; set; }

        // Where the run goes next. With none of these set, the run simply continues.
        public bool StartsAmbush { get; set; }
        public bool ReturnToTown { get; set; }
        public bool PressOn { get; set; }

        // At the sealed throne: which fight lies beyond the door, or just walk back up to town.
        public WheelEnding ThroneFight { get; set; }
        public bool GoHome { get; set; }

        public EventOutcome Say(string text, MessageTone tone = MessageTone.Normal)
        {
            Lines.Add((text, tone));
            return this;
        }
    }
}
