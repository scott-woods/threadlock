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
using Threadlock.Models;

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
            { "Dialogue", Dialogue },
            { "ActionShop", ActionShop }
        };

        static bool VerifyArgs(List<string> args, int requiredCount)
        {
            return args != null && args.Count >= requiredCount;
        }

        #region EVENTS

        public static IEnumerator ActionShop(Trigger trigger)
        {
            var dialogueSet = DialogueLoader.GetDialogue("TestDialogue", "ActionShop");

            if (dialogueSet == null)
                yield break;

            yield return Game1.UIManager.ShowTextboxText(dialogueSet);
        }

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
                        if (Areas.AreaDictionary.TryGetValue(areaString, out var area))
                        {
                            var encounterManager = new EncounterManager(() =>
                            {
                                foreach (var doorway in doorways)
                                    doorway.SetGateOpen(true);
                            });

                            int i = 0;
                            Dictionary<EnemyConfig, int> pickedConfigs = new Dictionary<EnemyConfig, int>();
                            var possibleConfigs = new List<EnemyConfig>();
                            foreach (var enemyName in area.EnemyTypes)
                            {
                                if (Enemies.EnemyConfigDictionary.TryGetValue(enemyName, out var enemyConfig))
                                    possibleConfigs.Add(enemyConfig);
                            }
                            while (i < enemySpawns.Count)
                            {
                                var spawn = enemySpawns[i];

                                Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds.Enemy_spawn);

                                EnemyConfig enemyConfig = possibleConfigs.RandomItem();

                                //add this type to typesPicked list
                                if (!pickedConfigs.ContainsKey(enemyConfig))
                                    pickedConfigs.Add(enemyConfig, 0);
                                pickedConfigs[enemyConfig]++;

                                //spawn enemy
                                //spawn.SpawnEnemy(typeof(ChainBot));
                                var enemy = spawn.SpawnEnemy(enemyConfig);
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

            bool stopMusic = true;
            if (trigger.Args.Count >= 3)
            {
                var stopMusicString = trigger.Args[2];
                stopMusic = Convert.ToBoolean(stopMusicString);
            }

            Game1.SceneManager.ChangeScene(targetSceneType, targetSpawn, stopMusic);

            trigger.Entity.Destroy();

            yield break;
        }

        public static IEnumerator SpawnTestEnemy(Trigger trigger)
        {
            if (!VerifyArgs(trigger.Args, 1))
                yield break;

            var spawns = trigger.FindComponentsOnMap<EnemySpawnPoint>();
            if (spawns != null && spawns.Count > 0)
                spawns.First().SpawnEnemy();
        }

        public static IEnumerator ChangeWeapon(Trigger trigger)
        {
            if (!VerifyArgs(trigger.Args, 1))
                yield break;

            var weaponName = trigger.Args[0];

            if (Game1.Scene.FindEntity("Player") is Player player && player.TryGetComponent<WeaponManager>(out var weaponManager))
                weaponManager.SetWeapon(weaponName);

            Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds.Menu_select);
        }

        #endregion
    }
}
