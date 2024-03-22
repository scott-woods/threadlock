using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Threadlock.SceneComponents.Dungenerator.CorridorPainter;

namespace Threadlock.Models
{
    public class FloorTile
    {
        public Vector2 Position { get; set; }
        public TileDirection TileDirection { get; set; }
        public TileOrientation TileOrientation { get; set; }

        public FloorTile(Vector2 position, TileDirection tileDirection, TileOrientation tileOrientation)
        {
            Position = position;
            TileDirection = tileDirection;
            TileOrientation = tileOrientation;
        }
    }
}
