using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Models
{
    public class DoorwayPoint
    {
        public bool HasConnection;
        public Vector2 Direction;
        public Vector2 Position { get => ParentRoom.Position + _localPosition; }
        public DungeonRoom ParentRoom { get; }

        Vector2 _localPosition;

        public DoorwayPoint(DungeonRoom parentRoom, Vector2 direction, Vector2 localPosition)
        {
            ParentRoom = parentRoom;
            Direction = direction;
            _localPosition = localPosition;
        }
    }
}
