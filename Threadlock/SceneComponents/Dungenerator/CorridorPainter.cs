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
                Direction mask = Direction.None;
                if (positions.Contains(pos + DirectionHelper.Up * 16))
                    mask |= Direction.Top;
                if (positions.Contains(pos + DirectionHelper.Down * 16))
                    mask |= Direction.Bottom;
                if (positions.Contains(pos + DirectionHelper.Left * 16))
                    mask |= Direction.Left;
                if (positions.Contains(pos + DirectionHelper.Right * 16))
                    mask |= Direction.Right;
                if (positions.Contains(pos + DirectionHelper.UpLeft * 16))
                    mask |= Direction.TopLeft;
                if (positions.Contains(pos + DirectionHelper.UpRight * 16))
                    mask |= Direction.TopRight;
                if (positions.Contains(pos + DirectionHelper.DownLeft * 16))
                    mask |= Direction.BottomLeft;
                if (positions.Contains(pos + DirectionHelper.DownRight * 16))
                    mask |= Direction.BottomRight;

                //center tile
                if (mask == (Direction.Top | Direction.Bottom | Direction.Left | Direction.Right))
                    tile = tileset.TileRegions[202];
                //top left corner
                else if ((mask & (Direction.Bottom | Direction.Right)) != 0
                    && (mask & (Direction.Top | Direction.Left)) == 0)
                    tile = tileset.TileRegions[176];
                //top right corner
                else if ((mask & (Direction.Bottom | Direction.Left)) != 0
                    && (mask & (Direction.Top | Direction.Right)) == 0)
                    tile = tileset.TileRegions[178];
                //bottom right corner
                else if ((mask & (Direction.Top | Direction.Left)) != 0
                    && (mask & (Direction.Bottom | Direction.Right)) == 0)
                    tile = tileset.TileRegions[228];
                //bottom left corner
                else if ((mask & (Direction.Top | Direction.Right)) != 0
                    && (mask & (Direction.Bottom | Direction.Left)) == 0)
                    tile = tileset.TileRegions[226];
                //top edge
                else if ((mask & (Direction.Bottom | Direction.Left | Direction.Right)) != 0
                    && (mask & Direction.Top) == 0)
                    tile = tileset.TileRegions[177];
                //right edge
                else if ((mask & (Direction.Left | Direction.Top | Direction.Bottom)) != 0
                    && (mask & Direction.Right) == 0)
                    tile = tileset.TileRegions[203];
                //bottom edge
                else if ((mask & (Direction.Top | Direction.Left | Direction.Right)) != 0
                    && (mask & Direction.Bottom) == 0)
                    tile = tileset.TileRegions[227];
                //left edge
                else if ((mask & (Direction.Right | Direction.Top | Direction.Bottom)) != 0
                    && (mask & Direction.Left) == 0)
                    tile = tileset.TileRegions[201];
                //bottom right inverse corner
                else if ((mask & (Direction.Left | Direction.Top)) != 0
                    && (mask & Direction.TopLeft) == 0)
                    tile = tileset.TileRegions[231];
                //bottom left inverse corner
                else if ((mask & (Direction.Right | Direction.Top)) != 0
                    && (mask & Direction.TopRight) == 0)
                    tile = tileset.TileRegions[229];
                //top left inverse corner
                else if ((mask & (Direction.Right | Direction.Bottom)) != 0
                    && (mask & Direction.BottomRight) == 0)
                    tile = tileset.TileRegions[179];
                //top right inverse corner
                else if ((mask & (Direction.Left | Direction.Bottom)) != 0
                    && (mask & Direction.BottomLeft) == 0)
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
