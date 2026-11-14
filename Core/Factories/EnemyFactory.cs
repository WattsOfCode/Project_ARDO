/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `EnemyFactory.cs` is a static factory file that creates concrete enemy instances
    for encounters. It exposes creation methods.
 */
using System;
using ARDO.Core.Abstracts;
using ARDO.Core.Enums;
using ARDO.Game.Enemies;

namespace ARDO.Core.Factories
{
    public static class EnemyFactory
    {
        public static Enemy Create(EnemyType enemyType)
        {
            return enemyType switch
            {
                EnemyType.Goblin => new Goblin(),
                EnemyType.Skeleton => new Skeleton(),
                EnemyType.FireImp => new FireImp(),
                EnemyType.WraithHerald => new WraithHerald(),
                EnemyType.HollowCaptain => new HollowCaptain(),
                EnemyType.Alhuezo => new Alhuezo(),
                EnemyType.GoblinCutpurse => new GoblinCutpurse(),
                EnemyType.GoblinBrute => new GoblinBrute(),
                EnemyType.GoblinShaman => new GoblinShaman(),
                _ => throw new ArgumentOutOfRangeException(nameof(enemyType))
            };
        }
    }
}
