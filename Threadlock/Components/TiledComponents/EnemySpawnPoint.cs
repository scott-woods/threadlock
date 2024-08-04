using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Entities.Characters.Enemies;
using Threadlock.Models;
using Threadlock.StaticData;

namespace Threadlock.Components.TiledComponents
{
    public class EnemySpawnPoint : TiledComponent
    {
        EnemyConfig _config;

        public override void Initialize()
        {
            base.Initialize();

            if (TmxObject.Properties != null && TmxObject.Properties.TryGetValue("EnemyType", out var enemyTypeString))
            {
                if (Enemies.EnemyConfigDictionary.TryGetValue(enemyTypeString, out var enemyConfig))
                    _config = enemyConfig;
            }
        }

        public Enemy SpawnEnemy(EnemyConfig config)
        {
            var enemy = Entity.Scene.AddEntity(new Enemy(config));
            enemy.SetPosition(Entity.Position);
            return enemy;
        }

        public Enemy SpawnEnemy()
        {
            if (TmxObject.Properties != null && TmxObject.Properties.TryGetValue("EnemyType", out var enemyTypeString))
            {
                if (DataLoader.EnemyDataDictionary.TryGetValue(enemyTypeString, out var enemyData))
                {
                    var enemy = Entity.Scene.AddEntity(enemyData.CreateEnemy());
                    enemy.SetPosition(Entity.Position);
                    return enemy;
                }
            }

            return null;

            //if (_config == null)
            //    return null;

            //return SpawnEnemy(_config);
        }
    }
}
