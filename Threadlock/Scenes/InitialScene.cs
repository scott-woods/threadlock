using Nez;
using System.Linq;
using Threadlock.Components;
using Threadlock.Helpers;
using Threadlock.SceneComponents;
using Threadlock.StaticData;
using Threadlock.UI.Canvases;

namespace Threadlock.Scenes
{
    public class InitialScene : BaseScene
    {
        public override void OnStart()
        {
            base.OnStart();

            var mapEntity = CreateEntity("map");
            var map = Content.LoadTiledMap(Nez.Content.Tiled.Tilemaps.Test);
            var mapRenderer = mapEntity.AddComponent(new TiledMapRenderer(map, "Walls"));
            mapRenderer.SetLayersToRender(new[] { "Back", "Walls" }.Where(l => map.Layers.Contains(l)).ToArray());
            mapRenderer.RenderLayer = RenderLayers.Back;
            Flags.SetFlagExclusive(ref mapRenderer.PhysicsLayer, PhysicsLayers.Environment);

            TiledHelper.CreateEntitiesForTiledObjects(mapRenderer);

            var frontMapRenderer = mapEntity.AddComponent(new TiledMapRenderer(map));
            frontMapRenderer.SetLayersToRender(new[] { "Front", "AboveFront" }.Where(l => map.Layers.Contains(l)).ToArray());
            frontMapRenderer.RenderLayer = RenderLayers.Front;

            var ui = CreateEntity("ui").AddComponent(new CombatUI());

            var playerSpawner = AddSceneComponent(new PlayerSpawner());
            var player = playerSpawner.SpawnPlayer();

            var followCam = Camera.AddComponent(new CustomFollowCamera(player));

            AddSceneComponent(new GridGraphManager());
        }
    }
}
