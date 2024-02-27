using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.SceneComponents;
using Threadlock.StaticData;
using Threadlock.UI.Canvases;

namespace Threadlock.Scenes
{
    public class ForgeDungeon : BaseScene
    {
        BSPDungenerator _generator;
        Dungenerator _dungenerator;
        PlayerSpawner _playerSpawner;

        public override void Initialize()
        {
            base.Initialize();

            CreateEntity("ui").AddComponent(new CombatUI());

            _generator = AddSceneComponent(new BSPDungenerator());
            _dungenerator = AddSceneComponent(new Dungenerator());
            _playerSpawner = AddSceneComponent(new PlayerSpawner());
        }

        public override void Begin()
        {
            base.Begin();

            //_generator.Generate();
            _dungenerator.Generate();

            //var map = DoorwayMaps.ForgeRightOpen.TmxMap;
            //var mapRenderer = CreateEntity("map").AddComponent(new TiledMapRenderer(map, "Walls"));
            //mapRenderer.SetLayersToRender("Back", "Walls");
            //Flags.SetFlagExclusive(ref mapRenderer.PhysicsLayer, PhysicsLayers.Environment);

            var player = _playerSpawner.SpawnPlayer();

            Camera.Entity.AddComponent(new CustomFollowCamera(player));
        }
    }
}
