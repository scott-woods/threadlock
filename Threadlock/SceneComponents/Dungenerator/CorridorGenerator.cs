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

        public static bool ConnectDoorways(DungeonDoorway startDoor, DungeonDoorway endDoor, WeightedGridGraph graph, List<DungeonRoomEntity> roomsToCheck, out List<SingleTile> tiles)
        {
            tiles = new List<SingleTile>();

            var startDoorwayGridPos = (startDoor.PathfindingOrigin / 16).ToPoint();
            var endDoorwayGridPos = (endDoor.PathfindingOrigin / 16).ToPoint();
            List<Vector2> reservedPositions = new List<Vector2>();
            reservedPositions.Add(startDoor.PathfindingOrigin);
            reservedPositions.Add(endDoor.PathfindingOrigin);

            switch (startDoor.Direction)
            {
                case "Top":
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Left * 16));
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Right * 16));
                    startDoorwayGridPos.Y -= 2;
                    break;
                case "Bottom":
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Left * 16));
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Right * 16));
                    startDoorwayGridPos.Y += 2;
                    break;
                case "Left":
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Up * 16));
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Down * 16));
                    startDoorwayGridPos.X -= 2;
                    break;
                case "Right":
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Up * 16));
                    reservedPositions.Add(startDoor.PathfindingOrigin + (DirectionHelper.Down * 16));
                    startDoorwayGridPos.X += 2;
                    break;
            }
            switch (endDoor.Direction)
            {
                case "Top":
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Left * 16));
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Right * 16));
                    endDoorwayGridPos.Y -= 2;
                    break;
                case "Bottom":
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Left * 16));
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Right * 16));
                    endDoorwayGridPos.Y += 2;
                    break;
                case "Left":
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Up * 16));
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Down * 16));
                    endDoorwayGridPos.X -= 2;
                    break;
                case "Right":
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Up * 16));
                    reservedPositions.Add(endDoor.PathfindingOrigin + (DirectionHelper.Down * 16));
                    endDoorwayGridPos.X += 2;
                    break;
            }

            //graph.Walls.Remove(startDoorwayGridPos);
            //graph.Walls.Remove(endDoorwayGridPos);

            var isPathValid = false;
            while (!isPathValid)
            {
                isPathValid = true;

                tiles.Clear();

                //try finding a path
                var path = graph.Search(startDoorwayGridPos, endDoorwayGridPos);

                //if no path found, connection failed
                if (path == null)
                    return false;

                //get path in world space
                var adjustedPath = path.Select(p =>
                {
                    return new Vector2(p.X, p.Y) * 16;
                }).ToList();

                var largerPath = IncreaseCorridorWidth(adjustedPath);
                var allTilePositions = largerPath.SelectMany(p => p.Value)
                    .Concat(reservedPositions)
                    .ToList();

                var tileDictionary = new Dictionary<Vector2, List<SingleTile>>();

                //check that all tiles in larger path are valid
                var setsToCheck = largerPath.Where(p => p.Key != startDoorwayGridPos.ToVector2() * 16 && p.Key != endDoorwayGridPos.ToVector2() * 16).ToList();
                foreach (var pathSet in setsToCheck)
                {
                    var tilesToAdd = new Dictionary<Vector2, SingleTile>();

                    foreach (var pathPoint in pathSet.Value)
                    {
                        //first check just the floor points, if any overlap go ahead and break
                        //if (roomsToCheck.Any(r => r.OverlapsRoom(pathPoint, false)))
                        //    break;

                        //all floor points were valid, now try handling walls
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

                        //center tile
                        if (mask == (Direction.Top | Direction.Bottom | Direction.Left | Direction.Right | Direction.TopLeft | Direction.TopRight | Direction.BottomLeft | Direction.BottomRight))
                            tilesToAdd[pathPoint] = (new SingleTile(pathPoint, Tiles.Forge.Floor.StoneCenter));
                        //top left corner
                        else if ((mask & (Direction.Bottom | Direction.Right)) != 0
                            && (mask & (Direction.Top | Direction.Left)) == 0)
                        {
                            tilesToAdd[pathPoint] = (new SingleTile(pathPoint, Tiles.Forge.Floor.StoneTopLeftCorner));
                            tilesToAdd[pathPoint + (DirectionHelper.Up * 16)] = (new SingleTile(pathPoint + (DirectionHelper.Up * 16), Tiles.Forge.Walls.LeftCornerLower));
                            tilesToAdd[pathPoint + (DirectionHelper.Up * 16 * 2)] = (new SingleTile(pathPoint + (DirectionHelper.Up * 16 * 2), Tiles.Forge.Walls.LeftCornerMid));
                            tilesToAdd[pathPoint + (DirectionHelper.Up * 16 * 3)] = (new SingleTile(pathPoint + (DirectionHelper.Up * 16 * 3), Tiles.Forge.Walls.LeftCornerTop));
                            tilesToAdd[pathPoint + (DirectionHelper.Up * 16 * 3)] = (new SingleTile(pathPoint + (DirectionHelper.Up * 16 * 3) + (DirectionHelper.Left * 16), Tiles.Forge.Walls.LeftSideRightTurn));
                        }

                        //top right corner
                        else if ((mask & (Direction.Bottom | Direction.Left)) != 0
                            && (mask & (Direction.Top | Direction.Right)) == 0)
                        {
                            tilesToAdd[pathPoint] = (new SingleTile(pathPoint, Tiles.Forge.Floor.StoneTopRightCorner));
                            tilesToAdd[pathPoint + (DirectionHelper.Up * 16)] = (new SingleTile(pathPoint + (DirectionHelper.Up * 16), Tiles.Forge.Walls.RightCornerLower));
                            tilesToAdd[pathPoint + (DirectionHelper.Up * 16 * 2)] = (new SingleTile(pathPoint + (DirectionHelper.Up * 16 * 2), Tiles.Forge.Walls.RightCornerMid));
                            tilesToAdd[pathPoint + (DirectionHelper.Up * 16 * 3)] = (new SingleTile(pathPoint + (DirectionHelper.Up * 16 * 3), Tiles.Forge.Walls.RightCornerTop));
                            tilesToAdd[pathPoint + (DirectionHelper.Up * 16 * 3) + (DirectionHelper.Right * 16)] = (new SingleTile(pathPoint + (DirectionHelper.Up * 16 * 3) + (DirectionHelper.Right * 16), Tiles.Forge.Walls.RightSideLeftTurn));
                        }
                        //bottom right corner
                        else if ((mask & (Direction.Top | Direction.Left)) != 0
                            && (mask & (Direction.Bottom | Direction.Right)) == 0)
                        {
                            tilesToAdd[pathPoint] = (new SingleTile(pathPoint, Tiles.Forge.Floor.StoneBottomRightCorner));
                            tilesToAdd[pathPoint + (DirectionHelper.Down * 16)] = (new SingleTile(pathPoint + (DirectionHelper.Down * 16), Tiles.Forge.Walls.Collider));
                            tilesToAdd[pathPoint + (DirectionHelper.Right * 16) + (DirectionHelper.Down * 16)] = (new SingleTile(pathPoint + (DirectionHelper.Right * 16) + (DirectionHelper.Down * 16), Tiles.Forge.Walls.Collider));
                            tilesToAdd[pathPoint + (DirectionHelper.Right * 16)] = (new SingleTile(pathPoint + (DirectionHelper.Right * 16), Tiles.Forge.Walls.BottomRightSideTurnLeft));
                        }
                        //bottom left corner
                        else if ((mask & (Direction.Top | Direction.Right)) != 0
                            && (mask & (Direction.Bottom | Direction.Left)) == 0)
                        {
                            tilesToAdd[pathPoint] = (new SingleTile(pathPoint, Tiles.Forge.Floor.StoneBottomLeftCorner));
                            tilesToAdd[pathPoint + (DirectionHelper.Down * 16)] = (new SingleTile(pathPoint + (DirectionHelper.Down * 16), Tiles.Forge.Walls.Collider));
                            tilesToAdd[pathPoint + (DirectionHelper.Left * 16) + (DirectionHelper.Down * 16)] = (new SingleTile(pathPoint + (DirectionHelper.Left * 16) + (DirectionHelper.Down * 16), Tiles.Forge.Walls.Collider));
                            tilesToAdd[pathPoint + (DirectionHelper.Left * 16)] = (new SingleTile(pathPoint + (DirectionHelper.Left * 16), Tiles.Forge.Walls.BottomLeftSideTurnRight));
                        }
                        //top edge
                        else if ((mask & (Direction.Bottom | Direction.Left | Direction.Right)) != 0
                            && (mask & Direction.Top) == 0)
                        {
                            tilesToAdd[pathPoint] = (new SingleTile(pathPoint, Tiles.Forge.Floor.StoneTopEdge));
                            tilesToAdd[pathPoint + (DirectionHelper.Up * 16)] = (new SingleTile(pathPoint + (DirectionHelper.Up * 16), Tiles.Forge.Walls.NormalLower));
                            tilesToAdd[pathPoint + (DirectionHelper.Up * 16 * 2)] = (new SingleTile(pathPoint + (DirectionHelper.Up * 16 * 2), Tiles.Forge.Walls.NormalMid));
                            tilesToAdd[pathPoint + (DirectionHelper.Up * 16 * 3)] = (new SingleTile(pathPoint + (DirectionHelper.Up * 16 * 3), Tiles.Forge.Walls.NormalTop));
                        }
                        //right edge
                        else if ((mask & (Direction.Left | Direction.Top | Direction.Bottom)) != 0
                            && (mask & Direction.Right) == 0)
                        {
                            tilesToAdd[pathPoint] = (new SingleTile(pathPoint, Tiles.Forge.Floor.StoneRightEdge));
                            tilesToAdd[pathPoint + (DirectionHelper.Right * 16)] = (new SingleTile(pathPoint + (DirectionHelper.Right * 16), Tiles.Forge.Walls.RightSide));
                        }
                        //bottom edge
                        else if ((mask & (Direction.Top | Direction.Left | Direction.Right)) != 0
                            && (mask & Direction.Bottom) == 0)
                        {
                            tilesToAdd[pathPoint] = (new SingleTile(pathPoint, Tiles.Forge.Floor.StoneBottomEdge));
                            tilesToAdd[pathPoint + (DirectionHelper.Down * 16)] = (new SingleTile(pathPoint + (DirectionHelper.Down * 16), Tiles.Forge.Walls.Collider));
                        }
                        //left edge
                        else if ((mask & (Direction.Right | Direction.Top | Direction.Bottom)) != 0
                            && (mask & Direction.Left) == 0)
                        {
                            tilesToAdd[pathPoint] = (new SingleTile(pathPoint, Tiles.Forge.Floor.StoneLeftEdge));
                            tilesToAdd[pathPoint + (DirectionHelper.Left * 16)] = (new SingleTile(pathPoint + (DirectionHelper.Left * 16), Tiles.Forge.Walls.LeftSide));
                        }
                        //bottom right inverse corner
                        else if ((mask & (Direction.Left | Direction.Top)) != 0
                            && (mask & Direction.TopLeft) == 0)
                        {
                            tilesToAdd[pathPoint] = (new SingleTile(pathPoint, Tiles.Forge.Floor.StoneBottomRightInverse));
                            var offset = DirectionHelper.Left * 16;
                            tilesToAdd[pathPoint + offset + (DirectionHelper.Up * 16)] = (new SingleTile(pathPoint + offset + (DirectionHelper.Up * 16), Tiles.Forge.Walls.RightEdgeCornerLower));
                            tilesToAdd[pathPoint + offset + (DirectionHelper.Up * 16 * 2)] = (new SingleTile(pathPoint + offset + (DirectionHelper.Up * 16 * 2), Tiles.Forge.Walls.RightEdgeCornerMid));
                            tilesToAdd[pathPoint + offset + (DirectionHelper.Up * 16 * 3)] = (new SingleTile(pathPoint + offset + (DirectionHelper.Up * 16 * 3), Tiles.Forge.Walls.RightEdgeCornerTop));
                        }
                        //bottom left inverse corner
                        else if ((mask & (Direction.Right | Direction.Top)) != 0
                            && (mask & Direction.TopRight) == 0)
                        {
                            tilesToAdd[pathPoint] = (new SingleTile(pathPoint, Tiles.Forge.Floor.StoneBottomLeftInverse));
                            var offset = DirectionHelper.Right * 16;
                            tilesToAdd[pathPoint + offset + (DirectionHelper.Up * 16)] = (new SingleTile(pathPoint + offset + (DirectionHelper.Up * 16), Tiles.Forge.Walls.LeftEdgeCornerLower));
                            tilesToAdd[pathPoint + offset + (DirectionHelper.Up * 16 * 2)] = (new SingleTile(pathPoint + offset + (DirectionHelper.Up * 16 * 2), Tiles.Forge.Walls.LeftEdgeCornerMid));
                            tilesToAdd[pathPoint + offset + (DirectionHelper.Up * 16 * 3)] = (new SingleTile(pathPoint + offset + (DirectionHelper.Up * 16 * 3), Tiles.Forge.Walls.LeftEdgeCornerTop));
                        }
                        //top left inverse corner
                        else if ((mask & (Direction.Right | Direction.Bottom)) != 0
                            && (mask & Direction.BottomRight) == 0)
                        {
                            tilesToAdd[pathPoint] = (new SingleTile(pathPoint, Tiles.Forge.Floor.StoneTopLeftInverse));
                            tilesToAdd[pathPoint + (DirectionHelper.Right * 16)] = (new SingleTile(pathPoint + (DirectionHelper.Right * 16), Tiles.Forge.Walls.BottomTurnLeft));
                        }
                        //top right inverse corner
                        else if ((mask & (Direction.Left | Direction.Bottom)) != 0
                            && (mask & Direction.BottomLeft) == 0)
                        {
                            tilesToAdd[pathPoint] = (new SingleTile(pathPoint, Tiles.Forge.Floor.StoneTopRightInverse));
                            tilesToAdd[pathPoint + (DirectionHelper.Left * 16)] = (new SingleTile(pathPoint + (DirectionHelper.Left * 16), Tiles.Forge.Walls.BottomTurnRight));
                        }
                    }

                    tileDictionary[pathSet.Key] = tilesToAdd.Values.ToList();

                    //if (pathSet.Value.Any(p =>
                    //{
                    //    if (roomsToCheck.Any(r => r.OverlapsRoom(p, false)))
                    //        return true;
                    //    //if (DirectionHelper.CardinalDirections.Any(d =>
                    //    //{
                    //    //    var posInDirection = p + (d * 16);
                    //    //    return roomsToCheck.Any(r => r.OverlapsRoom(posInDirection, false));
                    //    //}))
                    //    //    return true;
                    //    return false;
                    //}))
                    //{
                    //    var posToAddToWalls = (pathSet.Key / 16).ToPoint();
                    //    if (!graph.Walls.Contains(posToAddToWalls))
                    //        graph.Walls.Add(posToAddToWalls);
                    //    isPathValid = false;
                    //    break;
                    //}
                }

                //validate floor and tile positions
                foreach (var room in roomsToCheck)
                {
                    if (room.OverlapsRoom(tileDictionary.SelectMany(t => t.Value).Select(v => v.Position).ToList(), out var overlappingPositions))
                    {
                        isPathValid = false;

                        foreach (var overlap in overlappingPositions)
                        {
                            var posToAddToWalls = tileDictionary.FirstOrDefault(t => t.Value.Any(v => v.Position == overlap)).Key;
                            var adjustedPos = (posToAddToWalls / 16).ToPoint();
                            if (!graph.Walls.Contains(adjustedPos))
                                graph.Walls.Add(adjustedPos);
                        }
                    }
                }

                if (isPathValid)
                    tiles = tileDictionary.SelectMany(t => t.Value).ToList();
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
                        posDictionary[positions[i - 1]].Add(pos);
                    }
                }
            }

            return posDictionary;
        }
    }
}
