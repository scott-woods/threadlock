using Nez;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components.TiledComponents;
using Threadlock.Entities.Characters.Player;
using Threadlock.Entities.Characters.Player.BasicWeapons;
using Threadlock.Helpers;
using Threadlock.Managers;

namespace Threadlock.StaticData
{
    public static class Events
    {
        public static readonly Dictionary<string, Func<Trigger, IEnumerator>> EventsMap = new Dictionary<string, Func<Trigger, IEnumerator>>()
        {
            { "DungeonEncounter", DungeonEncounter },
            { "ExitArea", ExitArea },
            { "SpawnTestEnemy", SpawnTestEnemy },
            { "ChangeWeapon", ChangeWeapon },
            { "Dialogue", Dialogue }
        };

        static bool VerifyArgs(List<string> args, int requiredCount)
        {
            return args != null && args.Count >= requiredCount;
        }

        #region EVENTS

        public static IEnumerator Dialogue(Trigger trigger)
        {
            if (!VerifyArgs(trigger.Args, 2))
                yield break;

            var location = trigger.Args[0];
            var id = trigger.Args[1];

            var dialogueSet = DialogueLoader.GetDialogue(location, id);

            if (dialogueSet == null)
                yield break;

            yield return Game1.UIManager.ShowTextboxText(dialogueSet);
        }

        public static IEnumerator DungeonEncounter(Trigger trigger)
        {
            //disable other encounter triggers on map
            var triggers = trigger.FindComponentsOnMap<Trigger>().Where(t => t.Handler == DungeonEncounter);
            foreach (var t in triggers)
                t.SetEnabled(false);

            //lock gates
            Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds.Gate_close);
            var doorways = trigger.FindComponentsOnMap<DungeonDoorway>();
            foreach (var doorway in doorways)
            {
                doorway.SetGateOpen(false);
            }

            //wait 1 second before spawning enemies
            yield return Coroutine.WaitForSeconds(1f);

            //spawn enemies
            var enemySpawns = trigger.FindComponentsOnMap<EnemySpawnPoint>();
            if (enemySpawns != null && enemySpawns.Count > 0)
            {
                if (trigger.MapEntity.TryGetComponent<TiledMapRenderer>(out var renderer))
                {
                    if (renderer.TiledMap.Properties.TryGetValue("Area", out var areaString))
                    {
                        if (Areas.TryGetArea(areaString, out var area))
                        {
                            var encounterManager = new EncounterManager(() =>
                            {
                                foreach (var doorway in doorways)
                                    doorway.SetGateOpen(true);
                            });

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
                                        return typesPicked.Count(p => p == t) < 3;
                                    return true;
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
                                var enemy = spawn.SpawnEnemy(enemyType);
                                yield return null;
                                encounterManager.AddEnemy(enemy);

                                //wait a moment before spawning next enemy
                                yield return Coroutine.WaitForSeconds(.2f);

                                i++;
                            }
                        }
                    }
                }
            }

            //destroy other encounter triggers
            foreach (var t in triggers)
                t.Entity.Destroy();
        }

        public static IEnumerator ExitArea(Trigger trigger)
        {
            if (!VerifyArgs(trigger.Args, 2))
                yield break;

            var targetSceneType = Type.GetType("Threadlock.Scenes." + trigger.Args[0]);
            if (targetSceneType == null)
                yield break;
            var targetSpawn = trigger.Args[1];

            if (trigger.Args.Count >= 3)
            {
                var stopMusicString = trigger.Args[2];
                var stopMusic = Convert.ToBoolean(stopMusicString);
                if (stopMusic)
                    Game1.AudioManager.StopMusic();
            }

            Game1.SceneManager.ChangeScene(targetSceneType, targetSpawn);

            trigger.Entity.Destroy();

            yield break;
        }

        public static IEnumerator SpawnTestEnemy(Trigger trigger)
        {
            if (!VerifyArgs(trigger.Args, 1))
                yield break;

            var enemyName = trigger.Args[0];
            var enemyType = Type.GetType($"Threadlock.Entities.Characters.Enemies.{enemyName}.{enemyName}")
                ?? Type.GetType($"Threadlock.Entities.Characters.Enemies.{enemyName}");
            if (enemyType == null)
                yield break;

            var spawns = trigger.FindComponentsOnMap<EnemySpawnPoint>();
            if (spawns != null && spawns.Count > 0)
                spawns.First().SpawnEnemy(enemyType);
        }

        public static IEnumerator ChangeWeapon(Trigger trigger)
        {
            if (!VerifyArgs(trigger.Args, 1))
                yield break;

            var weaponName = trigger.Args[0];
            var weaponType = Type.GetType($"Threadlock.Entities.Characters.Player.BasicWeapons.{weaponName}");
            if (weaponType == null || !typeof(BasicWeapon).IsAssignableFrom(weaponType))
                yield break;

            var weaponInstance = Activator.CreateInstance(weaponType) as BasicWeapon;
            if (weaponInstance == null)
                yield break;

            Player.Instance.EquipNewWeapon(weaponInstance);

            Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds.Menu_select);
        }

        #endregion
    }
}
