using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez;
using Nez.DeferredLighting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Components.TiledComponents;
using Threadlock.Entities;
using Threadlock.Entities.Characters.Enemies.OrbMage;
using Threadlock.Entities.Characters.Player;
using Threadlock.Helpers;
using Threadlock.SceneComponents;
using Threadlock.StaticData;
using Threadlock.UI.Canvases;

namespace Threadlock.Scenes
{
    public class Hub : BaseScene
    {
        public override Color SceneColor => new Color(41, 16, 19, 255);

        public override void OnStart()
        {
            base.OnStart();

            var mapEntity = CreateEntity("map");
            var map = Content.LoadTiledMap(Nez.Content.Tiled.Tilemaps.HubMaps.Hub);
            TiledHelper.SetupMap(mapEntity, map);
            TiledHelper.SetupLightingTiles(mapEntity, map);
            //var map = Content.LoadTiledMap(Nez.Content.Tiled.Tilemaps.HubMaps.Hub);
            //var mapEntity = CreateEntity("map");
            //var backAndWallsLayers = new[] { "Back", "Walls", "Entities" };
            //var mapRenderer = mapEntity.AddComponent(new TiledMapRenderer(map, "Walls"));
            //mapRenderer.SetLayersToRender(map.Layers
            //    .Where(l => backAndWallsLayers.Any(x => l.Name.StartsWith(x)))
            //    .Select(l => l.Name)
            //    .ToArray());
            //mapRenderer.RenderLayer = RenderLayers.Back;
            //Flags.SetFlagExclusive(ref mapRenderer.PhysicsLayer, PhysicsLayers.Environment);
            //TiledHelper.CreateEntitiesForTiledObjects(mapRenderer);

            //var frontLayers = new[] { "Front", "AboveFront" };
            //var frontRenderer = mapEntity.AddComponent(new TiledMapRenderer(map));
            //frontRenderer.SetLayersToRender(map.Layers
            //    .Where(l => frontLayers.Any(x => l.Name.StartsWith(x)))
            //    .Select(l => l.Name)
            //    .ToArray());
            //frontRenderer.RenderLayer = RenderLayers.Front;

            var ui = CreateEntity("ui").AddComponent(new CombatUI());

            var playerSpawner = AddSceneComponent(new PlayerSpawner());
            var player = playerSpawner.SpawnPlayer();

            var followCam = Camera.AddComponent(new CustomFollowCamera(player));

            Game1.AudioManager.PlayMusic(Nez.Content.Audio.Music.The_bay);

            //var enemySpawns = FindComponentsOfType<EnemySpawnPoint>();
            //enemySpawns.First().SpawnEnemy();
        }
    }
}
