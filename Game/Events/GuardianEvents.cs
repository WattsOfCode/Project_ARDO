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

        public static DungeonEvent Approach(int floor)
        {
            Enemy guardian = For(floor);
            IReadOnlyList<string> description = floor switch
            {
                1 => new[]
                {
                    "The tunnel opens into a cave hung with stolen banners. On a throne of crates sits a goblin " +
                    "with a cooking pot for a crown, and he is delighted to see you.",
                    "He calls his lackeys to him when he can. They will not stay once he falls."
                },
                2 => new[]
                {
                    "The heat reaches you before the light does. In a chamber of black glass, a woman made of coals " +
                    "rises from the furnace floor and smiles with a mouth full of sparks.",
                    "Fire only feeds her. Every third breath, she lets loose a Firestorm."
                },
                _ => new[]
                {
                    "In the nave of the Hollow Cathedral, a woman in white lace waits under a pale parasol, " +
                    "though no sun has reached this place in a thousand years. Ravens line the rafters, watching.",
                    "Every wound she gives, she drinks. Her open parasol turns aside the next blow. Hurt her badly and the ravens come down."
                }
            };

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
