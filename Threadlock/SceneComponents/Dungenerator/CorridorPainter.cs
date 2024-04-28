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
using static Threadlock.Scenes.ForestTest;

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

        public enum TileDirection2
        {
            Top,
            Bottom,
            Left,
            Right,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }

        public enum Corners
        {
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
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

        [Flags]
        public enum WallTileType
        {
            None = 0,
            Floor = 1,
            Wall = 2
        }

        static TileDirection TopLeftRequiredDirs { get => TileDirection.Bottom | TileDirection.Right; }
        static TileDirection TopRightRequiredDirs { get => TileDirection.Left | TileDirection.Bottom; }
        static TileDirection BottomLeftRequiredDirs { get => TileDirection.Top | TileDirection.Right; }
        static TileDirection BottomRightRequiredDirs { get => TileDirection.Left | TileDirection.Top; }
        static TileDirection TopEdgeRequiredDirs { get => TileDirection.Left | TileDirection.Right; }
        static TileDirection LeftEdgeRequiredDirs { get => TileDirection.Top | TileDirection.Bottom | TileDirection.Right; }
        static TileDirection BottomEdgeRequiredDirs { get => TileDirection.Left | TileDirection.Right; }
        static TileDirection RightEdgeRequiredDirs { get => TileDirection.Top | TileDirection.Bottom | TileDirection.Left; }

        public static int CreateTileMask<TEnum>(Dictionary<Corners, TEnum> tileSections) where TEnum : struct, Enum
        {
            int mask = 0;

            foreach (Corners corner in Enum.GetValues(typeof(Corners)))
            {
                var bitPos = (int)corner * 2;

                if (tileSections.TryGetValue(corner, out var tileType))
                {
                    mask |= (Convert.ToInt32(tileType) << bitPos);
                }
            }

            return mask;
        }

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

        public static void CombineTiles(List<Vector2> allTiles, int xRange, int yRange)
        {
            List<Vector2> tilesToAdd = new List<Vector2>();

            foreach (var floorPos in allTiles)
            {
                var mask = GetTileBitmask(floorPos, allTiles);
                if ((mask & TileDirection.Bottom) == 0)
                    GlueTiles(floorPos, allTiles, tilesToAdd, yRange, DirectionHelper.Down);
                if ((mask & TileDirection.Left) == 0)
                    GlueTiles(floorPos, allTiles, tilesToAdd, xRange, DirectionHelper.Left);
                if ((mask & TileDirection.Top) == 0)
                    GlueTiles(floorPos, allTiles, tilesToAdd, yRange, DirectionHelper.Up);
                if ((mask & TileDirection.Right) == 0)
                    GlueTiles(floorPos, allTiles, tilesToAdd, xRange, DirectionHelper.Right);
            }

            foreach (var tile in tilesToAdd)
                if (!allTiles.Contains(tile))
                    allTiles.Add(tile);
        }

        static void GlueTiles(Vector2 position, List<Vector2> allPositions, List<Vector2> currentList, int range, Vector2 direction)
        {
            bool connectionFound = false;
            for (int i = range; i > 0; i--)
            {
                var testPos = position + (direction * 16 * i);

                if (allPositions.Contains(testPos))
                    connectionFound = true;

                if (connectionFound)
                {
                    if (!currentList.Contains(testPos))
                        currentList.Add(testPos);
                }
            }
        }

        public static List<CorridorRenderer> PaintCorridorTiles(List<Vector2> floorPositions, List<Vector2> reservedPositions, TmxTileset tileset)
        {
            var allTilesForMask = floorPositions.Concat(reservedPositions).ToList();

            Dictionary<Vector2, SingleTile> backTiles = new Dictionary<Vector2, SingleTile>();
            Dictionary<Vector2, SingleTile> frontTiles = new Dictionary<Vector2, SingleTile>();

            //foreach (var pos in reservedPositions)
            //{
            //    var r = Game1.Scene.CreateEntity("", pos).AddComponent(new PrototypeSpriteRenderer(2, 2));
            //    r.SetColor(Color.Red);
            //}

            foreach (var floorPos in floorPositions)
            {
                //Game1.Scene.CreateEntity("", floorPos).AddComponent(new PrototypeSpriteRenderer(2, 2));

                var orientation = GetTileOrientation(floorPos, allTilesForMask);
                var mask = GetTileBitmask(floorPos, allTilesForMask);

                switch (orientation)
                {
                    case TileOrientation.Center:
                        //floor
                        backTiles[floorPos] = new SingleTile(Tiles.Forge.Floor.StoneCenter);
                        break;
                    case TileOrientation.TopLeft:
                        //floor
                        backTiles[floorPos] = new SingleTile(Tiles.Forge.Floor.StoneTopLeftCorner);

                        //walls
                        var lowerWall = Tiles.Forge.Walls.LeftCornerLower;
                        var midWall = Tiles.Forge.Walls.LeftCornerMid;
                        var topWall = Tiles.Forge.Walls.LeftCornerTop;
                        backTiles[floorPos + (DirectionHelper.Up * 16 * 3) + (DirectionHelper.Left * 16)] = new SingleTile(Tiles.Forge.Walls.LeftSideRightTurn, true);
                        for (int i = 0; i < 3; i++)
                            if (!backTiles.ContainsKey(floorPos + (DirectionHelper.Left * 16) + (DirectionHelper.Up * 16 * i)))
                                backTiles[floorPos + (DirectionHelper.Left * 16) + (DirectionHelper.Up * 16 * i)] = new SingleTile(Tiles.Forge.Walls.LeftSide, true);
                        if ((mask & TileDirection.TopRight) == 0)
                        {
                            backTiles[floorPos + (DirectionHelper.Up * 16)] = new SingleTile(lowerWall, true);
                            backTiles[floorPos + (DirectionHelper.Up * 16 * 2)] = new SingleTile(midWall, true);
                            backTiles[floorPos + (DirectionHelper.Up * 16 * 3)] = new SingleTile(topWall, true);
                        }
                        break;
                    case TileOrientation.TopRight:
                        //floor
                        backTiles[floorPos] = new SingleTile(Tiles.Forge.Floor.StoneTopRightCorner);

                        //walls
                        var topRightLowerWall = Tiles.Forge.Walls.RightCornerLower;
                        var topRightMidWall = Tiles.Forge.Walls.RightCornerMid;
                        var topRightTopWall = Tiles.Forge.Walls.RightCornerTop;
                        backTiles[floorPos + (DirectionHelper.Up * 16 * 3) + (DirectionHelper.Right * 16)] = new SingleTile(Tiles.Forge.Walls.RightSideLeftTurn, true);
                        for (int i = 0; i < 3; i++)
                            if (!backTiles.ContainsKey(floorPos + (DirectionHelper.Right * 16) + (DirectionHelper.Up * 16 * i)))
                                backTiles[floorPos + (DirectionHelper.Right * 16) + (DirectionHelper.Up * 16 * i)] = new SingleTile(Tiles.Forge.Walls.RightSide, true);
                        if ((mask & TileDirection.TopLeft) == 0)
                        {
                            backTiles[floorPos + (DirectionHelper.Up * 16)] = new SingleTile(topRightLowerWall, true);
                            backTiles[floorPos + (DirectionHelper.Up * 16 * 2)] = new SingleTile(topRightMidWall, true);
                            backTiles[floorPos + (DirectionHelper.Up * 16 * 3)] = new SingleTile(topRightTopWall, true);
                        }
                        break;
                    case TileOrientation.BottomRight:
                        //floor
                        backTiles[floorPos] = new SingleTile(Tiles.Forge.Floor.StoneBottomRightCorner);

                        //front
                        if ((mask & (TileDirection.BottomLeft | TileDirection.BottomRight)) == 0)
                            frontTiles[floorPos] = new SingleTile(Tiles.Forge.Walls.Bottom);

                        //walls
                        if (!backTiles.ContainsKey(floorPos + (DirectionHelper.Down * 16)))
                            backTiles[floorPos + (DirectionHelper.Down * 16)] = new SingleTile(Tiles.Forge.Walls.Collider, true);
                        backTiles[floorPos + (DirectionHelper.Right * 16)] = new SingleTile(Tiles.Forge.Walls.BottomRightSideTurnLeft, true);
                        backTiles[floorPos + (DirectionHelper.DownRight * 16)] = new SingleTile(Tiles.Forge.Walls.Collider, true);
                        break;
                    case TileOrientation.BottomLeft:
                        //floor
                        backTiles[floorPos] = new SingleTile(Tiles.Forge.Floor.StoneBottomLeftCorner);

                        //front
                        if ((mask & (TileDirection.BottomLeft | TileDirection.BottomRight)) == 0)
                            frontTiles[floorPos] = new SingleTile(Tiles.Forge.Walls.Bottom);

                        //walls
                        if (!backTiles.ContainsKey(floorPos + (DirectionHelper.Down * 16)))
                            backTiles[floorPos + (DirectionHelper.Down * 16)] = new SingleTile(Tiles.Forge.Walls.Collider, true);
                        backTiles[floorPos + (DirectionHelper.Left * 16)] = new SingleTile(Tiles.Forge.Walls.BottomLeftSideTurnRight, true);
                        backTiles[floorPos + (DirectionHelper.DownLeft * 16)] = new SingleTile(Tiles.Forge.Walls.Collider, true);
                        break;
                    case TileOrientation.TopEdge:
                        //floor
                        backTiles[floorPos] = new SingleTile(Tiles.Forge.Floor.StoneTopEdge);

                        //walls
                        if ((mask & (TileDirection.TopLeft | TileDirection.TopRight)) == 0)
                        {
                            backTiles[floorPos + (DirectionHelper.Up * 16)] = new SingleTile(Tiles.Forge.Walls.NormalLower, true);
                            backTiles[floorPos + (DirectionHelper.Up * 16 * 2)] = new SingleTile(Tiles.Forge.Walls.NormalMid, true);
                            backTiles[floorPos + (DirectionHelper.Up * 16 * 3)] = new SingleTile(Tiles.Forge.Walls.NormalTop, true);
                        }
                        break;
                    case TileOrientation.RightEdge:
                        //floor
                        backTiles[floorPos] = new SingleTile(Tiles.Forge.Floor.StoneRightEdge);

                        //walls
                        if (!backTiles.ContainsKey(floorPos + (DirectionHelper.Right * 16)))
                            backTiles[floorPos + (DirectionHelper.Right * 16)] = new SingleTile(Tiles.Forge.Walls.RightSide, true);
                        break;
                    case TileOrientation.BottomEdge:
                        //floor
                        backTiles[floorPos] = new SingleTile(Tiles.Forge.Floor.StoneBottomEdge);

                        if ((mask & (TileDirection.BottomLeft | TileDirection.BottomRight)) == 0)
                        {
                            //front
                            frontTiles[floorPos] = new SingleTile(Tiles.Forge.Walls.Bottom);

                            //walls
                            backTiles[floorPos + (DirectionHelper.Down * 16)] = new SingleTile(Tiles.Forge.Walls.Collider, true);
                        }
                        break;
                    case TileOrientation.LeftEdge:
                        //floor
                        backTiles[floorPos] = new SingleTile(Tiles.Forge.Floor.StoneLeftEdge);

                        //walls
                        if (!backTiles.ContainsKey(floorPos + (DirectionHelper.Left * 16)))
                            backTiles[floorPos + (DirectionHelper.Left * 16)] = new SingleTile(Tiles.Forge.Walls.LeftSide, true);
                        break;
                    case TileOrientation.BottomRightInverse:
                        //floor
                        backTiles[floorPos] = new SingleTile(Tiles.Forge.Floor.StoneBottomRightInverse);

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
                        backTiles[floorPos + bottomRightInverseOffset + (DirectionHelper.Up * 16)] = new SingleTile(botRightInverseLowerWall, true);
                        backTiles[floorPos + bottomRightInverseOffset + (DirectionHelper.Up * 16 * 2)] = new SingleTile(botRightInverseMidWall, true);
                        backTiles[floorPos + bottomRightInverseOffset + (DirectionHelper.Up * 16 * 3)] = new SingleTile(botRightInverseTopWall, true);
                        break;
                    case TileOrientation.BottomLeftInverse:
                        //floor
                        backTiles[floorPos] = new SingleTile(Tiles.Forge.Floor.StoneBottomLeftInverse);

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
                        backTiles[floorPos + botLeftInverseOffset + (DirectionHelper.Up * 16)] = new SingleTile(botLeftInverseLowerWall, true);
                        backTiles[floorPos + botLeftInverseOffset + (DirectionHelper.Up * 16 * 2)] = new SingleTile(botLeftInverseMidWall, true);
                        backTiles[floorPos + botLeftInverseOffset + (DirectionHelper.Up * 16 * 3)] = new SingleTile(botLeftInverseTopWall, true);
                        break;
                    case TileOrientation.TopLeftInverse:
                        //floor
                        backTiles[floorPos] = new SingleTile(Tiles.Forge.Floor.StoneTopLeftInverse);

                        //front
                        frontTiles[floorPos + (DirectionHelper.Right * 16)] = new SingleTile(Tiles.Forge.Walls.BottomTurnLeft);
                        break;
                    case TileOrientation.TopRightInverse:
                        //floor
                        backTiles[floorPos] = new SingleTile(Tiles.Forge.Floor.StoneTopRightInverse);

                        //front
                        frontTiles[floorPos + (DirectionHelper.Left * 16)] = new SingleTile(Tiles.Forge.Walls.BottomTurnRight);
                        break;
                }
            }

            var corridorRenderers = new List<CorridorRenderer>();

            var tileRenderer = new CorridorRenderer(tileset, backTiles, true);
            tileRenderer.RenderLayer = RenderLayers.Back;
            corridorRenderers.Add(tileRenderer);

            var frontRenderer = new CorridorRenderer(tileset, frontTiles);
            frontRenderer.RenderLayer = RenderLayers.Front;
            corridorRenderers.Add(frontRenderer);

            return corridorRenderers;
        }
    }
}
