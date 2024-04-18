using Microsoft.Xna.Framework;
using Nez;
using Nez.Tiled;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Threadlock.Components;
using Threadlock.Components.TiledComponents;
using Threadlock.Helpers;
using Threadlock.SceneComponents;
using Threadlock.SceneComponents.Dungenerator;
using Threadlock.StaticData;
using Threadlock.UI.Canvases;

namespace Threadlock.Scenes
{
    public class ForgeDungeon : BaseScene
    {
        public override Color SceneColor => new Color(41, 16, 19);
        Dungenerator _dungenerator;
        PlayerSpawner _playerSpawner;

        public override void Initialize()
        {
            base.Initialize();

            CreateEntity("ui").AddComponent(new CombatUI());

            _dungenerator = AddSceneComponent(new Dungenerator());
            _playerSpawner = AddSceneComponent(new PlayerSpawner());
        }

        public override void OnStart()
        {
            base.OnStart();

            //_generator.Generate();
            _dungenerator.Generate();

            Game1.AudioManager.PlayMusic(Nez.Content.Audio.Music.Meltingidols);

            var player = _playerSpawner.SpawnPlayer();

            Camera.Entity.AddComponent(new CustomFollowCamera(player));
        }
    }
}
