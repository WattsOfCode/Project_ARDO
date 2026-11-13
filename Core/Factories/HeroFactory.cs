/**
 * Name: John Deardorff
 * Date: 11/12/26
 * Project ARDO
 *
 * Class description:
 * `HeroFactory.cs` is a static factory that builds a hero from its saved class
    name. The save file, the load screen and the creation screen all go through
    it, so a new class only has to be added here.
 */
using System.Collections.Generic;
using System.IO;
using ARDO.Core.Abstracts;
using ARDO.Game.Players;

namespace ARDO.Core.Factories
{
    public static class HeroFactory
    {
        // In the order the creation screen shows them.
        public static readonly IReadOnlyList<string> ClassNames = new[] { "Warrior", "Mage", "Archer", "Rogue" };

        public static Hero Create(string name, string classType)
        {
            return classType switch
            {
                "Warrior" => new Warrior(name),
                "Mage" => new Mage(name),
                "Archer" => new Archer(name),
                "Rogue" => new Rogue(name),
                _ => throw new InvalidDataException($"Unknown hero profile variant: {classType}")
            };
        }
    }
}
