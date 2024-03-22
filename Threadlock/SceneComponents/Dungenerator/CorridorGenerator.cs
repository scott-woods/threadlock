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
    }
}
