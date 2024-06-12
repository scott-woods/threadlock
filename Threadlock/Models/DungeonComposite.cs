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
using Threadlock.StaticData;

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

        public DungeonComposite(List<DungeonNode> roomNodes, DungeonCompositeType compositeType)
        {
            foreach (var node in roomNodes)
            {
                var roomEntity = new DungeonRoomEntity(this, node);
                roomEntity.SetEnabled(false);
                roomEntity.SetComponentsOnMapEnabled(false);
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

        public void MoveRooms(Vector2 movement, bool moveChildComposites = true)
        {
            foreach (var room in RoomEntities)
                room.MoveRoom(movement, moveChildComposites);
        }

        public void AdjustForPathfinding(int numberOfTiles)
        {
            var desiredPos = Vector2.Zero + (new Vector2(1, 1) * 16 * numberOfTiles);
            var amountToMove = desiredPos - Bounds.Location;
            MoveRooms(amountToMove, false);
        }

        public void Reset()
        {
            //clear maps
            foreach (var map in RoomEntities)
                map.ClearMap();
        }
    }

    public enum DungeonCompositeType
    {
        Tree,
        Loop
    }
}
