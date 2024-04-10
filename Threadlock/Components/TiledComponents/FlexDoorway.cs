using Microsoft.Xna.Framework;
using Nez;
using Nez.Tiled;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Helpers;
using Threadlock.SceneComponents.Dungenerator;
using static Threadlock.SceneComponents.Dungenerator.CorridorPainter;

namespace Threadlock.Components.TiledComponents
{
    public class FlexDoorway : TiledComponent
    {
        Dictionary<TileOrientation, int> _wallDict = new Dictionary<TileOrientation, int>()
        {
            {TileOrientation.TopLeft, 1044 },
            {TileOrientation.TopEdge, 1042 },
            { TileOrientation.Center, 946 },
            {TileOrientation.BottomLeft, 969 },
            {TileOrientation.BottomEdge, 967 },
            {TileOrientation.BottomRight, 966 },
            {TileOrientation.TopRight, 1041 },
            {TileOrientation.RightEdge, 991 },
            {TileOrientation.LeftEdge, 994 }
        };

        public void GetTiles()
        {
            var aboveFrontLayer = ParentMap.TileLayers.FirstOrDefault(l => l.Name.StartsWith("AboveFront"));

            var rect = new Rectangle((int)TmxObject.X, (int)TmxObject.Y, (int)TmxObject.Width, (int)TmxObject.Height);

            //get grid space tile coordinates
            List<Vector2> floorTiles = new List<Vector2>();
            Dictionary<Vector2, TmxLayerTile> rectWallTiles = new Dictionary<Vector2, TmxLayerTile>();
            List<TmxLayerTile> wallTiles = new List<TmxLayerTile>();
            for (int x = (int)TmxObject.X; x < TmxObject.X + TmxObject.Width; x += ParentMap.TileWidth)
            {
                for (int y = (int)TmxObject.Y; y < TmxObject.Y + TmxObject.Height; y+= ParentMap.TileHeight)
                {
                    var tileX = x / ParentMap.TileWidth;
                    var tileY = y / ParentMap.TileHeight;

                    if (x == TmxObject.X || x == TmxObject.X + TmxObject.Width - ParentMap.TileWidth)
                    {
                        TmxLayerTile baseTile = null;
                        if (aboveFrontLayer != null)
                        {
                            baseTile = aboveFrontLayer.GetTile(tileX, tileY);

                            foreach (var dir in DirectionHelper.PrincipleDirections)
                            {
                                var pos = new Point(tileX, tileY) + dir.ToPoint();
                                var adjustedPos = new Vector2(pos.X * ParentMap.TileWidth, pos.Y * ParentMap.TileHeight);
                                if (rect.Contains(adjustedPos))
                                    continue;

                                var tile = aboveFrontLayer.GetTile(pos.X, pos.Y);
                                if (tile != null)
                                    wallTiles.Add(tile);
                            }
                        }

                        var worldPos = new Vector2(tileX * ParentMap.TileWidth, tileY * ParentMap.TileHeight);

                        rectWallTiles.Add(worldPos, baseTile);
                    }
                    else
                        floorTiles.Add(new Vector2(tileX, tileY));
                }
            }

            Dictionary<Vector2, TmxLayerTile> combinedTiles = new Dictionary<Vector2, TmxLayerTile>(rectWallTiles);

            foreach (var tile in wallTiles)
            {
                var pos = new Vector2(tile.X * tile.Tileset.TileWidth, tile.Y * tile.Tileset.TileHeight);
                combinedTiles[pos] = tile;
            }

            foreach (var tile in rectWallTiles)
            {
                var orientation = CorridorPainter.GetTileOrientation(tile.Key, combinedTiles.Keys.ToList());
                if (_wallDict.TryGetValue(orientation, out var tileId))
                {
                    if (tile.Value != null)
                    {
                        tile.Value.Gid = tileId + tile.Value.Tileset.FirstGid;
                    }
                }
            }

            //List<TmxLayerTile> tiles = new List<TmxLayerTile>();
            //foreach (var layer in ParentMap.TileLayers)
            //{
            //    if (layer.Name.StartsWith("AboveFront"))
            //    {
            //        List<TmxLayerTile> layerTiles = new List<TmxLayerTile>();
            //        foreach (var wallTile in rectWallTiles)
            //        {
            //            foreach (var dir in DirectionHelper.PrincipleDirections)
            //            {
            //                var pos = (wallTile + dir).ToPoint();
            //                var layerTile = layer.GetTile(pos.X, pos.Y);
            //                if (layerTile != null)
            //                    layerTiles.Add(layerTile);
            //            }
            //            var tile = layer.GetTile((int)wallTile.X, (int)wallTile.Y);
            //        }
            //    }
            //    foreach (var rectTile in floorTiles)
            //    {
            //        var tile = layer.GetTile((int)rectTile.X, (int)rectTile.Y);
            //        if (tile != null)
            //            tiles.Add(tile);
            //    }
            //}

            //return tiles;
        }
    }
}
