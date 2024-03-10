using Nez.AI.Pathfinding;
using Nez.Tiled;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock
{
    public class WorldSpaceAstarGridGraph : AstarGridGraph
    {
        public WorldSpaceAstarGridGraph(TmxLayer tiledLayer) : base(tiledLayer)
        {
        }

        public WorldSpaceAstarGridGraph(int width, int height) : base(width, height)
        {
        }
    }
}
