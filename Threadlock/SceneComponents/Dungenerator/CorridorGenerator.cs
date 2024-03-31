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

                        for (int i = 1; i < dist - 1; i++)
                        {
                            var pathPos = startDoor.PathfindingOrigin + (startDir * 16 * i);
                            var rectX = pathPos.X - (_xPad * 16);
                            var rectY = pathPos.Y - (_yPad * 16);
                            var rectWidth = (_xPad * 16 * 2) + 16;
                            var rectHeight = (_yPad * 16) + ((_yPad - 1) * 16);
                            var testRect = new RectangleF(rectX, rectY, rectWidth, rectHeight);

                            if (roomsToCheck.Where(r => r != startDoor.DungeonRoomEntity).Any(r => r.CollisionBounds.Intersects(testRect)))
                                return false;

                            path.Add(pathPos);
                        }

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

                return false;
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

            return false;
        }

        public static bool ConnectStaticDoorways(DungeonDoorway startDoor, DungeonDoorway endDoor, List<DungeonRoomEntity> roomsToCheck)
        {
            var rectX = new[] { startDoor.DungeonRoomEntity.Position.X, endDoor.DungeonRoomEntity.Position.X }.Min();
            var rectY = new[] { startDoor.DungeonRoomEntity.Position.Y, endDoor.DungeonRoomEntity.Position.Y }.Min();
            var maxX = new[] { startDoor.DungeonRoomEntity.Bounds.Right, endDoor.DungeonRoomEntity.Bounds.Right }.Max();
            var maxY = new[] { startDoor.DungeonRoomEntity.Bounds.Bottom, endDoor.DungeonRoomEntity.Bounds.Bottom }.Max();

            var topLeft = new Vector2(rectX / 16, rectY / 16);
            var bottomRight = new Vector2(maxX / 16, maxY / 16);
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
                var renderer = room.GetComponents<TiledMapRenderer>().FirstOrDefault(r => r.CollisionLayer != null);
                if (renderer != null)
                {
                    var tiles = renderer.CollisionLayer.Tiles.Where(t => t != null)
                    .Select(t =>
                    {
                        var x = t.X;
                        var y = t.Y;
                        var pos = new Vector2(x, y);
                        pos += (renderer.Entity.Position / 16);
                        pos -= graphOffset;
                        return pos.ToPoint();
                    });
                    foreach (var tile in tiles)
                    {
                        if (!wallPositions.Contains(tile))
                            wallPositions.Add(tile);
                        ////only add walls that are within bounds and haven't already been added
                        //if (tile.X >= 0 && tile.Y >= 0 && !graph.Walls.Contains(tile))
                        //    graph.Walls.Add(tile);
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

            if (path == null)
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

        public static bool ConnectDoorways(DungeonDoorway startDoor, DungeonDoorway endDoor, List<DungeonRoomEntity> roomsToCheck, out List<Vector2> floorPositions)
        {
            floorPositions = new List<Vector2>();

            var startDir = startDoor.GetOutgingDirection();
            var endDir = endDoor.GetIncomingDirection();

            var offset = 2;

            Vector2 currentPos = startDoor.PathfindingOrigin + (startDir * 16 * offset);
            List<Vector2> path = new List<Vector2>() { currentPos };

            var endPos = endDoor.PathfindingOrigin - (endDir * 16 * offset);

            if (!startDoor.IsDirectMatch(endDoor))
            {
                var vertexPos = startDir.X != 0 ? new Vector2(endDoor.PathfindingOrigin.X, startDoor.PathfindingOrigin.Y)
                : new Vector2(startDoor.PathfindingOrigin.X, endDoor.PathfindingOrigin.Y);

                while (currentPos != vertexPos)
                {
                    currentPos += (startDir * 16);
                    path.Add(currentPos);
                }
            }
            
            while (currentPos != endPos)
            {
                currentPos += (endDir * 16);
                path.Add(currentPos);
            }

            if (roomsToCheck.Any(r => r.OverlapsRoom(path, out var overlaps, false)))
                return false;

            //reserved positions are extra positions outside the normal path that we still want to consider for the direction mask
            List<Vector2> reservedPositions = new List<Vector2>();
            reservedPositions.Add(startDoor.PathfindingOrigin);
            reservedPositions.Add(endDoor.PathfindingOrigin);
            switch (startDoor.Direction)
            {
                case "Top":
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Left * 16));
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Right * 16));
                    break;
                case "Bottom":
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Left * 16));
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Right * 16));
                    break;
                case "Left":
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Up * 16));
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Down * 16));
                    break;
                case "Right":
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Up * 16));
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Down * 16));
                    break;
            }
            switch (endDoor.Direction)
            {
                case "Top":
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Left * 16));
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Right * 16));
                    break;
                case "Bottom":
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Left * 16));
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Right * 16));
                    break;
                case "Left":
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Up * 16));
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Down * 16));
                    break;
                case "Right":
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Up * 16));
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Down * 16));
                    break;
            }

            //get larger hallway
            var largerPath = IncreaseCorridorWidth(path, reservedPositions);

            floorPositions = largerPath.SelectMany(p => p.Value).ToList();
            return true;
        }

        public static bool ConnectDoorways(DungeonDoorway startDoor, DungeonDoorway endDoor, WeightedGridGraph graph, List<DungeonRoomEntity> roomsToCheck, RectangleF graphRect, out List<Vector2> floorPositions)
        {
            //init floor positions list
            floorPositions = new List<Vector2>();

            //if this is a perfect pair, see if it's already aligned correctly
            if (startDoor.IsDirectMatch(endDoor))
            {
                if (Vector2.Distance(startDoor.PathfindingOrigin, endDoor.PathfindingOrigin) == 16)
                    return true;
            }

            //translate doorways to grid
            var startDoorwayGridPos = (startDoor.PathfindingOrigin / 16).ToPoint();
            var endDoorwayGridPos = (endDoor.PathfindingOrigin / 16).ToPoint();

            //reserved positions are extra positions outside the normal path that we still want to consider for the direction mask
            List<Vector2> reservedPositions = new List<Vector2>();
            reservedPositions.Add(startDoor.PathfindingOrigin);
            reservedPositions.Add(endDoor.PathfindingOrigin);

            //adjust starting position based on direction we're moving from
            var yOffset = 1;
            var xOffset = 2;
            Vector2 startDoorwaySide1 = Vector2.Zero;
            Vector2 startDoorwaySide2 = Vector2.Zero;
            Vector2 startDoorwayDirection = Vector2.Zero;
            Vector2 endDoorwaySide1 = Vector2.Zero;
            Vector2 endDoorwaySide2 = Vector2.Zero;
            Vector2 endDoorwayDirection = Vector2.Zero;
            switch (startDoor.Direction)
            {
                case "Top":
                    startDoorwayDirection = DirectionHelper.Up;
                    startDoorwaySide1 = startDoor.PathfindingOrigin + (DirectionHelper.Left * 16);
                    startDoorwaySide2 = startDoor.PathfindingOrigin + (DirectionHelper.Right * 16);
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Left * 16));
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Right * 16));
                    startDoorwayGridPos.Y -= yOffset;
                    break;
                case "Bottom":
                    startDoorwayDirection = DirectionHelper.Down;
                    startDoorwaySide1 = startDoor.PathfindingOrigin + (DirectionHelper.Left * 16);
                    startDoorwaySide2 = startDoor.PathfindingOrigin + (DirectionHelper.Right * 16);
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Left * 16));
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Right * 16));
                    startDoorwayGridPos.Y += yOffset;
                    break;
                case "Left":
                    startDoorwayDirection = DirectionHelper.Left;
                    startDoorwaySide1 = startDoor.PathfindingOrigin + (DirectionHelper.Up * 16);
                    startDoorwaySide2 = startDoor.PathfindingOrigin + (DirectionHelper.Down * 16);
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Up * 16));
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Down * 16));
                    startDoorwayGridPos.X -= xOffset;
                    break;
                case "Right":
                    startDoorwayDirection = DirectionHelper.Right;
                    startDoorwaySide1 = startDoor.PathfindingOrigin + (DirectionHelper.Up * 16);
                    startDoorwaySide2 = startDoor.PathfindingOrigin + (DirectionHelper.Down * 16);
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Up * 16));
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Down * 16));
                    startDoorwayGridPos.X += xOffset;
                    break;
            }
            switch (endDoor.Direction)
            {
                case "Top":
                    endDoorwayDirection = DirectionHelper.Up;
                    endDoorwaySide1 = endDoor.PathfindingOrigin + (DirectionHelper.Left * 16);
                    endDoorwaySide2 = endDoor.PathfindingOrigin + (DirectionHelper.Right * 16);
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Left * 16));
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Right * 16));
                    endDoorwayGridPos.Y -= yOffset;
                    break;
                case "Bottom":
                    endDoorwayDirection = DirectionHelper.Down;
                    endDoorwaySide1 = endDoor.PathfindingOrigin + (DirectionHelper.Left * 16);
                    endDoorwaySide2 = endDoor.PathfindingOrigin + (DirectionHelper.Right * 16);
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Left * 16));
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Right * 16));
                    endDoorwayGridPos.Y += yOffset;
                    break;
                case "Left":
                    endDoorwayDirection = DirectionHelper.Left;
                    endDoorwaySide1 = endDoor.PathfindingOrigin + (DirectionHelper.Up * 16);
                    endDoorwaySide2 = endDoor.PathfindingOrigin + (DirectionHelper.Down * 16);
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Up * 16));
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Down * 16));
                    endDoorwayGridPos.X -= xOffset;
                    break;
                case "Right":
                    endDoorwayDirection = DirectionHelper.Right;
                    endDoorwaySide1 = endDoor.PathfindingOrigin + (DirectionHelper.Up * 16);
                    endDoorwaySide2 = endDoor.PathfindingOrigin + (DirectionHelper.Down * 16);
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Up * 16));
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Down * 16));
                    endDoorwayGridPos.X += xOffset;
                    break;
            }

            var padding = 2;
            for (int i = 1; i < padding + 1; i++)
            {
                graph.Walls.Add((startDoorwaySide1 / 16).ToPoint() + (startDoorwayDirection * i).ToPoint() - (graphRect.Location / 16).ToPoint());
                graph.Walls.Add((startDoorwaySide2 / 16).ToPoint() + (startDoorwayDirection * i).ToPoint() - (graphRect.Location / 16).ToPoint());
                graph.Walls.Add((endDoorwaySide1 / 16).ToPoint() + (endDoorwayDirection * i).ToPoint() - (graphRect.Location / 16).ToPoint());
                graph.Walls.Add((endDoorwaySide2 / 16).ToPoint() + (endDoorwayDirection * i).ToPoint() - (graphRect.Location / 16).ToPoint());
            }

            for (int i = 1; i < padding + 2; i++)
            {
                if (graph.Walls.Contains((startDoor.PathfindingOrigin / 16).ToPoint() + (startDoorwayDirection * i).ToPoint() - (graphRect.Location / 16).ToPoint()))
                    graph.Walls.Remove((startDoor.PathfindingOrigin / 16).ToPoint() + (startDoorwayDirection * i).ToPoint() - (graphRect.Location / 16).ToPoint());

                if (graph.Walls.Contains((endDoor.PathfindingOrigin / 16).ToPoint() + (endDoorwayDirection * i).ToPoint() - (graphRect.Location / 16).ToPoint()))
                    graph.Walls.Remove((endDoor.PathfindingOrigin / 16).ToPoint() + (endDoorwayDirection * i).ToPoint() - (graphRect.Location / 16).ToPoint());
            }

            var isPathValid = false;
            while (!isPathValid)
            {
                isPathValid = true;

                //try finding a path
                var path = graph.Search(startDoorwayGridPos - (graphRect.Location / 16).ToPoint(), endDoorwayGridPos - (graphRect.Location / 16).ToPoint());

                //if no path found, connection failed
                //if (path == null || path.Count > 30)
                //    return false;

                if (path == null)
                    return false;

                //get path in world space
                var adjustedPath = path.Select(p =>
                {
                    var worldPos = new Vector2(p.X, p.Y) * 16;
                    worldPos += graphRect.Location;
                    return worldPos;
                }).ToList();

                //get larger hallway
                var largerPath = IncreaseCorridorWidth(adjustedPath, reservedPositions);

                floorPositions = largerPath.SelectMany(p => p.Value).ToList();
                return true;
            }

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
