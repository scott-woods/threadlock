using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Models
{
    public class SingleDungeonTile
    {
        public Vector2 Position { get; set; }
        public int TileId { get; set; }
        public bool IsCollider { get; set; } = false;

        public SingleDungeonTile(Vector2 position, int tileId, bool isCollider = false)
        {
            Position = position;
            TileId = tileId;
            IsCollider = isCollider;
        }
    }
}
