using Microsoft.Xna.Framework;
using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Entities.Characters.Player;
using Threadlock.Helpers;
using Threadlock.SceneComponents;
using Threadlock.StaticData;
using Threadlock.UI.Canvases;

namespace Threadlock.Scenes
{
    public class Hub : BaseScene
    {
        public override void Initialize()
        {
            base.Initialize();

            var mapEntity = CreateEntity("map");
            var map = Content.LoadTiledMap(Nez.Content.Tiled.Tilemaps.HubMaps.Hub);
            var mapRenderer = mapEntity.AddComponent(new TiledMapRenderer(map, "Walls"));
            mapRenderer.SetLayersToRender(new[] { "Back", "Walls" }.Where(l => map.Layers.Contains(l)).ToArray());
            mapRenderer.RenderLayer = RenderLayers.DefaultMapLayer;
            Flags.SetFlagExclusive(ref mapRenderer.PhysicsLayer, PhysicsLayers.None);
            TiledHelper.CreateEntitiesForTiledObjects(mapRenderer);

            var frontMapRenderer = mapEntity.AddComponent(new TiledMapRenderer(map));
            frontMapRenderer.SetLayersToRender(new[] { "Front", "AboveFront" }.Where(l => map.Layers.Contains(l)).ToArray());
            frontMapRenderer.RenderLayer = RenderLayers.Front;

            var ui = CreateEntity("ui").AddComponent(new CombatUI());

            var playerSpawner = AddSceneComponent(new PlayerSpawner());
            var player = playerSpawner.SpawnPlayer();

            //var chainBot = AddEntity(new ChainBot());

            var followCam = Camera.AddComponent(new CustomFollowCamera(player));
        }
    }
}
