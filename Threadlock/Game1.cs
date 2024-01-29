using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Nez;
using SDL2;
using Threadlock.GlobalManagers;
using Threadlock.Scenes;

namespace Threadlock
{
    public class Game1 : Core
    {
        public static ResolutionManager ResolutionManager = new ResolutionManager();

        public Game1() : base()
        {
            System.Environment.SetEnvironmentVariable("FNA_OPENGL_BACKBUGGER_SCALE_NEAREST", "1");
        }

        protected override void Initialize()
        {
            base.Initialize();

            //global managers
            RegisterGlobalManager(ResolutionManager);

            //global variables
            IsFixedTimeStep = true;
            SDL.SDL_GetCurrentDisplayMode(SDL.SDL_GetWindowDisplayIndex(Game1.Instance.Window.Handle), out var mode);
            var refreshRate = mode.refresh_rate == 0 ? 60 : mode.refresh_rate;
            TargetElapsedTime = System.TimeSpan.FromSeconds((double)1 / refreshRate);
            Graphics.Instance.Batcher.ShouldRoundDestinations = false;

            Scene.SetDefaultDesignResolution(ResolutionManager.DesignResolutionWithBleed.X, ResolutionManager.DesignResolutionWithBleed.Y, Scene.SceneResolutionPolicy.LinearBleed, 4, 4);
            Screen.SetSize(1920, 1080);
            Screen.ApplyChanges();

            Scene = new InitialScene();
        }
    }
}