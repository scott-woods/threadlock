using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Entities.Characters.Enemies;

namespace Threadlock.Managers
{
    public class EncounterManager
    {
        List<Enemy> _enemies = new List<Enemy>();
        Action _onEncounterComplete;

        public EncounterManager(Action onEncounterComplete)
        {
            _onEncounterComplete = onEncounterComplete;
        }

        public void AddEnemy(Enemy enemy)
        {
            if (enemy.TryGetComponent<DeathComponent>(out var dc))
            {
                dc.Emitter.AddObserver(DeathEventTypes.Started, OnEnemyDeath);
                _enemies.Add(enemy);
            }
        }

        void OnEnemyDeath(Entity enemyEntity)
        {
            if (enemyEntity.TryGetComponent<DeathComponent>(out var dc))
                dc.Emitter.RemoveObserver(DeathEventTypes.Started, OnEnemyDeath);

            _enemies.Remove(enemyEntity as Enemy);

            if (_enemies.Count == 0)
                _onEncounterComplete?.Invoke();
        }
    }
}
