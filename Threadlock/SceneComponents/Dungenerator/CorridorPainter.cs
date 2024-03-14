using Nez.Tiled;
using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Entities;
using Threadlock.Helpers;
using Threadlock.StaticData;
using Microsoft.Xna.Framework;
using Threadlock.Models;

namespace Threadlock.SceneComponents.Dungenerator
{
    public static class CorridorPainter
    {
        [Flags]
        public enum TileDirection
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

        public enum TileOrientation
        {
            None,
            Center,
            TopLeft,
            TopEdge,
            TopRight,
            LeftEdge,
            RightEdge,
            BottomLeft,
            BottomEdge,
            BottomRight,
            TopLeftInverse,
            TopRightInverse,
            BottomLeftInverse,
            BottomRightInverse
        }

        static TileDirection TopLeftRequiredDirs { get => TileDirection.Bottom | TileDirection.Right; }
        static TileDirection TopRightRequiredDirs { get => TileDirection.Left | TileDirection.Bottom; }
        static TileDirection BottomLeftRequiredDirs { get => TileDirection.Top | TileDirection.Right; }
        static TileDirection BottomRightRequiredDirs { get => TileDirection.Left | TileDirection.Top; }
        static TileDirection TopEdgeRequiredDirs { get => TileDirection.Left | TileDirection.Right | TileDirection.Bottom; }
        static TileDirection LeftEdgeRequiredDirs { get => TileDirection.Top | TileDirection.Bottom | TileDirection.Right; }
        static TileDirection BottomEdgeRequiredDirs { get => TileDirection.Left | TileDirection.Right | TileDirection.Top; }
        static TileDirection RightEdgeRequiredDirs { get => TileDirection.Top | TileDirection.Bottom | TileDirection.Left; }

        public static TileOrientation GetTileOrientation(Vector2 tilePosition, List<Vector2> allTilePositions)
        {
            var mask = GetTileBitmask(tilePosition, allTilePositions);

            //center
            if (mask == (TileDirection.Top | TileDirection.Bottom | TileDirection.Left | TileDirection.Right | TileDirection.TopLeft | TileDirection.TopRight | TileDirection.BottomLeft | TileDirection.BottomRight))
                return TileOrientation.Center;
            //top left
            else if ((mask & TopLeftRequiredDirs) == TopLeftRequiredDirs
                    && (mask & (TileDirection.Top | TileDirection.Left)) == 0)
                return TileOrientation.TopLeft;
            //top right corner
            else if ((mask & TopRightRequiredDirs) == TopRightRequiredDirs
                && (mask & (TileDirection.Top | TileDirection.Right)) == 0)
                return TileOrientation.TopRight;
            //bottom right corner
            else if ((mask & BottomRightRequiredDirs) == BottomRightRequiredDirs
                && (mask & (TileDirection.Bottom | TileDirection.Right)) == 0)
                return TileOrientation.BottomRight;
            //bottom left corner
            else if ((mask & BottomLeftRequiredDirs) == BottomLeftRequiredDirs
                && (mask & (TileDirection.Bottom | TileDirection.Left)) == 0)
                return TileOrientation.BottomLeft;
            //top edge
            else if ((mask & TopEdgeRequiredDirs) == TopEdgeRequiredDirs
                && (mask & TileDirection.Top) == 0)
                return TileOrientation.TopEdge;
            //right edge
            else if ((mask & RightEdgeRequiredDirs) == RightEdgeRequiredDirs
                && (mask & TileDirection.Right) == 0)
                return TileOrientation.RightEdge;
            //bottom edge
            else if ((mask & BottomEdgeRequiredDirs) == BottomEdgeRequiredDirs
                && (mask & (TileDirection.Bottom)) == 0)
                return TileOrientation.BottomEdge;
            //left edge
            else if ((mask & LeftEdgeRequiredDirs) == LeftEdgeRequiredDirs
                && (mask & TileDirection.Left) == 0)
                return TileOrientation.LeftEdge;
            //bottom right inverse corner
            else if ((mask & BottomRightRequiredDirs) == BottomRightRequiredDirs
                && (mask & TileDirection.TopLeft) == 0)
                return TileOrientation.BottomRightInverse;
            //bottom left inverse corner
            else if ((mask & BottomLeftRequiredDirs) == BottomLeftRequiredDirs
                && (mask & TileDirection.TopRight) == 0)
                return TileOrientation.BottomLeftInverse;
            //top left inverse corner
            else if ((mask & TopLeftRequiredDirs) == TopLeftRequiredDirs
                && (mask & TileDirection.BottomRight) == 0)
                return TileOrientation.TopLeftInverse;
            //top right inverse corner
            else if ((mask & TopRightRequiredDirs) == TopRightRequiredDirs
                && (mask & TileDirection.BottomLeft) == 0)
                return TileOrientation.TopRightInverse;

            return TileOrientation.None;
        }

        public static TileDirection GetTileBitmask(Vector2 currentTile, List<Vector2> allTiles)
        {
            TileDirection mask = TileDirection.None;
            if (allTiles.Contains(currentTile + DirectionHelper.Up * 16))
                mask |= TileDirection.Top;
            if (allTiles.Contains(currentTile + DirectionHelper.Down * 16))
                mask |= TileDirection.Bottom;
            if (allTiles.Contains(currentTile + DirectionHelper.Left * 16))
                mask |= TileDirection.Left;
            if (allTiles.Contains(currentTile + DirectionHelper.Right * 16))
                mask |= TileDirection.Right;
            if (allTiles.Contains(currentTile + DirectionHelper.UpLeft * 16))
                mask |= TileDirection.TopLeft;
            if (allTiles.Contains(currentTile + DirectionHelper.UpRight * 16))
                mask |= TileDirection.TopRight;
            if (allTiles.Contains(currentTile + DirectionHelper.DownLeft * 16))
                mask |= TileDirection.BottomLeft;
            if (allTiles.Contains(currentTile + DirectionHelper.DownRight * 16))
                mask |= TileDirection.BottomRight;

            return mask;
        }

        public static List<SingleTileRenderer> PaintCorridorTiles(List<Vector2> floorPositions, List<Vector2> reservedPositions, TmxTileset tileset)
        {
            var renderers = new List<SingleTileRenderer>();

            var allTilesForMask = floorPositions.Concat(reservedPositions).ToList();

            Dictionary<Vector2, SingleTileRenderer> tileDictionary = new Dictionary<Vector2, SingleTileRenderer>();
            Dictionary<Vector2, SingleTileRenderer> frontTileDictionary = new Dictionary<Vector2, SingleTileRenderer>();
            Dictionary<Vector2, SingleTileRenderer> sideWallDictionary = new Dictionary<Vector2, SingleTileRenderer>();
            Dictionary<Vector2, SingleTileRenderer> colliderDictionary = new Dictionary<Vector2, SingleTileRenderer>();

            foreach (var floorPos in floorPositions)
            {
                var orientation = GetTileOrientation(floorPos, allTilesForMask);
                var mask = GetTileBitmask(floorPos, allTilesForMask);

                switch (orientation)
                {
                    case TileOrientation.Center:
                        tileDictionary[floorPos] = new SingleTileRenderer(tileset, Tiles.Forge.Floor.StoneCenter, RenderLayers.Back);
                        break;
                    case TileOrientation.TopLeft:
                        //floor
                        tileDictionary[floorPos] = new SingleTileRenderer(tileset, Tiles.Forge.Floor.StoneTopLeftCorner, RenderLayers.Back);

                        //walls
                        var lowerWall = Tiles.Forge.Walls.LeftCornerLower;
                        var midWall = Tiles.Forge.Walls.LeftCornerMid;
                        var topWall = Tiles.Forge.Walls.LeftCornerTop;
                        sideWallDictionary[floorPos + (DirectionHelper.Up * 16 * 3) + (DirectionHelper.Left * 16)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.LeftSideRightTurn, RenderLayers.Back, true);
                        for (int i = 0; i < 3; i++)
                            sideWallDictionary[floorPos + (DirectionHelper.Left * 16) + (DirectionHelper.Up * 16 * i)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.LeftSide, RenderLayers.Back, true);
                        if ((mask & TileDirection.TopRight) == 0)
                        {
                            tileDictionary[floorPos + (DirectionHelper.Up * 16)] = new SingleTileRenderer(tileset, lowerWall, RenderLayers.Back, true);
                            tileDictionary[floorPos + (DirectionHelper.Up * 16 * 2)] = new SingleTileRenderer(tileset, midWall, RenderLayers.Back, true);
                            tileDictionary[floorPos + (DirectionHelper.Up * 16 * 3)] = new SingleTileRenderer(tileset, topWall, RenderLayers.Back, true);
                        }
                        break;
                    case TileOrientation.TopRight:
                        //floor
                        tileDictionary[floorPos] = new SingleTileRenderer(tileset, Tiles.Forge.Floor.StoneTopRightCorner, RenderLayers.Back);

                        //walls
                        var topRightLowerWall = Tiles.Forge.Walls.RightCornerLower;
                        var topRightMidWall = Tiles.Forge.Walls.RightCornerMid;
                        var topRightTopWall = Tiles.Forge.Walls.RightCornerTop;
                        sideWallDictionary[floorPos + (DirectionHelper.Up * 16 * 3) + (DirectionHelper.Right * 16)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.RightSideLeftTurn, RenderLayers.Back, true);
                        for (int i = 0; i < 3; i++)
                            sideWallDictionary[floorPos + (DirectionHelper.Right * 16) + (DirectionHelper.Up * 16 * i)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.RightSide, RenderLayers.Back, true);
                        if ((mask & TileDirection.TopLeft) == 0)
                        {
                            tileDictionary[floorPos + (DirectionHelper.Up * 16)] = new SingleTileRenderer(tileset, topRightLowerWall, RenderLayers.Back, true);
                            tileDictionary[floorPos + (DirectionHelper.Up * 16 * 2)] = new SingleTileRenderer(tileset, topRightMidWall, RenderLayers.Back, true);
                            tileDictionary[floorPos + (DirectionHelper.Up * 16 * 3)] = new SingleTileRenderer(tileset, topRightTopWall, RenderLayers.Back, true);
                        }
                        break;
                    case TileOrientation.BottomRight:
                        //floor
                        tileDictionary[floorPos] = new SingleTileRenderer(tileset, Tiles.Forge.Floor.StoneBottomRightCorner, RenderLayers.Back);

                        //front
                        if ((mask & (TileDirection.BottomLeft | TileDirection.BottomRight)) == 0)
                            frontTileDictionary[floorPos] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.Bottom, RenderLayers.Front);

                        //walls
                        colliderDictionary[floorPos + (DirectionHelper.Down * 16)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.Collider, RenderLayers.Back, true);
                        tileDictionary[floorPos + (DirectionHelper.Right * 16)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.BottomRightSideTurnLeft, RenderLayers.Back, true);
                        colliderDictionary[floorPos + (DirectionHelper.DownRight * 16)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.Collider, RenderLayers.Back, true);
                        break;
                    case TileOrientation.BottomLeft:
                        //floor
                        tileDictionary[floorPos] = new SingleTileRenderer(tileset, Tiles.Forge.Floor.StoneBottomLeftCorner, RenderLayers.Back);

                        //front
                        if ((mask & (TileDirection.BottomLeft | TileDirection.BottomRight)) == 0)
                            frontTileDictionary[floorPos] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.Bottom, RenderLayers.Front);

                        //walls
                        colliderDictionary[floorPos + (DirectionHelper.Down * 16)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.Collider, RenderLayers.Back, true);
                        tileDictionary[floorPos + (DirectionHelper.Left * 16)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.BottomLeftSideTurnRight, RenderLayers.Back, true);
                        colliderDictionary[floorPos + (DirectionHelper.DownLeft * 16)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.Collider, RenderLayers.Back, true);
                        break;
                    case TileOrientation.TopEdge:
                        //floor
                        tileDictionary[floorPos] = new SingleTileRenderer(tileset, Tiles.Forge.Floor.StoneTopEdge, RenderLayers.Back);

                        //walls
                        if ((mask & (TileDirection.TopLeft | TileDirection.TopRight)) == 0)
                        {
                            tileDictionary[floorPos + (DirectionHelper.Up * 16)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.NormalLower, RenderLayers.Back, true);
                            tileDictionary[floorPos + (DirectionHelper.Up * 16 * 2)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.NormalMid, RenderLayers.Back, true);
                            tileDictionary[floorPos + (DirectionHelper.Up * 16 * 3)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.NormalTop, RenderLayers.Back, true);
                        }
                        break;
                    case TileOrientation.RightEdge:
                        //floor
                        tileDictionary[floorPos] = new SingleTileRenderer(tileset, Tiles.Forge.Floor.StoneRightEdge, RenderLayers.Back);

                        //walls
                        sideWallDictionary[floorPos + (DirectionHelper.Right * 16)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.RightSide, RenderLayers.Back, true);
                        break;
                    case TileOrientation.BottomEdge:
                        //floor
                        tileDictionary[floorPos] = new SingleTileRenderer(tileset, Tiles.Forge.Floor.StoneBottomEdge, RenderLayers.Back);

                        //front
                        if ((mask & (TileDirection.BottomLeft | TileDirection.BottomRight)) == 0)
                            frontTileDictionary[floorPos] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.Bottom, RenderLayers.Front);

                        //walls
                        colliderDictionary[floorPos + (DirectionHelper.Down * 16)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.Collider, RenderLayers.Back, true);
                        break;
                    case TileOrientation.LeftEdge:
                        //floor
                        tileDictionary[floorPos] = new SingleTileRenderer(tileset, Tiles.Forge.Floor.StoneLeftEdge, RenderLayers.Back);

                        //walls
                        sideWallDictionary[floorPos + (DirectionHelper.Left * 16)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.LeftSide, RenderLayers.Back, true);
                        break;
                    case TileOrientation.BottomRightInverse:
                        //floor
                        tileDictionary[floorPos] = new SingleTileRenderer(tileset, Tiles.Forge.Floor.StoneBottomRightInverse, RenderLayers.Back);

                        //walls
                        var botRightInverseLowerWall = Tiles.Forge.Walls.RightEdgeLower;
                        var botRightInverseMidWall = Tiles.Forge.Walls.RightEdgeMid;
                        var botRightInverseTopWall = Tiles.Forge.Walls.RightEdgeTop;
                        if (!allTilesForMask.Contains(floorPos + (DirectionHelper.Left * 16 * 2)))
                        {
                            botRightInverseLowerWall = Tiles.Forge.Walls.RightEdgeCornerLower;
                            botRightInverseMidWall = Tiles.Forge.Walls.RightEdgeCornerMid;
                            botRightInverseTopWall = Tiles.Forge.Walls.RightEdgeCornerTop;
                        }
                        var bottomRightInverseOffset = (DirectionHelper.Left * 16);
                        tileDictionary[floorPos + bottomRightInverseOffset + (DirectionHelper.Up * 16)] = new SingleTileRenderer(tileset, botRightInverseLowerWall, RenderLayers.Back, true);
                        tileDictionary[floorPos + bottomRightInverseOffset + (DirectionHelper.Up * 16 * 2)] = new SingleTileRenderer(tileset, botRightInverseMidWall, RenderLayers.Back, true);
                        tileDictionary[floorPos + bottomRightInverseOffset + (DirectionHelper.Up * 16 * 3)] = new SingleTileRenderer(tileset, botRightInverseTopWall, RenderLayers.Back, true);
                        break;
                    case TileOrientation.BottomLeftInverse:
                        //floor
                        tileDictionary[floorPos] = new SingleTileRenderer(tileset, Tiles.Forge.Floor.StoneBottomLeftInverse, RenderLayers.Back);

                        //walls
                        var botLeftInverseLowerWall = Tiles.Forge.Walls.LeftEdgeLower;
                        var botLeftInverseMidWall = Tiles.Forge.Walls.LeftEdgeMid;
                        var botLeftInverseTopWall = Tiles.Forge.Walls.LeftEdgeTop;
                        if (!allTilesForMask.Contains(floorPos + (DirectionHelper.Right * 16 * 2)))
                        {
                            botLeftInverseLowerWall = Tiles.Forge.Walls.LeftEdgeCornerLower;
                            botLeftInverseMidWall = Tiles.Forge.Walls.LeftEdgeCornerMid;
                            botLeftInverseTopWall = Tiles.Forge.Walls.LeftEdgeCornerTop;
                        }
                        var botLeftInverseOffset = (DirectionHelper.Right * 16);
                        tileDictionary[floorPos + botLeftInverseOffset + (DirectionHelper.Up * 16)] = new SingleTileRenderer(tileset, botLeftInverseLowerWall, RenderLayers.Back, true);
                        tileDictionary[floorPos + botLeftInverseOffset + (DirectionHelper.Up * 16 * 2)] = new SingleTileRenderer(tileset, botLeftInverseMidWall, RenderLayers.Back, true);
                        tileDictionary[floorPos + botLeftInverseOffset + (DirectionHelper.Up * 16 * 3)] = new SingleTileRenderer(tileset, botLeftInverseTopWall, RenderLayers.Back, true);
                        break;
                    case TileOrientation.TopLeftInverse:
                        //floor
                        tileDictionary[floorPos] = new SingleTileRenderer(tileset, Tiles.Forge.Floor.StoneTopLeftInverse, RenderLayers.Back);

                        //front
                        frontTileDictionary[floorPos + (DirectionHelper.Right * 16)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.BottomTurnLeft, RenderLayers.Front);
                        break;
                    case TileOrientation.TopRightInverse:
                        //floor
                        tileDictionary[floorPos] = new SingleTileRenderer(tileset, Tiles.Forge.Floor.StoneTopRightInverse, RenderLayers.Back);

                        //front
                        frontTileDictionary[floorPos + (DirectionHelper.Left * 16)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.BottomTurnRight, RenderLayers.Front);
                        break;
                }
            }

            foreach (var kvp in tileDictionary)
            {
                var renderer = Game1.Scene.CreateEntity("", kvp.Key).AddComponent(kvp.Value);
                renderers.Add(renderer);
            }
            foreach (var kvp in frontTileDictionary)
            {
                var renderer = Game1.Scene.CreateEntity("", kvp.Key).AddComponent(kvp.Value);
                renderers.Add(renderer);
            }
            foreach (var kvp in sideWallDictionary.Where(w => !tileDictionary.ContainsKey(w.Key)))
            {
                var renderer = Game1.Scene.CreateEntity("", kvp.Key).AddComponent(kvp.Value);
                renderers.Add(renderer);
            }
            foreach (var kvp in colliderDictionary.Where(w => !sideWallDictionary.ContainsKey(w.Key) && !tileDictionary.ContainsKey(w.Key)))
            {
                var renderer = Game1.Scene.CreateEntity("", kvp.Key).AddComponent(kvp.Value);
                renderers.Add(renderer);
            }

            return renderers;
        }
    }
}
