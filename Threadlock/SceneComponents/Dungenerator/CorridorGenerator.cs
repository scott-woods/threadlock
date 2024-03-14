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
            var weightOffset = 6;
            switch (startDoor.Direction)
            {
                case "Top":
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Left * 16));
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Right * 16));
                    graph.WeightedNodes.Add(startDoorwayGridPos + new Point(0, -weightOffset) - (graphRect.Location / 16).ToPoint());
                    startDoorwayGridPos.Y -= doorwayOffset;
                    break;
                case "Bottom":
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Left * 16));
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Right * 16));
                    graph.WeightedNodes.Add(startDoorwayGridPos + new Point(0, weightOffset) - (graphRect.Location / 16).ToPoint());
                    startDoorwayGridPos.Y += doorwayOffset;
                    break;
                case "Left":
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Up * 16));
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Down * 16));
                    graph.WeightedNodes.Add(startDoorwayGridPos + new Point(-weightOffset, 0) - (graphRect.Location / 16).ToPoint());
                    startDoorwayGridPos.X -= doorwayOffset;
                    break;
                case "Right":
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Up * 16));
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Down * 16));
                    graph.WeightedNodes.Add(startDoorwayGridPos + new Point(weightOffset, 0) - (graphRect.Location / 16).ToPoint());
                    startDoorwayGridPos.X += doorwayOffset;
                    break;
            }
            switch (endDoor.Direction)
            {
                case "Top":
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Left * 16));
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Right * 16));
                    graph.WeightedNodes.Add(endDoorwayGridPos + new Point(0, -weightOffset) - (graphRect.Location / 16).ToPoint());
                    endDoorwayGridPos.Y -= doorwayOffset;
                    break;
                case "Bottom":
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Left * 16));
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Right * 16));
                    graph.WeightedNodes.Add(endDoorwayGridPos + new Point(0, weightOffset) - (graphRect.Location / 16).ToPoint());
                    endDoorwayGridPos.Y += doorwayOffset;
                    break;
                case "Left":
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Up * 16));
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Down * 16));
                    graph.WeightedNodes.Add(endDoorwayGridPos + new Point(-weightOffset, 0) - (graphRect.Location / 16).ToPoint());
                    endDoorwayGridPos.X -= doorwayOffset;
                    break;
                case "Right":
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Up * 16));
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Down * 16));
                    graph.WeightedNodes.Add(endDoorwayGridPos + new Point(weightOffset, 0) - (graphRect.Location / 16).ToPoint());
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

                        var orientation = GetTileOrientation(pathPoint, allTilePositions);

                        switch (orientation)
                        {
                            case TileOrientation.TopLeft:
                                posDictionary[pathPoint + (DirectionHelper.Left * 16)] = pathSet.Key;
                                for (int i = 1; i < 4; i++)
                                {
                                    posDictionary[pathPoint + (DirectionHelper.Left * 16) + (DirectionHelper.Up * 16 * i)] = pathSet.Key;
                                    posDictionary[pathPoint + (DirectionHelper.Up * 16 * i)] = pathSet.Key;
                                }
                                break;
                            case TileOrientation.TopRight:
                                posDictionary[pathPoint + (DirectionHelper.Right * 16)] = pathSet.Key;
                                for (int i = 1; i < 4; i++)
                                {
                                    posDictionary[pathPoint + (DirectionHelper.Right * 16) + (DirectionHelper.Up * 16 * i)] = pathSet.Key;
                                    posDictionary[pathPoint + (DirectionHelper.Up * 16 * i)] = pathSet.Key;
                                }
                                break;
                            case TileOrientation.BottomRight:
                                posDictionary[pathPoint + (DirectionHelper.Down * 16)] = pathSet.Key;
                                posDictionary[pathPoint + (DirectionHelper.Right * 16)] = pathSet.Key;
                                posDictionary[pathPoint + (DirectionHelper.DownRight * 16)] = pathSet.Key;
                                break;
                            case TileOrientation.BottomLeft:
                                posDictionary[pathPoint + (DirectionHelper.Down * 16)] = pathSet.Key;
                                posDictionary[pathPoint + (DirectionHelper.Left * 16)] = pathSet.Key;
                                posDictionary[pathPoint + (DirectionHelper.DownLeft * 16)] = pathSet.Key;
                                break;
                            case TileOrientation.TopEdge:
                                for (int i = 1; i < 4; i++)
                                    posDictionary[pathPoint + (DirectionHelper.Up * 16 * i)] = pathSet.Key;
                                break;
                            case TileOrientation.RightEdge:
                                posDictionary[pathPoint + (DirectionHelper.Right * 16)] = pathSet.Key;
                                break;
                            case TileOrientation.BottomEdge:
                                posDictionary[pathPoint + (DirectionHelper.Down * 16)] = pathSet.Key;
                                break;
                            case TileOrientation.LeftEdge:
                                posDictionary[pathPoint + (DirectionHelper.Left * 16)] = pathSet.Key;
                                break;
                            case TileOrientation.BottomRightInverse:
                                var bottomRightInverseOffset = DirectionHelper.Left * 16;
                                for (int i = 1; i < 4; i++)
                                    posDictionary[pathPoint + bottomRightInverseOffset + (DirectionHelper.Up * 16 * i)] = pathSet.Key;
                                break;
                            case TileOrientation.BottomLeftInverse:
                                var bottomLeftInverseOffset = DirectionHelper.Right * 16;
                                for (int i = 1; i < 4; i++)
                                    posDictionary[pathPoint + bottomLeftInverseOffset + (DirectionHelper.Up * 16 * i)] = pathSet.Key;
                                break;
                            case TileOrientation.TopLeftInverse:
                                posDictionary[pathPoint + (DirectionHelper.Right * 16)] = pathSet.Key;
                                posDictionary[pathPoint + (DirectionHelper.DownRight * 16)] = pathSet.Key;
                                break;
                            case TileOrientation.TopRightInverse:
                                posDictionary[pathPoint + (DirectionHelper.Left * 16)] = pathSet.Key;
                                posDictionary[pathPoint + (DirectionHelper.DownLeft * 16)] = pathSet.Key;
                                break;
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
