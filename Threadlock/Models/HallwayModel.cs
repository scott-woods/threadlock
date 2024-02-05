using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Models
{
    public class HallwayModel
    {
        public Map Map;
        public Point PathPoint;
        public bool IsCorner = true;

        public HallwayModel(Map map, Point pathPoint, bool isCorner = true)
        {
            Map = map;
            PathPoint = pathPoint;
            IsCorner = isCorner;
        }
    }
}
