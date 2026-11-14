/**
 * Name: John Deardorff
 * Date: 11/14/26
 * Project ARDO
 *
 * Class description:
 * `TownTalk.cs` is the inn's common room, where the hero can talk to whoever is
    in town. Maren is always there. Eldrin turns up once the hero has reached the
    Emberward Depths, and Sera once they have reached the Hollow Cathedral. Each
    has a few lines that change with the hero's progress; talking again gives the
    next one.
 */
using System.Collections.Generic;
using System.Linq;
using ARDO.Core.Abstracts;
using ARDO.Core.Enums;

namespace ARDO.Game.Events
{
    public static class TownTalk
    {
        public const string Title = "THE COMMON ROOM";

        private sealed record Speaker(string Key, string Label, IReadOnlyList<string> Lines);

        // Talking in town: one choice per person present, then a way back to the town menu.
        public static DungeonEvent CommonRoom(Hero hero, IDictionary<string, int> timesTalked)
        {
            List<Speaker> present = WhoIsHere(hero).ToList();
            var choices = present
                .Select(speaker => new EventChoice($"Talk to {speaker.Label}", _ =>
                {
                    timesTalked.TryGetValue(speaker.Key, out int count);
                    timesTalked[speaker.Key] = count + 1;
                    string line = speaker.Lines[count % speaker.Lines.Count];
                    return new EventOutcome().Say($"[{speaker.Label}]: \"{line}\"", MessageTone.Dim);
                }))
                .ToList();
            choices.Add(new EventChoice("Back to the town", _ => new EventOutcome()));

            return new DungeonEvent(
                Title,
                new[] { present.Count == 1 ? "The common room is quiet. Maren is wiping down the bar." : "The common room is warm and half full. A few faces you know look up." },
                choices);
        }

        private static IEnumerable<Speaker> WhoIsHere(Hero hero)
        {
            int reached = hero.StoryComplete ? 4 : hero.ActiveStoryNode;
            bool helpedEldrin = hero.HasDeed(Hero.DeedHelpedEldrin);

            var maren = new List<string> { $"Well met, {hero.Name}. I'm glad to see you again." };
            if (hero.StoryComplete)
                maren.Add("You came back from the very bottom. Most don't come back from the top floor.");
            else
                maren.Add("Whatever you're chasing down there, eat something first. The dark doesn't care if you're hungry.");
            if (hero.WheelTurn > 0)
                maren.Add("The monsters seem to be getting more riled up in the depths. I wonder what's going on.");
            yield return new Speaker("maren", "Maren, Inn Keeper", maren);

            if (reached >= 2 || helpedEldrin)
            {
                var eldrin = new List<string> { $"Well met, {hero.Name}. I'm glad to see you again." };
                if (helpedEldrin)
                    eldrin.Add("I'm doing much better now that I'm not in that awful place anymore. I owe you for that potion.");
                eldrin.Add("While I was down there I heard chatter, hiding from one of the floor guardians. There's a being as old as time on the final level.");
                yield return new Speaker("eldrin", "Eldrin, Wandering Scout", eldrin);
            }

            if (reached >= 3)
            {
                var sera = new List<string> { "The bells rang twice last night. They never ring for anything good." };
                if (hero.StoryComplete)
                    sera.Add("There's apparently a door in the depths that needs three keys to open. The old records call it the wheel's own door.");
                else
                    sera.Add("The cathedral's records go back further than Oakhaven. Every page ends with the same name: Alhuezo.");
                yield return new Speaker("sera", "Sera, Cathedral Scribe", sera);
            }
        }
    }
}
