using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Models
{
    public class PositionInfo
    {
        public Vector2 Position { get; set; }
        public bool HasLineOfSight { get; set; }

        public PositionInfo(Vector2 position, bool hasLineOfSight)
        {
            Position = position;
            HasLineOfSight = hasLineOfSight;
        }
    }
}
