using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Entities.Characters.Enemies;

namespace Threadlock.Components.TiledComponents
{
    public class EnemySpawnPoint : TiledComponent
    {
        Type? _enemyType;

        public override void Initialize()
        {
            base.Initialize();

            if (TmxObject.Properties != null && TmxObject.Properties.TryGetValue("EnemyType", out var enemyTypeString))
            {
                Type enemyType = Type.GetType($"Threadlock.Entities.Characters.Enemies.{enemyTypeString}.{enemyTypeString}");
                if (enemyType != null)
                {
                    _enemyType = enemyType;
                }
            }
        }

        public BaseEnemy SpawnEnemy(Type type)
        {
            var enemy = Entity.Scene.AddEntity((BaseEnemy)Activator.CreateInstance(type));
            enemy.SetPosition(Entity.Position);

            return enemy;
        }

        public BaseEnemy SpawnEnemy()
        {
            if (_enemyType == null)
                return null;

            var enemy = Entity.Scene.AddEntity((BaseEnemy)Activator.CreateInstance(_enemyType));
            enemy.SetPosition(Entity.Position);

            return enemy;
        }
    }
}
