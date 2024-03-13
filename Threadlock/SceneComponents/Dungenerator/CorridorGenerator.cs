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

namespace Threadlock.SceneComponents.Dungenerator
{
    public static class CorridorGenerator
    {
        [Flags]
        enum Direction
        {
            None = 0,
            Top = 1 << 0,
            Bottom = 1 << 1,
            Left = 1 << 2,
            Right = 1 << 3,
            TopLeft = 1 << 4,
            TopRight = 1 << 5,
            BottomLeft = 1 << 6,
            BottomRight = 1 << 7,
        }

        public static bool ConnectDoorways(DungeonDoorway startDoor, DungeonDoorway endDoor, WeightedGridGraph graph, List<DungeonRoomEntity> roomsToCheck, RectangleF graphRect, out List<Vector2> floorPositions)
        {
            //init floor positions list
            floorPositions = new List<Vector2>();

            //translate doorways to grid
            var startDoorwayGridPos = (startDoor.PathfindingOrigin / 16).ToPoint();
            var endDoorwayGridPos = (endDoor.PathfindingOrigin / 16).ToPoint();

            //reserved positions are extra positions outside the normal path that we still want to consider for the direction mask
            List<Vector2> reservedPositions = new List<Vector2>();
            reservedPositions.Add(startDoor.PathfindingOrigin);
            reservedPositions.Add(endDoor.PathfindingOrigin);

            //adjust starting position based on direction we're moving from
            var doorwayOffset = 2;
            switch (startDoor.Direction)
            {
                case "Top":
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Left * 16));
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Right * 16));
                    startDoorwayGridPos.Y -= doorwayOffset;
                    break;
                case "Bottom":
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Left * 16));
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Right * 16));
                    startDoorwayGridPos.Y += doorwayOffset;
                    break;
                case "Left":
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Up * 16));
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Down * 16));
                    startDoorwayGridPos.X -= doorwayOffset;
                    break;
                case "Right":
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Up * 16));
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Down * 16));
                    startDoorwayGridPos.X += doorwayOffset;
                    break;
            }
            switch (endDoor.Direction)
            {
                case "Top":
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Left * 16));
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Right * 16));
                    endDoorwayGridPos.Y -= doorwayOffset;
                    break;
                case "Bottom":
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Left * 16));
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Right * 16));
                    endDoorwayGridPos.Y += doorwayOffset;
                    break;
                case "Left":
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Up * 16));
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Down * 16));
                    endDoorwayGridPos.X -= doorwayOffset;
                    break;
                case "Right":
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Up * 16));
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Down * 16));
                    endDoorwayGridPos.X += doorwayOffset;
                    break;
            }

            var isPathValid = false;
            while (!isPathValid)
            {
                isPathValid = true;

                //try finding a path
                var path = graph.Search(startDoorwayGridPos - (graphRect.Location / 16).ToPoint(), endDoorwayGridPos - (graphRect.Location / 16).ToPoint());

                //if no path found, connection failed
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
                var largerPath = IncreaseCorridorWidth(adjustedPath);

                //get list of all positions that we want to consider for bitmasking
                var allTilePositions = largerPath.SelectMany(p => p.Value)
                    .Concat(reservedPositions)
                    .ToList();

                //key is the actual position, value is the base path position it belongs to
                var posDictionary = new Dictionary<Vector2, Vector2>();

                //check that all tiles in larger path are valid
                var setsToCheck = largerPath.Where(p => p.Key != startDoorwayGridPos.ToVector2() * 16 && p.Key != endDoorwayGridPos.ToVector2() * 16).ToList();
                foreach (var pathSet in setsToCheck)
                {
                    foreach (var pathPoint in pathSet.Value)
                    {
                        posDictionary[pathPoint] = pathSet.Key;

                        //get mask
                        Direction mask = Direction.None;
                        if (allTilePositions.Contains(pathPoint + DirectionHelper.Up * 16))
                            mask |= Direction.Top;
                        if (allTilePositions.Contains(pathPoint + DirectionHelper.Down * 16))
                            mask |= Direction.Bottom;
                        if (allTilePositions.Contains(pathPoint + DirectionHelper.Left * 16))
                            mask |= Direction.Left;
                        if (allTilePositions.Contains(pathPoint + DirectionHelper.Right * 16))
                            mask |= Direction.Right;
                        if (allTilePositions.Contains(pathPoint + DirectionHelper.UpLeft * 16))
                            mask |= Direction.TopLeft;
                        if (allTilePositions.Contains(pathPoint + DirectionHelper.UpRight * 16))
                            mask |= Direction.TopRight;
                        if (allTilePositions.Contains(pathPoint + DirectionHelper.DownLeft * 16))
                            mask |= Direction.BottomLeft;
                        if (allTilePositions.Contains(pathPoint + DirectionHelper.DownRight * 16))
                            mask |= Direction.BottomRight;

                        //top left corner
                        else if ((mask & (Direction.Bottom | Direction.Right)) != 0
                            && (mask & (Direction.Top | Direction.Left)) == 0)
                        {
                            posDictionary[pathPoint + (DirectionHelper.Left * 16)] = pathSet.Key;
                            for (int i = 1; i < 4; i++)
                            {
                                posDictionary[pathPoint + (DirectionHelper.Left * 16) + (DirectionHelper.Up * 16 * i)] = pathSet.Key;
                                posDictionary[pathPoint + (DirectionHelper.Up * 16 * i)] = pathSet.Key;
                            }
                        }

                        //top right corner
                        else if ((mask & (Direction.Bottom | Direction.Left)) != 0
                            && (mask & (Direction.Top | Direction.Right)) == 0)
                        {
                            posDictionary[pathPoint + (DirectionHelper.Right * 16)] = pathSet.Key;
                            for (int i = 1; i < 4; i++)
                            {
                                posDictionary[pathPoint + (DirectionHelper.Right * 16) + (DirectionHelper.Up * 16 * i)] = pathSet.Key;
                                posDictionary[pathPoint + (DirectionHelper.Up * 16 * i)] = pathSet.Key;
                            }
                        }
                        //bottom right corner
                        else if ((mask & (Direction.Top | Direction.Left)) != 0
                            && (mask & (Direction.Bottom | Direction.Right)) == 0)
                        {
                            posDictionary[pathPoint + (DirectionHelper.Down * 16)] = pathSet.Key;
                            posDictionary[pathPoint + (DirectionHelper.Right * 16)] = pathSet.Key;
                            posDictionary[pathPoint + (DirectionHelper.DownRight * 16)] = pathSet.Key;
                        }
                        //bottom left corner
                        else if ((mask & (Direction.Top | Direction.Right)) != 0
                            && (mask & (Direction.Bottom | Direction.Left)) == 0)
                        {
                            posDictionary[pathPoint + (DirectionHelper.Down * 16)] = pathSet.Key;
                            posDictionary[pathPoint + (DirectionHelper.Left * 16)] = pathSet.Key;
                            posDictionary[pathPoint + (DirectionHelper.DownLeft * 16)] = pathSet.Key;
                        }
                        //top edge
                        else if ((mask & (Direction.Bottom | Direction.Left | Direction.Right)) != 0
                            && (mask & Direction.Top) == 0)
                        {
                            for (int i = 1; i < 4; i++)
                                posDictionary[pathPoint + (DirectionHelper.Up * 16 * i)] = pathSet.Key;
                        }
                        //right edge
                        else if ((mask & (Direction.Left | Direction.Top | Direction.Bottom)) != 0
                            && (mask & Direction.Right) == 0)
                        {
                            posDictionary[pathPoint + (DirectionHelper.Right * 16)] = pathSet.Key;
                        }
                        //bottom edge
                        else if ((mask & (Direction.Top | Direction.Left | Direction.Right)) != 0
                            && (mask & Direction.Bottom) == 0)
                        {
                            posDictionary[pathPoint + (DirectionHelper.Down * 16)] = pathSet.Key;
                        }
                        //left edge
                        else if ((mask & (Direction.Right | Direction.Top | Direction.Bottom)) != 0
                            && (mask & Direction.Left) == 0)
                        {
                            posDictionary[pathPoint + (DirectionHelper.Left * 16)] = pathSet.Key;
                        }
                        //bottom right inverse corner
                        else if ((mask & (Direction.Left | Direction.Top)) != 0
                            && (mask & Direction.TopLeft) == 0)
                        {
                            var offset = DirectionHelper.Left * 16;
                            for (int i = 1; i < 4; i++)
                                posDictionary[pathPoint + offset + (DirectionHelper.Up * 16 * i)] = pathSet.Key;
                        }
                        //bottom left inverse corner
                        else if ((mask & (Direction.Right | Direction.Top)) != 0
                            && (mask & Direction.TopRight) == 0)
                        {
                            var offset = DirectionHelper.Right * 16;
                            for (int i = 1; i < 4; i++)
                                posDictionary[pathPoint + offset + (DirectionHelper.Up * 16 * i)] = pathSet.Key;
                        }
                        //top left inverse corner
                        else if ((mask & (Direction.Right | Direction.Bottom)) != 0
                            && (mask & Direction.BottomRight) == 0)
                        {
                            posDictionary[pathPoint + (DirectionHelper.Right * 16)] = pathSet.Key;
                            posDictionary[pathPoint + (DirectionHelper.DownRight * 16)] = pathSet.Key;
                        }
                        //top right inverse corner
                        else if ((mask & (Direction.Left | Direction.Bottom)) != 0
                            && (mask & Direction.BottomLeft) == 0)
                        {
                            posDictionary[pathPoint + (DirectionHelper.Left * 16)] = pathSet.Key;
                            posDictionary[pathPoint + (DirectionHelper.DownLeft * 16)] = pathSet.Key;
                        }
                    }
                }

                //validate floor and tile positions
                foreach (var room in roomsToCheck)
                {
                    List<Vector2> overlappingPositions = new List<Vector2>();

                    if (room.OverlapsRoom(posDictionary.Keys.ToList(), out var roomOverlaps, false))
                        overlappingPositions.AddRange(roomOverlaps);

                    if (overlappingPositions.Any())
                    {
                        isPathValid = false;

                        foreach (var overlap in overlappingPositions)
                        {
                            if (posDictionary.TryGetValue(overlap, out var parentPos))
                            {
                                var adjustedPos = ((parentPos / 16) - (graphRect.Location / 16)).ToPoint();
                                if (!graph.Walls.Contains(adjustedPos))
                                    graph.Walls.Add(adjustedPos);
                            }
                        }
                    }
                }

                if (isPathValid)
                    floorPositions = largerPath.SelectMany(p => p.Value).ToList();
            }

            return true;
        }

        public static Dictionary<Vector2, List<Vector2>> IncreaseCorridorWidth(List<Vector2> positions)
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
                        if (!visitedPositions.Contains(pos))
                        {
                            posDictionary[positions[i - 1]].Add(pos);
                            visitedPositions.Add(pos);
                        }
                    }
                }
            }

            return posDictionary;
        }
    }
}
