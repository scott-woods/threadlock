using Microsoft.Xna.Framework;
using Nez;
using Nez.AI.Pathfinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components.TiledComponents;
using Threadlock.Entities;

namespace Threadlock.Models
{
    public class DungeonComposite
    {
        public List<DungeonRoomEntity> RoomEntities = new List<DungeonRoomEntity>();
        public DungeonCompositeType CompositeType;
        public RectangleF Bounds
        {
            get
            {
                Vector2 topLeft = new Vector2(float.MaxValue, float.MaxValue);
                Vector2 bottomRight = new Vector2(float.MinValue, float.MinValue);
                foreach (var room in RoomEntities)
                {
                    if (room.Position.X < topLeft.X)
                        topLeft.X = room.Position.X;
                    if (room.Position.Y < topLeft.Y)
                        topLeft.Y = room.Position.Y;
                    if (room.Position.X > bottomRight.X)
                        bottomRight.X = room.Position.X;
                    if (room.Position.Y > bottomRight.Y)
                        bottomRight.Y = room.Position.Y;
                }

                return new RectangleF(topLeft, bottomRight - topLeft);
            }
        }

        public DungeonComposite(List<DungeonNode> roomNodes, DungeonCompositeType compositeType)
        {
            foreach (var node in roomNodes)
            {
                var roomEntity = Game1.Scene.AddEntity(new DungeonRoomEntity(this, node));
                RoomEntities.Add(roomEntity);
            }

            CompositeType = compositeType;
        }

        /// <summary>
        /// returns a pathfinding graph that is the size of the entire composite, with walls for all non-null tiles
        /// </summary>
        /// <returns></returns>
        public AstarGridGraph GetPathfindingGraph()
        {
            var graph = new AstarGridGraph((int)Bounds.Width, (int)Bounds.Height);

            foreach (var map in RoomEntities)
            {
                if (map.TryGetComponent<TiledMapRenderer>(out var renderer))
                {
                    foreach (var layer in renderer.TiledMap.TileLayers)
                    {
                        foreach (var tile in layer.Tiles.Where(t => t != null))
                        {
                            //get tile position in world space
                            var tileWorldPos = new Vector2(map.Position.X + (tile.X * renderer.TiledMap.TileWidth), map.Position.Y + (tile.Y * renderer.TiledMap.TileHeight));

                            //adjusted position is the position relative to the top left of the composite
                            var adjustedPos = tileWorldPos - Bounds.Location;

                            //adjust for tile coords
                            adjustedPos /= new Vector2(renderer.TiledMap.TileWidth, renderer.TiledMap.TileHeight);

                            //add wall to graph
                            var wallPoint = adjustedPos.ToPoint();
                            if (!graph.Walls.Contains(wallPoint))
                                graph.Walls.Add(wallPoint);
                        }
                    }
                }

                //add walls on every tile of each dungeon doorway
                var doorways = map.FindComponentsOnMap<DungeonDoorway>();
                if (doorways != null)
                {
                    foreach (var doorway in doorways)
                    {
                        for (var y = 0; y < doorway.TmxObject.Height / 16; y++)
                        {
                            for (var x = 0; x < doorway.TmxObject.Width / 16; x++)
                            {
                                var tileWorldPos = new Vector2(doorway.Entity.Position.X + (x * 16), doorway.Entity.Position.Y + (y * 16));

                                //adjusted position is the position relative to the top left of the composite
                                var adjustedPos = tileWorldPos - Bounds.Location;

                                //adjust for tile coords
                                adjustedPos /= new Vector2(16, 16);

                                //add wall to graph
                                var wallPoint = adjustedPos.ToPoint();
                                if (!graph.Walls.Contains(wallPoint))
                                    graph.Walls.Add(wallPoint);
                            }
                        }
                    }
                }
            }

            return graph;
        }

        public void MoveRooms(Vector2 movement)
        {
            foreach (var room in RoomEntities)
                room.Position += movement;
        }
    }

    public enum DungeonCompositeType
    {
        Tree,
        Loop
    }
}
