using Microsoft.Xna.Framework;
using Nez.Tiled;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Helpers;

namespace Threadlock.Models
{
    public class DoorwayPoint
    {
        public bool HasConnection;
        public Vector2 Direction;
        public Vector2 Position { get => ParentRoom != null ? ParentRoom.Position + _localPosition : _localPosition; }
        public DungeonRoom ParentRoom { get; }

        Vector2 _localPosition;

        public DoorwayPoint(DungeonRoom parentRoom, Vector2 direction, Vector2 localPosition)
        {
            ParentRoom = parentRoom;
            Direction = direction;
            _localPosition = localPosition;
        }

        public DoorwayPoint(DungeonRoom parentRoom, TmxObject tmxObject)
        {
            ParentRoom = parentRoom;
            _localPosition = new Vector2(tmxObject.X, tmxObject.Y);
            if (tmxObject.Properties != null)
            {
                if (tmxObject.Properties.TryGetValue("Direction", out var dirString))
                    DirectionHelper.StringDirectionDictionary.TryGetValue(dirString, out Direction);
            }
        }
    }
}
