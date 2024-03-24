using Nez.Tiled;
using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.StaticData;
using Microsoft.Xna.Framework;
using System.Collections;

namespace Threadlock.Components.TiledComponents
{
    public class Trigger : TiledComponent, IUpdatable
    {
        public Collider Collider { get; private set; }
        public TriggerType TriggerType { get; private set; } = TriggerType.None;
        public Func<Trigger, IEnumerator> Handler { get; private set; }
        public List<string> Args { get; private set; } = new List<string>();

        public override void Initialize()
        {
            base.Initialize();

            switch (TmxObject.ObjectType)
            {
                case TmxObjectType.Basic:
                case TmxObjectType.Tile:
                    Collider = Entity.AddComponent(new BoxCollider(TmxObject.Width, TmxObject.Height));
                    Collider.SetLocalOffset(new Vector2(TmxObject.Width / 2, TmxObject.Height / 2));
                    break;
                case TmxObjectType.Ellipse:
                    Collider = Entity.AddComponent(new CircleCollider(TmxObject.Width));
                    break;
                case TmxObjectType.Polygon:
                    Collider = Entity.AddComponent(new PolygonCollider(TmxObject.Points));
                    var width = TmxObject.Points.Select(p => p.X).Max() - TmxObject.Points.Select(p => p.X).Min();
                    var height = TmxObject.Points.Select(p => p.Y).Max() - TmxObject.Points.Select(p => p.Y).Min();
                    Collider.SetLocalOffset(new Vector2(width / 2, height / 2));
                    break;
            }

            Collider.IsTrigger = true;
            Flags.SetFlagExclusive(ref Collider.PhysicsLayer, PhysicsLayers.Trigger);
            Flags.SetFlagExclusive(ref Collider.CollidesWithLayers, PhysicsLayers.PlayerCollider);

            if (TmxObject.Properties != null && TmxObject.Properties.Count > 0)
            {
                //get trigger type
                if (TmxObject.Properties.TryGetValue("Type", out string type))
                {
                    if (Enum.TryParse(type, out TriggerType value))
                        TriggerType = value;
                }

                //try to get event handler
                if (TmxObject.Properties.TryGetValue("EventName", out string eventName))
                    if (_eventsMap.TryGetValue(eventName, out var handler))
                        Handler = handler;

                //get args
                if (TmxObject.Properties.TryGetValue("Args", out string args))
                    Args = args.Split(' ').ToList();
            }
        }

        public void Update()
        {
            if (TriggerType == TriggerType.Area && Collider.CollidesWithAny(out CollisionResult result))
                Game1.StartCoroutine(HandleTriggered());
        }

        public IEnumerator HandleTriggered()
        {
            //if handler is null, there's nothing to trigger. break here
            if (Handler == null)
                yield break;

            //disable during event so it isn't triggered again
            SetEnabled(false);

            //call handler
            yield return Handler(this);

            //check just in case entity was destroyed during event
            if (Entity != null)
                SetEnabled(true);
        }

        #region EVENTS

        static readonly Dictionary<string, Func<Trigger, IEnumerator>> _eventsMap = new Dictionary<string, Func<Trigger, IEnumerator>>()
        {
            { "DungeonEncounter", DungeonEncounter },
            { "ExitArea", ExitArea },
            { "SpawnTestEnemy", SpawnTestEnemy }
        };

        static IEnumerator DungeonEncounter(Trigger trigger)
        {
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
            trigger.Entity.Destroy();
        }

        static IEnumerator ExitArea(Trigger trigger)
        {
            if (!VerifyArgs(trigger.Args, 2))
                yield break;

            var targetSceneType = Type.GetType("Threadlock.Scenes." + trigger.Args[0]);
            if (targetSceneType == null)
                yield break;
            var targetSpawn = trigger.Args[1];

            Game1.SceneManager.ChangeScene(targetSceneType, targetSpawn);

            trigger.Entity.Destroy();

            yield break;
        }

        static IEnumerator SpawnTestEnemy(Trigger trigger)
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

        #endregion

        static bool VerifyArgs(List<string> args, int requiredCount)
        {
            return args != null && args.Count >= requiredCount;
        }
    }

    public enum TriggerType
    {
        None,
        Area,
        Interact
    }
}
