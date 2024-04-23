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
using Threadlock.GlobalManagers;
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

            _dungenerator = AddSceneComponent(new Dungenerator());
            _playerSpawner = AddSceneComponent(new PlayerSpawner());

            Game1.SceneManager.Emitter.AddObserver(SceneManagerEvents.FadeInStarted, OnFadeInStarted);
        }

        public override void End()
        {
            base.End();

            Game1.SceneManager.Emitter.RemoveObserver(SceneManagerEvents.FadeInStarted, OnFadeInStarted);
        }

        public void GenerateDungeon()
        {
            _dungenerator.Generate();
        }

        public void FinalizeDungeon()
        {
            _dungenerator.FinalizeDungeon();

            var player = _playerSpawner.SpawnPlayer();

            Camera.Entity.AddComponent(new CustomFollowCamera(player));
        }

        void OnFadeInStarted()
        {
            Game1.AudioManager.PlayMusic(Nez.Content.Audio.Music.Meltingidols, true, 1.5f, 1f, false);
            Game1.AudioManager.PlayMusic(Nez.Content.Audio.Music.Shweet_sales, true, 0, 0f, false);
            CreateEntity("ui").AddComponent(new CombatUI());
        }
    }
}
