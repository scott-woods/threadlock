using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Models
{
    public class TileData
    {
        public int TileId = -1;
        public Type EnumType;
        public Vector2 Position;
        public int Mask;

        public TileData(Type enumType, Vector2 position)
        {
            EnumType = enumType;
            Position = position;
        }
    }
}
