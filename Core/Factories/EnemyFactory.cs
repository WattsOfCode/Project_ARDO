using System;
using ARDO.Core.Abstracts;
using ARDO.Core.Enums;
using ARDO.Game.Enemies;

namespace ARDO.Core.Factories
{
    public static class EnemyFactory
    {
        public static Enemy Create(string enemyKey)
        {
            switch (enemyKey.ToLower())
            {
                case "goblin":
                    return new Goblin();

                case "skeleton":
                    return new Skeleton();

                case "fireimp":
                case "fire imp":
                    return new FireImp();

                case "wraithherald":
                case "wraith herald":
                    return new WraithHerald();

                case "hollowcaptain":
                case "hollow captain":
                    return new HollowCaptain();

                case "alhuezo":
                    return new Boss("Alhuezo, the First King", 999, 80);

                default:
                    throw new ArgumentException(
                        $"Unknown enemy key: {enemyKey}"
                    );
            }
        }

        public static Enemy Create(EnemyType enemyType)
        {
            return enemyType switch
            {
                EnemyType.Goblin => new Goblin(),
                EnemyType.Skeleton => new Skeleton(),
                EnemyType.FireImp => new FireImp(),
                EnemyType.WraithHerald => new WraithHerald(),
                EnemyType.HollowCaptain => new HollowCaptain(),
                EnemyType.Alhuezo => new Boss("Alhuezo, the First King", 999, 80),
                _ => throw new ArgumentOutOfRangeException(nameof(enemyType))
            };
        }
    }
}
