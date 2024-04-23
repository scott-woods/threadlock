using Microsoft.Xna.Framework;
using Nez;
using Nez.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Helpers
{
    public class UIHelper
    {
        public static void MoveToWorldSpace(Vector2 worldPos, Element element)
        {
            var screenPos = Game1.Scene.Camera.WorldToScreenPoint(worldPos) / Game1.ResolutionManager.UIScale.ToVector2();
            element.SetPosition(screenPos.X, screenPos.Y);
        }

        public static Vector2 GetScreenPoint(Vector2 worldPos)
        {
            return Game1.Scene.Camera.WorldToScreenPoint(worldPos) / Game1.ResolutionManager.UIScale.ToVector2();
        }
    }
}
