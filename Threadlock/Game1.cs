using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Nez;
using SDL2;
using System.IO;
using Threadlock.GlobalManagers;
using Threadlock.Scenes;

namespace Threadlock
{
    public class Game1 : Core
    {
        public static ResolutionManager ResolutionManager { get; private set; } = new ResolutionManager();
        public static GameStateManager GameStateManager { get; private set; } = new GameStateManager();
        public static AudioManager AudioManager { get; private set; } = new AudioManager(GameStateManager);
        public static SceneManager SceneManager { get; private set; } = new SceneManager();
        public static UIManager UIManager { get; private set; } = new UIManager();

        public Game1() : base()
        {
            System.Environment.SetEnvironmentVariable("FNA_OPENGL_BACKBUGGER_SCALE_NEAREST", "1");
        }

        protected override void Initialize()
        {
            base.Initialize();

            //init data directory
            if (!Directory.Exists("Data")) Directory.CreateDirectory("Data");

            //global managers
            RegisterGlobalManager(ResolutionManager);
            RegisterGlobalManager(AudioManager);
            RegisterGlobalManager(SceneManager);
            RegisterGlobalManager(GameStateManager);
            RegisterGlobalManager(UIManager);

            //misc settings
            IsMouseVisible = false;
            ExitOnEscapeKeypress = false;

            //time step and refresh rate
            IsFixedTimeStep = true;
            SDL.SDL_GetCurrentDisplayMode(SDL.SDL_GetWindowDisplayIndex(Game1.Instance.Window.Handle), out var mode);
            var refreshRate = mode.refresh_rate == 0 ? 60 : mode.refresh_rate;
            TargetElapsedTime = System.TimeSpan.FromSeconds((double)1 / refreshRate);

            //graphics settings
            Graphics.Instance.Batcher.ShouldRoundDestinations = false;

            //physics config
            Physics.SpatialHashCellSize = 32;
            Physics.RaycastsStartInColliders = true;
            Physics.RaycastsHitTriggers = true;
            Physics.Gravity = new Vector2(0, 800f);

            //resolution settings
            Scene.UIRenderTargetSize = new Point(ResolutionManager.UIResolution.X, ResolutionManager.UIResolution.Y);
            Scene.SetDefaultDesignResolution(ResolutionManager.DesignResolutionWithBleed.X, ResolutionManager.DesignResolutionWithBleed.Y, Scene.SceneResolutionPolicy.LinearBleed, 4, 4);
            Screen.SetSize(1920, 1080);
            Screen.ApplyChanges();

            Scene = new EnemyTestZone();
        }
    }
}