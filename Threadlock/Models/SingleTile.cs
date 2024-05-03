using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Models
{
    /// <summary>
    /// model to be used in a CorridorGenerator, contains a TileId and bool to determine if it should be a collider
    /// </summary>
    public class SingleTile
    {
        public int TileId;
        public bool IsCollider = false;

        public SingleTile(int tileId, bool isCollider = false)
        {
            TileId = tileId;
            IsCollider = isCollider;
        }
    }
}
