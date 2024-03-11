using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Models
{
    public class SingleTile
    {
        public Vector2 Position;
        public int TileId;

        public SingleTile(Vector2 position, int tileId)
        {
            Position = position;
            TileId = tileId;
        }
    }
}
