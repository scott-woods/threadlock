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
        public BaseEnemy SpawnEnemy(Type type)
        {
            var enemy = Entity.Scene.AddEntity((BaseEnemy)Activator.CreateInstance(type));
            enemy.SetPosition(Entity.Position);

            return enemy;
        }
    }
}
