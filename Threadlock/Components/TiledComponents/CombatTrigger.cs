using Nez;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Entities.Characters.Enemies.ChainBot;
using Threadlock.StaticData;

namespace Threadlock.Components.TiledComponents
{
    public class CombatTrigger : AreaTrigger
    {
        public override void OnTriggered()
        {
            SetEnabled(false);

            //destroy other triggers if any are out there
            //var triggers = FindComponentsOnMap<CombatTrigger>().Where(t => t != this);
            //foreach (var trigger in triggers)
            //    trigger.Entity.Destroy();

            //Game1.StartCoroutine(CombatTriggerCoroutine());
        }

        IEnumerator CombatTriggerCoroutine()
        {
            //lock gates
            Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds.Gate_close);
            var doorways = FindComponentsOnMap<DungeonDoorway>();
            foreach (var doorway in doorways)
            {
                doorway.SetGateOpen(false);
            }

            //wait 1 second before spawning enemies
            yield return Coroutine.WaitForSeconds(1f);

            //spawn enemies
            var enemySpawns = FindComponentsOnMap<EnemySpawnPoint>();
            if (enemySpawns != null && enemySpawns.Count > 0)
            {
                if (MapEntity.TryGetComponent<TiledMapRenderer>(out var renderer))
                {
                    if (renderer.TiledMap.Properties.TryGetValue("Area", out var areaString))
                    {
                        if (Areas.TryGetArea(areaString, out var area))
                        {
                            int i = 0;
                            var typesPicked = new List<Type>();
                            while (i < enemySpawns.Count)
                            {
                                var spawn = enemySpawns[i];

                                Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds.Enemy_spawn);

                                Type enemyType;

                                //get enemy types that have been picked less than 3 times
                                var possibleTypes = area.EnemyTypes.Where(t =>
                                {
                                    if (typesPicked.Contains(t))
                                        if (typesPicked.Count(p => p == t) < 3)
                                            return true;
                                    return false;
                                }).ToList();

                                //if any possible types, get a random from that list
                                if (possibleTypes.Any())
                                    enemyType = possibleTypes.RandomItem();
                                else //if all types are already at max, just pick another random one
                                    enemyType = area.EnemyTypes.RandomItem();

                                //add this type to typesPicked list
                                typesPicked.Add(enemyType);

                                //spawn enemy
                                //spawn.SpawnEnemy(typeof(ChainBot));
                                spawn.SpawnEnemy(enemyType);

                                //wait a moment before spawning next enemy
                                yield return Coroutine.WaitForSeconds(.2f);

                                i++;
                            }
                        }
                    }
                }
            }

            //destroy trigger entity
            Entity.Destroy();
        }
    }
}
