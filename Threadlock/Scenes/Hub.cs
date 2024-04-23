using Microsoft.Xna.Framework;
using Nez;
using Threadlock.Components;
using Threadlock.Helpers;
using Threadlock.SceneComponents;
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
            //var map = Content.LoadTiledMap(Nez.Content.Tiled.Tilemaps.Forge.Forge_action_store);
            TiledHelper.SetupMap(mapEntity, map);
            TiledHelper.SetupLightingTiles(mapEntity, map);

            var ui = CreateEntity("ui").AddComponent(new CombatUI());

            var playerSpawner = AddSceneComponent(new PlayerSpawner());
            var player = playerSpawner.SpawnPlayer();

            //var mapBounds = TiledHelper.GetActualBounds(mapEntity);
            //var followCam = Camera.AddComponent(new CustomFollowCamera(player, mapBounds.Location.ToVector2(), (mapBounds.Location + mapBounds.GetSize()).ToVector2()));
            var followCam = Camera.AddComponent(new CustomFollowCamera(player));

            Game1.AudioManager.PlayMusic(Nez.Content.Audio.Music.The_bay, true, 0, 1);
        }
    }
}
