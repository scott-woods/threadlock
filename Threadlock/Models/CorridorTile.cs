using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Models
{
    public class CorridorTile
    {
        DungeonRoom _parentRoom;
        Vector2 _localPosition;

        public Vector2 Position { get => _parentRoom.Position + _localPosition; }

        public CorridorTile(DungeonRoom parentRoom, Vector2 localPosition)
        {
            _parentRoom = parentRoom;
            _localPosition = localPosition;
        }
    }
}
