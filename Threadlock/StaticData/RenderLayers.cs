using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.StaticData
{
    public static class RenderLayers
    {
        public const int Back = 3;
        public const int Shadow = 2;
        public const int Walls = 1;
        public const int YSort = 0;
        public const int Front = -10;
        public const int AboveFront = -20;
        public const int ScreenSpaceRenderLayer = -10000;
        public const int Cursor = -20000;

        public static int GetLayerValue(string layerName)
        {
            var field = typeof(RenderLayers).GetField(layerName);
            if (field != null && field.IsStatic && field.FieldType == typeof(int))
                return (int)field.GetValue(null);
            else
                throw new ArgumentException("Invalid layer name", nameof(layerName));
        }
    }
}
