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
                var mask = GetTileBitmask(floorPos, allTilesForMask);

                //center tile
                if (mask == (TileDirection.Top | TileDirection.Bottom | TileDirection.Left | TileDirection.Right | TileDirection.TopLeft | TileDirection.TopRight | TileDirection.BottomLeft | TileDirection.BottomRight))
                    tileDictionary[floorPos] = new SingleTileRenderer(tileset, Tiles.Forge.Floor.StoneCenter, RenderLayers.Back);
                    //renderers.Add(Game1.Scene.CreateEntity("", floorPos).AddComponent(new SingleTileRenderer(tileset, Tiles.Forge.Floor.StoneCenter)));
                //top left corner
                else if ((mask & (TileDirection.Bottom | TileDirection.Right)) != 0
                    && (mask & (TileDirection.Top | TileDirection.Left)) == 0)
                {
                    //floor
                    tileDictionary[floorPos] = new SingleTileRenderer(tileset, Tiles.Forge.Floor.StoneTopLeftCorner, RenderLayers.Back);

                    //walls
                    tileDictionary[floorPos + (DirectionHelper.Up * 16)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.LeftCornerLower, RenderLayers.Back);
                    tileDictionary[floorPos + (DirectionHelper.Up * 16 * 2)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.LeftCornerMid, RenderLayers.Back);
                    tileDictionary[floorPos + (DirectionHelper.Up * 16 * 3)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.LeftCornerTop, RenderLayers.Back);
                    tileDictionary[floorPos + (DirectionHelper.Up * 16 * 3) + (DirectionHelper.Left * 16)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.LeftSideRightTurn, RenderLayers.Back);
                    for (int i = 0; i < 3; i++)
                        sideWallDictionary[floorPos + (DirectionHelper.Left * 16) + (DirectionHelper.Up * 16 * i)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.LeftSide, RenderLayers.Back);
                }
                //top right corner
                else if ((mask & (TileDirection.Bottom | TileDirection.Left)) != 0
                    && (mask & (TileDirection.Top | TileDirection.Right)) == 0)
                {
                    //floor
                    tileDictionary[floorPos] = new SingleTileRenderer(tileset, Tiles.Forge.Floor.StoneTopRightCorner, RenderLayers.Back);

                    //walls
                    tileDictionary[floorPos + (DirectionHelper.Up * 16)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.RightCornerLower, RenderLayers.Back);
                    tileDictionary[floorPos + (DirectionHelper.Up * 16 * 2)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.RightCornerMid, RenderLayers.Back);
                    tileDictionary[floorPos + (DirectionHelper.Up * 16 * 3)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.RightCornerTop, RenderLayers.Back);
                    tileDictionary[floorPos + (DirectionHelper.Up * 16 * 3) + (DirectionHelper.Right * 16)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.RightSideLeftTurn, RenderLayers.Back);
                    for (int i = 0; i < 3; i++)
                        sideWallDictionary[floorPos + (DirectionHelper.Right * 16) + (DirectionHelper.Up * 16 * i)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.RightSide, RenderLayers.Back);
                }
                //bottom right corner
                else if ((mask & (TileDirection.Top | TileDirection.Left)) != 0
                    && (mask & (TileDirection.Bottom | TileDirection.Right)) == 0)
                {
                    //floor
                    tileDictionary[floorPos] = new SingleTileRenderer(tileset, Tiles.Forge.Floor.StoneBottomRightCorner, RenderLayers.Back);

                    //front
                    frontTileDictionary[floorPos] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.Bottom, RenderLayers.Front);

                    //walls
                    colliderDictionary[floorPos + (DirectionHelper.Down * 16)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.Collider, RenderLayers.Back);
                    tileDictionary[floorPos + (DirectionHelper.Right * 16)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.BottomRightSideTurnLeft, RenderLayers.Back);
                    colliderDictionary[floorPos + (DirectionHelper.DownRight * 16)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.Collider, RenderLayers.Back);
                }
                //bottom left corner
                else if ((mask & (TileDirection.Top | TileDirection.Right)) != 0
                    && (mask & (TileDirection.Bottom | TileDirection.Left)) == 0)
                {
                    //floor
                    tileDictionary[floorPos] = new SingleTileRenderer(tileset, Tiles.Forge.Floor.StoneBottomLeftCorner, RenderLayers.Back);

                    //front
                    frontTileDictionary[floorPos] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.Bottom, RenderLayers.Front);

                    //walls
                    colliderDictionary[floorPos + (DirectionHelper.Down * 16)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.Collider, RenderLayers.Back);
                    tileDictionary[floorPos + (DirectionHelper.Left * 16)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.BottomLeftSideTurnRight, RenderLayers.Back);
                    colliderDictionary[floorPos + (DirectionHelper.DownLeft * 16)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.Collider, RenderLayers.Back);
                }
                //top edge
                else if ((mask & (TileDirection.Bottom | TileDirection.Left | TileDirection.Right)) != 0
                    && (mask & TileDirection.Top) == 0)
                {
                    //floor
                    tileDictionary[floorPos] = new SingleTileRenderer(tileset, Tiles.Forge.Floor.StoneTopEdge, RenderLayers.Back);

                    //walls
                    tileDictionary[floorPos + (DirectionHelper.Up * 16)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.NormalLower, RenderLayers.Back);
                    tileDictionary[floorPos + (DirectionHelper.Up * 16 * 2)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.NormalMid, RenderLayers.Back);
                    tileDictionary[floorPos + (DirectionHelper.Up * 16 * 3)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.NormalTop, RenderLayers.Back);
                }
                //right edge
                else if ((mask & (TileDirection.Left | TileDirection.Top | TileDirection.Bottom)) != 0
                    && (mask & TileDirection.Right) == 0)
                {
                    //floor
                    tileDictionary[floorPos] = new SingleTileRenderer(tileset, Tiles.Forge.Floor.StoneRightEdge, RenderLayers.Back);

                    //walls
                    sideWallDictionary[floorPos + (DirectionHelper.Right * 16)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.RightSide, RenderLayers.Back);
                }
                //bottom edge
                else if ((mask & (TileDirection.Top | TileDirection.Left | TileDirection.Right)) != 0
                    && (mask & TileDirection.Bottom) == 0)
                {
                    //floor
                    tileDictionary[floorPos] = new SingleTileRenderer(tileset, Tiles.Forge.Floor.StoneBottomEdge, RenderLayers.Back);

                    //front
                    frontTileDictionary[floorPos] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.Bottom, RenderLayers.Front);
                    //renderers.Add(Game1.Scene.CreateEntity("", floorPos).AddComponent(new SingleTileRenderer(tileset, Tiles.Forge.Walls.Bottom)));

                    //walls
                    colliderDictionary[floorPos + (DirectionHelper.Down * 16)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.Collider, RenderLayers.Back);
                }
                //left edge
                else if ((mask & (TileDirection.Right | TileDirection.Top | TileDirection.Bottom)) != 0
                    && (mask & TileDirection.Left) == 0)
                {
                    //floor
                    tileDictionary[floorPos] = new SingleTileRenderer(tileset, Tiles.Forge.Floor.StoneLeftEdge, RenderLayers.Back);

                    //walls
                    sideWallDictionary[floorPos + (DirectionHelper.Left * 16)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.LeftSide, RenderLayers.Back);
                }
                //bottom right inverse corner
                else if ((mask & (TileDirection.Left | TileDirection.Top)) != 0
                    && (mask & TileDirection.TopLeft) == 0)
                {
                    //floor
                    tileDictionary[floorPos] = new SingleTileRenderer(tileset, Tiles.Forge.Floor.StoneBottomRightInverse, RenderLayers.Back);

                    //walls
                    var offset = (DirectionHelper.Left * 16);
                    tileDictionary[floorPos + offset + (DirectionHelper.Up * 16)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.RightEdgeLower, RenderLayers.Back);
                    tileDictionary[floorPos + offset + (DirectionHelper.Up * 16 * 2)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.RightEdgeMid, RenderLayers.Back);
                    tileDictionary[floorPos + offset + (DirectionHelper.Up * 16 * 3)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.RightEdgeTop, RenderLayers.Back);
                }
                //bottom left inverse corner
                else if ((mask & (TileDirection.Right | TileDirection.Top)) != 0
                    && (mask & TileDirection.TopRight) == 0)
                {
                    //floor
                    tileDictionary[floorPos] = new SingleTileRenderer(tileset, Tiles.Forge.Floor.StoneBottomLeftInverse, RenderLayers.Back);

                    //walls
                    var offset = (DirectionHelper.Right * 16);
                    tileDictionary[floorPos + offset + (DirectionHelper.Up * 16)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.LeftEdgeLower, RenderLayers.Back);
                    tileDictionary[floorPos + offset + (DirectionHelper.Up * 16 * 2)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.LeftEdgeMid, RenderLayers.Back);
                    tileDictionary[floorPos + offset + (DirectionHelper.Up * 16 * 3)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.LeftEdgeTop, RenderLayers.Back);
                }
                //top left inverse corner
                else if ((mask & (TileDirection.Right | TileDirection.Bottom)) != 0
                    && (mask & TileDirection.BottomRight) == 0)
                {
                    //floor
                    tileDictionary[floorPos] = new SingleTileRenderer(tileset, Tiles.Forge.Floor.StoneTopLeftInverse, RenderLayers.Back);

                    //front
                    frontTileDictionary[floorPos + (DirectionHelper.Right * 16)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.BottomTurnLeft, RenderLayers.Front);
                }
                //top right inverse corner
                else if ((mask & (TileDirection.Left | TileDirection.Bottom)) != 0
                    && (mask & TileDirection.BottomLeft) == 0)
                {
                    //floor
                    tileDictionary[floorPos] = new SingleTileRenderer(tileset, Tiles.Forge.Floor.StoneTopRightInverse, RenderLayers.Back);

                    //front
                    frontTileDictionary[floorPos + (DirectionHelper.Left * 16)] = new SingleTileRenderer(tileset, Tiles.Forge.Walls.BottomTurnRight, RenderLayers.Front);
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

        public static List<SingleTileRenderer> PaintFloorTiles(List<SingleTile> tileModels, TmxTileset tileset, DungeonRoomEntity parentRoom)
        {
            var renderers = new List<SingleTileRenderer>();

            foreach (var tileModel in tileModels)
            {
                var ent = Game1.Scene.CreateEntity("tile");
                ent.SetPosition(tileModel.Position);

                var tileRect = tileset.TileRegions[tileModel.TileId];
                var tileRenderer = ent.AddComponent(new SingleTileRenderer(tileset.Image.Texture, tileRect));
                tileRenderer.RenderLayer = RenderLayers.Back;
                renderers.Add(tileRenderer);
            }

            return renderers;
        }

        public static List<SingleTileRenderer> PaintFloorTiles(List<Vector2> positions, TmxTileset tileset, DungeonRoomEntity parentRoom)
        {
            var renderers = new List<SingleTileRenderer>();
            //var tile = tileset.TileRegions[202];
            //var tile = map.TileLayers.First().Tiles.First();
            foreach (var pos in positions)
            {
                var ent = Game1.Scene.CreateEntity("tile");
                ent.SetPosition(pos);

                RectangleF tile = tileset.TileRegions[202];
                var mask = GetTileBitmask(pos, positions);

                //center tile
                if (mask == (TileDirection.Top | TileDirection.Bottom | TileDirection.Left | TileDirection.Right))
                    tile = tileset.TileRegions[202];
                //top left corner
                else if ((mask & (TileDirection.Bottom | TileDirection.Right)) != 0
                    && (mask & (TileDirection.Top | TileDirection.Left)) == 0)
                    tile = tileset.TileRegions[176];
                //top right corner
                else if ((mask & (TileDirection.Bottom | TileDirection.Left)) != 0
                    && (mask & (TileDirection.Top | TileDirection.Right)) == 0)
                    tile = tileset.TileRegions[178];
                //bottom right corner
                else if ((mask & (TileDirection.Top | TileDirection.Left)) != 0
                    && (mask & (TileDirection.Bottom | TileDirection.Right)) == 0)
                    tile = tileset.TileRegions[228];
                //bottom left corner
                else if ((mask & (TileDirection.Top | TileDirection.Right)) != 0
                    && (mask & (TileDirection.Bottom | TileDirection.Left)) == 0)
                    tile = tileset.TileRegions[226];
                //top edge
                else if ((mask & (TileDirection.Bottom | TileDirection.Left | TileDirection.Right)) != 0
                    && (mask & TileDirection.Top) == 0)
                    tile = tileset.TileRegions[177];
                //right edge
                else if ((mask & (TileDirection.Left | TileDirection.Top | TileDirection.Bottom)) != 0
                    && (mask & TileDirection.Right) == 0)
                    tile = tileset.TileRegions[203];
                //bottom edge
                else if ((mask & (TileDirection.Top | TileDirection.Left | TileDirection.Right)) != 0
                    && (mask & TileDirection.Bottom) == 0)
                    tile = tileset.TileRegions[227];
                //left edge
                else if ((mask & (TileDirection.Right | TileDirection.Top | TileDirection.Bottom)) != 0
                    && (mask & TileDirection.Left) == 0)
                    tile = tileset.TileRegions[201];
                //bottom right inverse corner
                else if ((mask & (TileDirection.Left | TileDirection.Top)) != 0
                    && (mask & TileDirection.TopLeft) == 0)
                    tile = tileset.TileRegions[231];
                //bottom left inverse corner
                else if ((mask & (TileDirection.Right | TileDirection.Top)) != 0
                    && (mask & TileDirection.TopRight) == 0)
                    tile = tileset.TileRegions[229];
                //top left inverse corner
                else if ((mask & (TileDirection.Right | TileDirection.Bottom)) != 0
                    && (mask & TileDirection.BottomRight) == 0)
                    tile = tileset.TileRegions[179];
                //top right inverse corner
                else if ((mask & (TileDirection.Left | TileDirection.Bottom)) != 0
                    && (mask & TileDirection.BottomLeft) == 0)
                    tile = tileset.TileRegions[181];

                var tileRenderer = ent.AddComponent(new SingleTileRenderer(tileset.Image.Texture, tile));
                tileRenderer.RenderLayer = RenderLayers.Back;
                renderers.Add(tileRenderer);
            }

            return renderers;
        }

        public static void GenerateWalls(List<Vector2> floorPositions, TmxTileset tileset)
        {
            List<Vector2> wallPositions = new List<Vector2>();

            var tile = tileset.TileRegions[152];
            foreach (var pos in floorPositions)
            {
                foreach (var dir in new[] { new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1) })
                {
                    var neighborPos = pos + dir * tileset.TileWidth;
                    if (!floorPositions.Contains(neighborPos))
                        wallPositions.Add(neighborPos);
                }
            }

            foreach (var wallPos in wallPositions)
            {
                var ent = Game1.Scene.CreateEntity("wall", wallPos);
                ent.AddComponent(new SingleTileRenderer(tileset.Image.Texture, tile));
            }
        }
    }
}
