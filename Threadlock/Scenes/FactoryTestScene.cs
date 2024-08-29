using Nez;
using Nez.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Helpers;
using Threadlock.SaveData;
using Threadlock.SceneComponents;
using Threadlock.StaticData;
using Threadlock.UI.Canvases;

namespace Threadlock.Scenes
{
    public class FactoryTestScene : BaseScene
    {
        public override void OnStart()
        {
            base.OnStart();

            var mapEntity = CreateEntity("map");

            var map = Content.LoadTiledMap(Nez.Content.Tiled.Tilemaps.Forge.Forge_factory_test);
            TiledHelper.SetupMap(mapEntity, map);

            UI.AddComponent(new CombatUI());

            var playerSpawner = AddSceneComponent(new PlayerSpawner());
            var player = playerSpawner.SpawnPlayer();

            var followCam = Camera.AddComponent(new CustomFollowCamera(player));
        }
    }
}
