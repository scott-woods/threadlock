using Microsoft.Xna.Framework;
using Nez.AI.Pathfinding;
using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components.TiledComponents;
using Threadlock.Entities;
using Threadlock.Helpers;
using Nez.Tiled;
using Threadlock.Models;
using Threadlock.StaticData;
using Threadlock.Components;
using static Threadlock.SceneComponents.Dungenerator.CorridorPainter;

namespace Threadlock.SceneComponents.Dungenerator
{
    public static class CorridorGenerator
    {
        static readonly int _minDistance = 4;
        static readonly int _maxDistance = 20;
        static readonly int _xPad = 2;
        static readonly int _yPad = 4;

        /// <summary>
        /// connect doorways, handling moving rooms
        /// </summary>
        /// <param name="startDoor"></param>
        /// <param name="endDoor"></param>
        /// <param name="roomsToCheck"></param>
        /// <param name="roomsToMove"></param>
        /// <param name="floorPositions"></param>
        /// <returns></returns>
        public static bool ConnectDoorways(DungeonDoorway startDoor, DungeonDoorway endDoor, List<DungeonRoomEntity> roomsToCheck, List<DungeonRoomEntity> roomsToMove, out List<Vector2> floorPositions)
        {
            floorPositions = new List<Vector2>();

            var startDir = startDoor.GetOutgingDirection();
            var endDir = endDoor.GetIncomingDirection();

            //first, handle direct path
            if (startDir == endDir)
            {
                //try to find a straight path
                int dist = 1;
                var startPos = startDoor.PathfindingOrigin;
                while (dist <= _maxDistance)
                {
                    var roomMovement = (startDoor.PathfindingOrigin + (startDir * 16 * dist)) - endDoor.PathfindingOrigin;
                    if (Dungenerator.ValidateRoomMovement(roomMovement, roomsToMove, roomsToCheck))
                    {
                        foreach (var room in roomsToMove)
                            room.MoveRoom(roomMovement);

                        List<Vector2> path = new List<Vector2>();

                        bool hitWall = false;
                        for (int i = 1; i < dist - 1; i++)
                        {
                            var pathPos = startDoor.PathfindingOrigin + (startDir * 16 * i);
                            var rectX = pathPos.X - (_xPad * 16);
                            var rectY = pathPos.Y - (_yPad * 16);
                            var rectWidth = (_xPad * 16 * 2) + 16;
                            var rectHeight = (_yPad * 16) + ((_yPad - 1) * 16);
                            var testRect = new RectangleF(rectX, rectY, rectWidth, rectHeight);

                            if (roomsToCheck.Where(r => r != startDoor.DungeonRoomEntity).Any(r => r.CollisionBounds.Intersects(testRect)))
                            {
                                hitWall = true;
                                break;
                            }

                            path.Add(pathPos);
                        }

                        if (hitWall)
                            break;

                        var reservedPositions = GetReservedPositions(startDoor, endDoor);

                        var largerPath = IncreaseCorridorWidth(path, reservedPositions);

                        floorPositions = largerPath.SelectMany(p => p.Value).ToList();

                        startDoor.SetOpen(true);
                        endDoor.SetOpen(true);

                        startDoor.DungeonRoomEntity.FloorTilePositions.AddRange(floorPositions);

                        return true;
                    }

                    dist++;
                }
            }

            //handle non-direct and non-identical path
            if (startDoor.Direction != endDoor.Direction)
            {
                int startDistance = _minDistance;
                int endDistance = _minDistance;

                List<List<Vector2>> possiblePaths = new List<List<Vector2>>();

                while (startDistance <= _maxDistance)
                {
                    List<Vector2> startPath = new List<Vector2>();

                    bool pathFailed = false;
                    for (int i = 1; i <= startDistance; i++)
                    {
                        var pathPos = startDoor.PathfindingOrigin + (startDir * 16 * i);
                        var rectX = pathPos.X - (_xPad * 16);
                        var rectY = pathPos.Y - (_yPad * 16);
                        var rectWidth = (_xPad * 16 * 2) + 16;
                        var rectHeight = (_yPad * 16) + ((_yPad - 1) * 16);
                        var testRect = new RectangleF(rectX, rectY, rectWidth, rectHeight);

                        if (roomsToCheck.Where(r => r != startDoor.DungeonRoomEntity).Any(r => r.CollisionBounds.Intersects(testRect)))
                        {
                            pathFailed = true;
                            break;
                        }

                        startPath.Add(pathPos);
                    }

                    if (pathFailed)
                        break;

                    var vertexPos = startDoor.PathfindingOrigin + (startDir * 16 * startDistance);

                    endDistance = _minDistance;
                    while (endDistance <= _maxDistance)
                    {
                        var targetPos = vertexPos + (endDir * 16 * endDistance);
                        var roomMovement = targetPos - endDoor.PathfindingOrigin;
                        if (!Dungenerator.ValidateRoomMovement(roomMovement, roomsToMove, roomsToCheck))
                        {
                            endDistance++;
                            continue;
                        }

                        List<Vector2> endPath = new List<Vector2>();

                        for (int i = 1; i < endDistance - 1; i++)
                        {
                            var pathPos = vertexPos + (endDir * 16 * i);
                            var rectX = pathPos.X - (_xPad * 16);
                            var rectY = pathPos.Y - (_yPad * 16);
                            var rectWidth = (_xPad * 16 * 2) + 16;
                            var rectHeight = (_yPad * 16) + ((_yPad - 1) * 16);
                            var testRect = new RectangleF(rectX, rectY, rectWidth, rectHeight);

                            if (roomsToCheck.Where(r => r != startDoor.DungeonRoomEntity).Any(r => r.CollisionBounds.Intersects(testRect)))
                            {
                                pathFailed = true;
                                break;
                            }

                            endPath.Add(pathPos);
                        }

                        if (pathFailed)
                            break;

                        possiblePaths.Add(startPath.Concat(endPath).ToList());

                        endDistance++;
                    }

                    startDistance++;
                }

                possiblePaths = possiblePaths
                    .OrderBy(p => p.Count)
                    .ToList();

                var reservedPositions = GetReservedPositions(startDoor, endDoor);

                while (possiblePaths.Count > 0)
                {
                    var path = possiblePaths.First();

                    var finalPos = path.Last();
                    var targetPos = finalPos + (endDir * 16 * 2);
                    var roomMovement = targetPos - endDoor.PathfindingOrigin;
                    foreach (var room in roomsToMove)
                        room.MoveRoom(roomMovement);

                    var largerPath = IncreaseCorridorWidth(path, reservedPositions);

                    floorPositions = largerPath.SelectMany(p => p.Value).ToList();

                    startDoor.SetOpen(true);
                    endDoor.SetOpen(true);

                    startDoor.DungeonRoomEntity.FloorTilePositions.AddRange(floorPositions);

                    return true;
                }
            }

            //if other methods have failed, try pathfinding in a radius around the start door
            var joinedRooms = roomsToMove.Concat(roomsToCheck).ToList();
            for (int x = -8; x <= 8; x++)
            {
                if (Math.Abs(x) < _minDistance)
                    continue;
                for (int y = -8; y <= 8; y++)
                {
                    if (Math.Abs(y) < _minDistance)
                        continue;

                    var dir = new Vector2(x * 16, y * 16);
                    var roomMovement = (startDoor.PathfindingOrigin + dir) - endDoor.PathfindingOrigin;
                    if (Dungenerator.ValidateRoomMovement(roomMovement, roomsToMove, roomsToCheck))
                    {
                        roomsToMove.ForEach(r => r.MoveRoom(roomMovement));
                        if (ConnectStaticDoorways(startDoor, endDoor, joinedRooms))
                            return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// connect doorways without moving rooms. RoomsToCheck should be all rooms involved. Assumes rooms aren't overlapping
        /// </summary>
        /// <param name="startDoor"></param>
        /// <param name="endDoor"></param>
        /// <param name="roomsToCheck"></param>
        /// <returns></returns>
        public static bool ConnectStaticDoorways(DungeonDoorway startDoor, DungeonDoorway endDoor, List<DungeonRoomEntity> roomsToCheck)
        {
            var rectX = new[] { startDoor.DungeonRoomEntity.Position.X, endDoor.DungeonRoomEntity.Position.X }.Min();
            var rectY = new[] { startDoor.DungeonRoomEntity.Position.Y, endDoor.DungeonRoomEntity.Position.Y }.Min();
            var maxX = new[] { startDoor.DungeonRoomEntity.Bounds.Right, endDoor.DungeonRoomEntity.Bounds.Right }.Max();
            var maxY = new[] { startDoor.DungeonRoomEntity.Bounds.Bottom, endDoor.DungeonRoomEntity.Bounds.Bottom }.Max();

            var topLeft = new Vector2((rectX / 16) - _minDistance, (rectY / 16) - _minDistance);
            var bottomRight = new Vector2((maxX / 16) + _minDistance, (maxY / 16) + _minDistance);
            var size = bottomRight - topLeft;
            var rect = new RectangleF(topLeft, size);

            var graph = new AstarGridGraph((int)size.X, (int)size.Y);
            var graphOffset = topLeft;

            var joinedRooms = new List<DungeonRoomEntity>(roomsToCheck);
            if (!joinedRooms.Contains(startDoor.DungeonRoomEntity))
                joinedRooms.Add(startDoor.DungeonRoomEntity);
            if (!joinedRooms.Contains(endDoor.DungeonRoomEntity))
                joinedRooms.Add(endDoor.DungeonRoomEntity);

            List<Point> wallPositions = new List<Point>();

            foreach (var room in joinedRooms)
            {
                //add walls from collision tiles
                var renderers = room.GetComponents<TiledMapRenderer>().Where(r => r.CollisionLayer != null);
                foreach (var renderer in renderers)
                {
                    var tiles = TiledHelper.GetLayerTilesWithPositions(renderer.CollisionLayer);
                    foreach (var tile in tiles)
                    {
                        var x = tile.Item1.X;
                        var y = tile.Item1.Y;
                        var pos = new Vector2(x, y);
                        pos += (renderer.Entity.Position / 16);
                        pos -= graphOffset;

                        if (!wallPositions.Contains(pos.ToPoint()))
                            wallPositions.Add(pos.ToPoint());
                    }
                }

                //add walls from doorways
                var doorways = room.FindComponentsOnMap<DungeonDoorway>();
                foreach (var doorway in doorways)
                {
                    for (int x = 0; x < doorway.TmxObject.Width / 16; x++)
                    {
                        for (int y = 0; y < doorway.TmxObject.Height / 16; y++)
                        {
                            var pos = new Vector2(x, y);
                            pos += (doorway.Entity.Position / 16);
                            pos -= graphOffset;
                            var point = pos.ToPoint();

                            if (!wallPositions.Contains(point))
                                wallPositions.Add(point);
                            //if (point.X >= 0 && point.Y >= 0 && !graph.Walls.Contains(point))
                            //    graph.Walls.Add(point);
                        }
                    }
                }
            }

            foreach (var wallPos in wallPositions)
            {
                for (int x = -_xPad; x <= _xPad; x++)
                {
                    for (int y = -_yPad; y <= _yPad; y++)
                    {
                        var offset = new Point(x, y);
                        var offsetPos = wallPos + offset;
                        if (offsetPos.X >= 0 && offsetPos.Y >= 0 && !graph.Walls.Contains(offsetPos))
                            graph.Walls.Add(offsetPos);
                    }
                }
            }

            var startDir = startDoor.GetOutgingDirection();
            var endDir = endDoor.GetOutgingDirection();
            for (int i = 0; i <= _minDistance; i++)
            {
                var adjustedStartPos = (startDoor.PathfindingOrigin / 16).ToPoint() - graphOffset.ToPoint();
                adjustedStartPos += (startDir * i).ToPoint();
                if (graph.Walls.Contains(adjustedStartPos))
                    graph.Walls.Remove(adjustedStartPos);

                var adjustedEndPos = (endDoor.PathfindingOrigin / 16).ToPoint() - graphOffset.ToPoint();
                adjustedEndPos += (endDir * i).ToPoint();
                if (graph.Walls.Contains(adjustedEndPos))
                    graph.Walls.Remove(adjustedEndPos);

            }

            //remove pathfinding origins from walls
            //var adjustedStartPos = (startDoor.PathfindingOrigin / 16).ToPoint() - graphOffset.ToPoint();
            //if (graph.Walls.Contains(adjustedStartPos))
            //    graph.Walls.Remove(adjustedStartPos);
            //var adjustedEndPos = (endDoor.PathfindingOrigin / 16).ToPoint() - graphOffset.ToPoint();
            //if (graph.Walls.Contains(adjustedEndPos))
            //    graph.Walls.Remove(adjustedEndPos);

            var path = graph.Search(((startDoor.PathfindingOrigin / 16) - graphOffset).ToPoint(), ((endDoor.PathfindingOrigin / 16) - graphOffset).ToPoint());

            if (path == null || path.Count > 50)
                return false;

            //get path in world space
            var adjustedPath = path.Select(p =>
            {
                var pos = new Vector2(p.X, p.Y) + graphOffset;
                return pos * 16;
            }).ToList();

            var reservedPositions = GetReservedPositions(startDoor, endDoor);

            //get larger hallway
            var largerPath = IncreaseCorridorWidth(adjustedPath, reservedPositions);

            var floorPositions = largerPath.SelectMany(p => p.Value).ToList();

            startDoor.SetOpen(true);
            endDoor.SetOpen(true);

            startDoor.DungeonRoomEntity.FloorTilePositions.AddRange(floorPositions);

            return true;
        }

        public static Dictionary<Vector2, List<Vector2>> IncreaseCorridorWidth(List<Vector2> positions, List<Vector2> reservedPositions)
        {
            Dictionary<Vector2, List<Vector2>> posDictionary = new Dictionary<Vector2, List<Vector2>>();
            List<Vector2> visitedPositions = new List<Vector2>();
            for (int i = 1; i < positions.Count + 1; i++)
            {
                posDictionary[positions[i - 1]] = new List<Vector2>();
                for (int x = -1; x < 2; x++)
                {
                    for (int y = -1; y < 2; y++)
                    {
                        var pos = new Vector2(x * 16, y * 16) + positions[i - 1];
                        if (!visitedPositions.Contains(pos) && !reservedPositions.Contains(pos))
                        {
                            posDictionary[positions[i - 1]].Add(pos);
                            visitedPositions.Add(pos);
                        }
                    }
                }
            }

            return posDictionary;
        }

        static List<Vector2> GetReservedPositions(DungeonDoorway startDoor, DungeonDoorway endDoor)
        {
            var reservedPositions = new List<Vector2>();
            reservedPositions.Add(startDoor.PathfindingOrigin);
            reservedPositions.Add(endDoor.PathfindingOrigin);

            switch (startDoor.Direction)
            {
                case "Top":
                case "Bottom":
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Left * 16));
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Right * 16));
                    break;
                case "Left":
                case "Right":
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Up * 16));
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Down * 16));
                    break;
            }
            switch (endDoor.Direction)
            {
                case "Top":
                case "Bottom":
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Left * 16));
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Right * 16));
                    break;
                case "Left":
                case "Right":
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Up * 16));
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Down * 16));
                    break;
            }

            return reservedPositions;
        }
    }
}
