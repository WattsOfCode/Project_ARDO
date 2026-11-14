/**
 * Name: John Deardorff
 * Date: 11/13/26
 * Project ARDO
 *
 * Class description:
 * `GuardianEvents.cs` builds the approach to each floor's guardian: what the
    hero sees, a fair warning of how the guardian fights, and the choice to face
    it or turn back with the floor left uncleared.
 */
using System.Collections.Generic;
using ARDO.Core.Abstracts;
using ARDO.Game.Enemies;

namespace ARDO.Game.Events
{
    public static class GuardianEvents
    {
        public const string TitlePrefix = "GUARDIAN: ";

        public static Enemy For(int floor) => floor switch
        {
            1 => new GriskGoblinKing(),
            2 => new CinderMatron(),
            _ => new RavenWitch()
        };

        public static DungeonEvent Approach(int floor, int wheelTurn = 0)
        {
            Enemy guardian = For(floor);
            List<string> description = new(floor switch
            {
                1 => new[]
                {
                    "The tunnel opens into a cave hung with stolen banners. On a throne of crates sits a goblin " +
                    "in a hood and a mantle of white fur strung with skulls. A blade burns green in one hand, a skull in the other, and he is delighted to see you.",
                    "He calls his lackeys to him when he can. They will not stay once he falls."
                },
                2 => new[]
                {
                    "The heat reaches you before the light does. In a field of ash and burnt trees, something horned and " +
                    "four-armed rises from the coals, fire running in the cracks of her stone skin, and grins with a mouth full of flame.",
                    "Fire only feeds her. Every third breath, she lets loose a Firestorm."
                },
                _ => new[]
                {
                    "On the cathedral roof, under a full moon and a forest of broken spires, a pale woman in a black gown waits, " +
                    "white hair stirring though there is no wind. A furled black parasol hangs at her side. Ravens line the spires, watching.",
                    "Every wound she gives, she drinks. Her open parasol turns aside the next blow. Hurt her badly and the ravens come down."
                }
            });

            if (wheelTurn >= Progression.WheelTurns.SecondPhaseTurn)
            {
                description.Add(floor switch
                {
                    1 => "This turn of the wheel, his treasure sack bulges. Hurt him badly and he will hide behind every coin he has taken from you.",
                    2 => "This turn of the wheel, the ground here glows. Hurt her badly and the floor itself will burn you, hotter every round.",
                    _ => "This turn of the wheel, the ravens are hungrier. Hurt her badly and she will hide in them, and they will feed her."
                });
            }

            return new DungeonEvent(
                TitlePrefix + guardian.Name.ToUpperInvariant(),
                description,
                new[]
                {
                    new EventChoice($"Face {guardian.Name}", _ => new EventOutcome { Guardian = guardian }),
                    new EventChoice("Turn back to Oakhaven (the floor stays uncleared)", _ =>
                        new EventOutcome { GoHome = true }.Say("You back away. The guardian lets you go, this time.", Core.Enums.MessageTone.Dim))
                });
        }
    }
}
