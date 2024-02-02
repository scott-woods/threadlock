using Microsoft.Xna.Framework;
using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.GlobalManagers
{
    public class ResolutionManager : GlobalManager
    {
        public Point TrueDesignResolution = new Point(480, 270);
        public Point DesignResolution = new Point(1920, 1080);
        public Point BleedArea = new Point(4, 4);
        public Point DesignResolutionWithBleed { get => DesignResolution + BleedArea; }
        public Point ResolutionScale { get => DesignResolution / TrueDesignResolution; }

        public Point UIResolution = new Point(480, 270);

        public List<Vector2> ScreenSizes = new List<Vector2>()
        {
            new Vector2(480, 270),
            new Vector2(640, 360),
            new Vector2(854, 480),
            new Vector2(1280, 720),
            new Vector2(1920, 1080),
            new Vector2(2560, 1440),
            new Vector2(3840, 2160)
        };

        public override void Update()
        {
            base.Update();

            if (Input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.F1) && !Screen.IsFullscreen)
            {
                var current = ScreenSizes.FindIndex(s => s == Screen.Size);
                if (current > 0)
                {
                    var newSize = ScreenSizes[current - 1];
                    Screen.SetSize((int)newSize.X, (int)newSize.Y);
                }

                Screen.ApplyChanges();
            }
            else if (Input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.F2) && !Screen.IsFullscreen)
            {
                var current = ScreenSizes.FindIndex(s => s == Screen.Size);
                if (current != -1 && current < ScreenSizes.Count - 1)
                {
                    var newSize = ScreenSizes[current + 1];
                    Screen.SetSize((int)newSize.X, (int)newSize.Y);
                }

                Screen.ApplyChanges();
            }
            if (Input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.F4))
            {
                Screen.IsFullscreen = !Screen.IsFullscreen;
                Screen.ApplyChanges();
            }
        }
    }
}
