using Microsoft.Xna.Framework;
using Nez;
using Nez.AI.Pathfinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
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
                    if (room.Bounds.Right > bottomRight.X)
                        bottomRight.X = room.Bounds.Right;
                    if (room.Bounds.Bottom > bottomRight.Y)
                        bottomRight.Y = room.Bounds.Bottom;
                }

                return new RectangleF(topLeft, bottomRight - topLeft);
            }
        }
        public List<SingleTileRenderer> SingleTileRenderers = new List<SingleTileRenderer>();
        public List<Vector2> FloorTilePositions = new List<Vector2>();

        public DungeonComposite(List<DungeonNode> roomNodes, DungeonCompositeType compositeType)
        {
            foreach (var node in roomNodes)
            {
                var roomEntity = Game1.Scene.AddEntity(new DungeonRoomEntity(this, node));
                RoomEntities.Add(roomEntity);
            }

            CompositeType = compositeType;
        }

        public List<DungeonRoomEntity> GetRoomsFromChildrenComposites()
        {
            var allRooms = new List<DungeonRoomEntity>();
            AddAllChildrenRecursive(allRooms);
            return allRooms;
        }

        void AddAllChildrenRecursive(List<DungeonRoomEntity> allRooms)
        {
            allRooms.AddRange(RoomEntities);
            foreach (var room in RoomEntities.Where(r => r.ChildrenOutsideComposite != null && r.ChildrenOutsideComposite.Count > 0))
            {
                foreach (var child in room.ChildrenOutsideComposite)
                {
                    child.ParentComposite.AddAllChildrenRecursive(allRooms);
                }
            }
        }

        /// <summary>
        /// returns a pathfinding graph that is the size of the entire composite, with walls for all non-null tiles
        /// </summary>
        /// <returns></returns>
        public WeightedGridGraph GetPathfindingGraph()
        {
            //var graph = new AstarGridGraph((int)Bounds.Width / 16, (int)Bounds.Height / 16);
            var graph = new WeightedGridGraph((int)Bounds.Right / 16, (int)Bounds.Bottom / 16);

            foreach (var map in RoomEntities)
            {
                if (map.TryGetComponent<TiledMapRenderer>(out var renderer))
                {
                    foreach (var layer in renderer.TiledMap.TileLayers)
                    {
                        foreach (var tile in layer.Tiles.Where(t => t != null))
                        {
                            var tilePos = new Vector2(tile.X, tile.Y);
                            var adjustedTilePos = tilePos + (map.Position / 16);

                            var tilePoint = adjustedTilePos.ToPoint();
                            if (!graph.Walls.Contains(tilePoint))
                                graph.Walls.Add(tilePoint);
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
                                var tilePos = new Vector2(x, y);
                                var adjustedTilePos = tilePos + (doorway.Entity.Position / 16);

                                var tilePoint = adjustedTilePos.ToPoint();
                                if (!graph.Walls.Contains(tilePoint))
                                    graph.Walls.Add(tilePoint);
                            }
                        }
                    }
                }
            }

            return graph;
        }

        public void MoveRooms(Vector2 movement, bool moveChildComposites = true)
        {
            foreach (var tileRenderer in SingleTileRenderers)
            {
                tileRenderer.Entity.Position += movement;
            }

            for (int i = 0; i < FloorTilePositions.Count; i++)
                FloorTilePositions[i] += movement;

            foreach (var room in RoomEntities)
            {
                room.Position += movement;
                if (moveChildComposites)
                {
                    if (room.ChildrenOutsideComposite != null && room.ChildrenOutsideComposite.Count > 0)
                    {
                        foreach (var child in room.ChildrenOutsideComposite)
                            child.ParentComposite.MoveRooms(movement);
                    }
                }
            }
        }

        public void AdjustForPathfinding(int numberOfTiles)
        {
            var desiredPos = Vector2.Zero + (new Vector2(1, 1) * 16 * numberOfTiles);
            var amountToMove = desiredPos - Bounds.Location;
            MoveRooms(amountToMove, false);
        }
    }

    public enum DungeonCompositeType
    {
        Tree,
        Loop
    }
}
