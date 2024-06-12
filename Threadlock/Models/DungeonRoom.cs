using Microsoft.Xna.Framework;
using Nez;
using Nez.Tiled;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Helpers;

namespace Threadlock.Models
{
    public class DungeonRoom
    {
        public int Id;
        public string RoomType;
        public DungeonComposite2 ParentComposite;
        public Vector2 Position;
        TmxMap _map;
        public TmxMap Map { get => _map; set => SetMap(value); }
        public List<CorridorTile> CorridorTiles = new List<CorridorTile>();
        public List<DoorwayPoint> Doorways = new List<DoorwayPoint>();
        public RectangleF CollisionBounds
        {
            get
            {
                return TiledHelper.GetCollisionBounds(Map, Position);
            }
        }
        public List<DungeonRoom> Children = new List<DungeonRoom>();
        public List<DungeonRoom> ChildrenOutsideComposite { get => Children.Where(c => !ParentComposite.DungeonRooms.Contains(c)).ToList(); }

        public DungeonRoom(DungeonNode node)
        {
            Id = node.Id;
            RoomType = node.Type;
        }

        public void Reset()
        {
            Map = null;
            Position = Vector2.Zero;
            CorridorTiles.Clear();
            Doorways.Clear();
        }

        void SetMap(TmxMap map)
        {
            _map = map;

            Doorways.Clear();

            if (map != null)
            {
                var doorwayTmxObjects = _map.ObjectGroups?.SelectMany(g => g.Objects).Where(o => o.Type == "DoorwayPoint");
                foreach (var doorwayTmxObj in doorwayTmxObjects)
                {
                    var pos = new Vector2(doorwayTmxObj.X, doorwayTmxObj.Y);
                    var dir = Vector2.Zero;
                    if (doorwayTmxObj.Properties != null && doorwayTmxObj.Properties.TryGetValue("Direction", out var dirString))
                        DirectionHelper.StringDirectionDictionary.TryGetValue(dirString, out dir);
                    var doorwayPoint = new DoorwayPoint(this, dir, pos);
                    Doorways.Add(doorwayPoint);
                }
            }
        }
    }
}
