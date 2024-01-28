using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.StaticData
{
    public static class RenderLayers
    {
        public const int DefaultMapLayer = 1;
        public const int Front = -10;
        public const int ScreenSpaceRenderLayer = -10000;
        public const int Cursor = -20000;
    }
}
