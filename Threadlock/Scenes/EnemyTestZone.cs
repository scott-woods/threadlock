using Microsoft.Xna.Framework;
using Nez;
using Nez.Tiled;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Entities;
using Threadlock.Helpers;
using Threadlock.SceneComponents;
using Threadlock.StaticData;
using Threadlock.UI.Canvases;

namespace Threadlock.Scenes
{
    public class EnemyTestZone : BaseScene
    {
        public override void OnStart()
        {
            base.OnStart();

            var mapEntity = CreateEntity("map");
            
            var map = Content.LoadTiledMap(Nez.Content.Tiled.Tilemaps.Enemy_test_zone);
            TiledHelper.SetupMap(mapEntity, map);

            UI.AddComponent(new CombatUI());

            var playerSpawner = AddSceneComponent(new PlayerSpawner());
            var player = playerSpawner.SpawnPlayer();

            var followCam = Camera.AddComponent(new CustomFollowCamera(player));

            var crateCount = Nez.Random.Range(10, 30);
            for (int i = 0; i < crateCount; i++)
            {
                var crate = new Crate();
                crate.Position = new Microsoft.Xna.Framework.Vector2(Nez.Random.Range(0, map.WorldWidth), Nez.Random.Range(0, map.WorldHeight));
                AddEntity(crate);
            }

            //Game1.AudioManager.PlayMusic(Nez.Content.Audio.Music.Meltingidols);
        }
    }
}
