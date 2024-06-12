using Microsoft.Xna.Framework;
using Nez;
using Nez.Tiled;
using System;
using System.Collections.Generic;
using System.Linq;
using Threadlock.Components;
using Threadlock.Components.TiledComponents;
using Threadlock.Helpers;
using Threadlock.Models;
using Threadlock.SceneComponents;
using Threadlock.StaticData;
using Threadlock.UI.Canvases;
using static Threadlock.SceneComponents.Dungenerator.CorridorPainter;
using static Threadlock.StaticData.Terrains;
using DoorwayPoint = Threadlock.Components.TiledComponents.DoorwayPoint;

namespace Threadlock.Scenes
{
    public class ForestTest : BaseScene
    {
        public override Color SceneColor => new Color(12, 56, 33);
        public override Color AmbientLightColor => new Color(150, 150, 150);

        const int _pathWidth = 5;
        int _halfWidth { get => _pathWidth / 2; }

        readonly Dictionary<Corners, List<Tuple<Vector2, Corners>>> _matchingCornersDict = new Dictionary<Corners, List<Tuple<Vector2, Corners>>>()
        {
            [Corners.TopLeft] = new List<Tuple<Vector2, Corners>>()
            {
                new Tuple<Vector2, Corners>(DirectionHelper.Left, Corners.TopRight),
                new Tuple<Vector2, Corners>(DirectionHelper.Up, Corners.BottomLeft),
                new Tuple<Vector2, Corners>(DirectionHelper.UpLeft, Corners.BottomRight),
            },
            [Corners.TopRight] = new List<Tuple<Vector2, Corners>>()
            {
                new Tuple<Vector2, Corners>(DirectionHelper.Right, Corners.TopLeft),
                new Tuple<Vector2, Corners>(DirectionHelper.Up, Corners.BottomRight),
                new Tuple<Vector2, Corners>(DirectionHelper.UpRight, Corners.BottomLeft),
            },
            [Corners.BottomLeft] = new List<Tuple<Vector2, Corners>>()
            {
                new Tuple<Vector2, Corners>(DirectionHelper.Left, Corners.BottomRight),
                new Tuple<Vector2, Corners>(DirectionHelper.Down, Corners.TopLeft),
                new Tuple<Vector2, Corners>(DirectionHelper.DownLeft, Corners.TopRight),
            },
            [Corners.BottomRight] = new List<Tuple<Vector2, Corners>>()
            {
                new Tuple<Vector2, Corners>(DirectionHelper.Right, Corners.BottomLeft),
                new Tuple<Vector2, Corners>(DirectionHelper.Down, Corners.TopRight),
                new Tuple<Vector2, Corners>(DirectionHelper.DownRight, Corners.TopLeft),
            },
        };

        public override void Initialize()
        {
            base.Initialize();

            var map = Content.LoadTiledMap(Nez.Content.Tiled.Tilemaps.FairyForest.Forest_test_2);
            var mapEntity = CreateEntity("map");
            TiledHelper.SetupMap(mapEntity, map);

            var ui = CreateEntity("ui").AddComponent(new CombatUI());
        }

        public override void OnStart()
        {
            base.OnStart();

            var playerSpawner = AddSceneComponent(new PlayerSpawner());
            var player = playerSpawner.SpawnPlayer();

            var followCam = Camera.AddComponent(new CustomFollowCamera(player));

            HandlePoint();
        }

        void HandlePoint()
        {
            //find doorway point
            var doorwayPoint = FindComponentOfType<DoorwayPoint>();
            if (doorwayPoint == null) return;

            var currentPos = doorwayPoint.Entity.Position;
            var pathPoints = new List<Vector2>() { currentPos };
            for (int i = 0; i < Nez.Random.Range(10, 10); i++)
            {
                currentPos += new Vector2(16, 0);
                pathPoints.Add(currentPos);
            }
            for (int i = 0; i < Nez.Random.Range(9, 9); i++)
            {
                currentPos += new Vector2(0, -16);
                pathPoints.Add(currentPos);
            }

            //remove the first few points, they'll be added in the larger path
            pathPoints.RemoveRange(0, _halfWidth);

            //get the expanded path
            var pathDict = GetLargerPath(pathPoints, _pathWidth);

            //place actual tiles
            PaintTiles(pathDict);
        }

        public static List<TileInfo<ForestFloor>> GetLargerPath(List<Vector2> positions, int size)
        {
            var largerPath = new Dictionary<Vector2, TileInfo<ForestFloor>>();
            var halfWidth = size / 2;
            for (int i = 0; i < positions.Count; i++)
            {
                for (int x = -halfWidth; x <= halfWidth; x++)
                {
                    for (int y = -halfWidth; y <= halfWidth; y++)
                    {
                        var tileType = (x == 0 && y == 0) ? ForestFloor.Dirt : ForestFloor.DarkGrass;
                        var priority = tileType == ForestFloor.Dirt ? 1 : 0;
                        var pos = new Vector2(x * 16, y * 16) + positions[i];

                        if (!largerPath.TryGetValue(pos, out var existingTile) || existingTile.Priority < priority)
                            largerPath[pos] = new TileInfo<ForestFloor>(pos, tileType, priority);
                    }
                }
            }

            return largerPath.Values.ToList();
        }

        /// <summary>
        /// for a specific renderer, get tiles on a certain layer. Remove tiles that would overlap those in the existing path
        /// </summary>
        /// <param name="renderer"></param>
        /// <param name="layerName"></param>
        /// <param name="pathTiles"></param>
        /// <returns></returns>
        List<Tuple<Vector2, TmxLayerTile>> HandleRenderer<TEnum>(TiledMapRenderer renderer, string layerName, List<TileInfo<TEnum>> pathTiles) where TEnum : struct, Enum
        {
            List<Tuple<Vector2, TmxLayerTile>> tiles = new List<Tuple<Vector2, TmxLayerTile>>();

            if (string.IsNullOrWhiteSpace(layerName))
                return tiles;

            var layers = renderer.TiledMap.TileLayers.Where(l => l.Name.StartsWith(layerName));

            foreach (var layer in layers)
            {
                var layerTiles = TiledHelper.GetLayerTilesWithPositions(layer);
                foreach (var layerTile in layerTiles)
                {
                    var tile = layerTile.Item2;
                    var tilePos = layerTile.Item1;

                    if (pathTiles.Any(t => t.Position == new Vector2(tilePos.X * tile.Tileset.TileWidth, tilePos.Y * tile.Tileset.TileHeight)))
                        layer.RemoveTile((int)tilePos.X, (int)tilePos.Y);
                    else
                        tiles.Add(layerTile);
                }
            }

            return tiles;
        }

        void PaintTiles<TEnum>(List<TileInfo<TEnum>> path) where TEnum : struct, Enum
        {
            List<Tuple<Vector2, TmxLayerTile>> backTiles = new List<Tuple<Vector2, TmxLayerTile>>();
            List<Tuple<Vector2, TmxLayerTile>> wallTiles = new List<Tuple<Vector2, TmxLayerTile>>();
            List<Tuple<Vector2, TmxLayerTile>> aboveFrontTiles = new List<Tuple<Vector2, TmxLayerTile>>();

            //get tiles in this map by layer
            var renderers = FindComponentsOfType<TiledMapRenderer>();
            foreach (var renderer in renderers)
            {
                if (renderer.RenderLayer == RenderLayers.Back)
                    backTiles.AddRange(HandleRenderer(renderer, "Back", path));
                if (renderer.RenderLayer == RenderLayers.Walls)
                    wallTiles.AddRange(HandleRenderer(renderer, "Walls", path));
                if (renderer.RenderLayer == RenderLayers.AboveFront)
                    aboveFrontTiles.AddRange(HandleRenderer(renderer, "AboveFront", path));
            }

            //filter tiles
            backTiles = backTiles.Distinct().ToList();
            wallTiles = wallTiles.Distinct().ToList();
            aboveFrontTiles = aboveFrontTiles.Distinct().ToList();

            //open tileset
            var tileset = TiledHelper.GetTileset(Nez.Content.Tiled.Tilesets.Fairy_forest_tileset);
            //var backTerrainSet = tileset.TerrainSets.FirstOrDefault(t => t.EnumType == typeof(TEnum));
            //var wallTerrainSet = tileset.TerrainSets.FirstOrDefault(t => t.EnumType == typeof(WallTileType));

            //convert back tiles to list of TileInfo
            var backTileInfo = new List<TileInfo<TEnum>>();
            if (tileset.TryGetTerrainSet(typeof(TEnum), out var terrainSet))
            {
                foreach (var tileTuple in backTiles)
                {
                    var tile = tileTuple.Item2;
                    var tilePos = tileTuple.Item1;

                    //find mask for this tile if it exists in the tileset terrain dictionary
                    if (terrainSet.TryGetMask(tile.Gid - tile.Tileset.FirstGid, out var mask))
                    {
                        var pos = new Vector2(tilePos.X * tile.Tileset.TileWidth, tilePos.Y * tile.Tileset.TileHeight);
                        var tileInfo = new TileInfo<TEnum>(pos, tile.Gid - tile.Tileset.FirstGid, mask);
                        backTileInfo.Add(tileInfo);
                    }
                }
            }

            //get combined list of path tiles and pre existing Back tiles
            var joinedTiles = path.Concat(backTileInfo).ToList();

            //order path by priority
            path = path.OrderBy(t => t.Priority).ToList();

            //init list to keep track of where we'll need walls
            var wallPositions = new List<Vector2>();

            for (int i = 0; i < path.Count; i++)
            {
                var pathTile = path[i];

                //get the mask of the surrounding terrain
                pathTile.PositionalMask = TileBitmaskHelper.GetPositionalMask(pathTile, joinedTiles);

                //try to find a tile that matches the mask
                if (!tileset.TryFindTile(typeof(TEnum), pathTile.CombinedMask, out var tileId))
                    continue;

                //update tile id and terrain mask
                pathTile.TileId = tileId;
                pathTile.TerrainMask = pathTile.CombinedMask;

                //determine where we need walls
                foreach (var direction in DirectionHelper.PrincipleDirections)
                {
                    var targetPos = pathTile.Position + (direction * 16);

                    if (!joinedTiles.Any(t => t.Position == targetPos))
                    {
                        if (!wallPositions.Contains(targetPos))
                        {
                            wallPositions.Add(targetPos);
                        } 
                    }
                }

                //loop through corners
                foreach (Corners corner in Enum.GetValues(typeof(Corners)))
                {
                    //update neighbors
                    var terrainType = TileBitmaskHelper.GetMaskInCorner<TEnum>(pathTile.TerrainMask, corner);
                    var dirsToHandle = _matchingCornersDict[corner];
                    foreach (var kvp in dirsToHandle)
                    {
                        var dir = kvp.Item1;
                        var matchingCorner = kvp.Item2;
                        
                        var neighborPos = pathTile.Position + (dir * 16);
                        var neighbor = path.FirstOrDefault(p => p.Position == neighborPos);
                        if (neighbor != null && neighbor.Priority <= pathTile.Priority)
                        {
                            neighbor.SetTerrainMaskValue(terrainType, matchingCorner);

                            //try to replace neighbor tile
                            if (tileset.TryFindTile(typeof(TEnum), neighbor.TerrainMask, out var neighborTileId))
                                neighbor.TileId = neighborTileId;
                        }
                    }
                }
            }

            //convert wall tiles to list of TileInfo
            var wallTileInfo = new List<TileInfo<WallTileType>>();
            if (tileset.TryGetTerrainSet(typeof(WallTileType), out var wallTerrainSet))
            {
                foreach (var tileTuple in wallTiles)
                {
                    var tile = tileTuple.Item2;
                    var tilePos = tileTuple.Item1;

                    //find mask for this tile if it exists in the tileset terrain dictionary
                    if (wallTerrainSet.TryGetMask(tile.Gid, out var mask))
                    {
                        var pos = new Vector2(tilePos.X * tile.Tileset.TileWidth, tilePos.Y * tile.Tileset.TileHeight);
                        var tileInfo = new TileInfo<WallTileType>(pos, tile.Gid, mask);
                        wallTileInfo.Add(tileInfo);
                    }
                }
            }

            //create list of tile info out of new wall positions
            List<TileInfo<WallTileType>> newWallsInfo = new List<TileInfo<WallTileType>>();
            foreach (var wallPos in wallPositions)
            {
                var tileInfo = new TileInfo<WallTileType>(wallPos, WallTileType.Wall, 0);

                //get positional mask for this wall tile
                var posMask = 0;
                foreach (Corners corner in Enum.GetValues(typeof(Corners)))
                {
                    var dir = TileBitmaskHelper.CornerDirectionDict[corner];
                    var pos = wallPos + (dir * 16);

                    WallTileType wallTileType = WallTileType.None;
                    if (joinedTiles.Any(t => t.Position == pos))
                        wallTileType = WallTileType.Floor;
                    else if (wallPositions.Contains(pos) || wallTileInfo.Any(t => t.Position == pos))
                        wallTileType = WallTileType.Wall;

                    posMask |= ((int)wallTileType << (int)corner * 2);
                }

                //update positional mask value
                tileInfo.PositionalMask = posMask;

                //find matching tile id
                if (tileset.TryFindTile(typeof(WallTileType), posMask, out var wallTileId))
                    tileInfo.TileId = wallTileId;
                else if (tileset.TryFindTile(typeof(WallTileType), tileInfo.CombinedMask, out wallTileId))
                    tileInfo.TileId = wallTileId;
                else
                    continue;

                newWallsInfo.Add(tileInfo);
            }

            //handle extra tiles
            Dictionary<Vector2, ExtraTile> extraTileDict = new Dictionary<Vector2, ExtraTile>();
            foreach (var tile in path)
            {
                if (tileset.ExtraTileDict.TryGetValue(tile.TileId.Value, out var extraTiles))
                {
                    var groupedTiles = extraTiles
                        .GroupBy(t => t.Offset)
                        .Select(g =>
                        {
                            int index = Nez.Random.NextInt(g.Count());
                            return g.ElementAt(index);
                        });

                    foreach (var extraTile in groupedTiles)
                    {
                        var pos = tile.Position + (extraTile.Offset * 16);
                        extraTileDict.TryAdd(pos, extraTile);
                    }
                }
            }
            foreach (var tile in newWallsInfo)
            {
                if (tileset.ExtraTileDict.TryGetValue(tile.TileId.Value, out var extraTiles))
                {
                    var groupedTiles = extraTiles
                        .GroupBy(t => t.Offset)
                        .Select(g =>
                        {
                            int index = Nez.Random.NextInt(g.Count());
                            return g.ElementAt(index);
                        });

                    foreach (var extraTile in groupedTiles)
                    {
                        var pos = tile.Position + (extraTile.Offset * 16);
                        extraTileDict.TryAdd(pos, extraTile);
                    }
                }
            }

            //convert back tiles to single tiles
            var backTileDict = new Dictionary<Vector2, SingleTile>();
            foreach (var pathTile in path)
            {
                if (pathTile.TileId != null && pathTile.TileId >= 0)
                {
                    var singleTile = new SingleTile(pathTile.TileId.Value, false);
                    backTileDict.Add(pathTile.Position, singleTile);
                }
            }

            //convert wall tiles to single tiles
            var wallTileDict = new Dictionary<Vector2, SingleTile>();
            foreach (var wallTile in newWallsInfo)
            {
                if (wallTile.TileId != null && wallTile.TileId >= 0)
                {
                    var singleTile = new SingleTile(wallTile.TileId.Value, true);
                    wallTileDict.Add(wallTile.Position, singleTile);
                }
            }

            var aboveFrontDict = new Dictionary<Vector2, SingleTile>();
            foreach (var pair in extraTileDict)
            {
                switch (pair.Value.RenderLayer)
                {
                    case RenderLayers.Back:
                        backTileDict.TryAdd(pair.Key, new SingleTile(pair.Value.TileId));
                        break;
                    case RenderLayers.Walls:
                        wallTileDict.TryAdd(pair.Key, new SingleTile(pair.Value.TileId));
                        break;
                    case RenderLayers.AboveFront:
                        aboveFrontDict.TryAdd(pair.Key, new SingleTile(pair.Value.TileId));
                        break;
                }
            }

            //make corridor renderers
            var corridorRenderer = CreateEntity("").AddComponent(new CorridorRenderer(tileset.Tileset, backTileDict));
            corridorRenderer.SetRenderLayer(RenderLayers.Back);
            var wallRenderer = CreateEntity("").AddComponent(new CorridorRenderer(tileset.Tileset, wallTileDict, true));
            wallRenderer.SetRenderLayer(RenderLayers.AboveFront); //for the forest, walls should be AboveFront
            var aboveFrontRenderer = CreateEntity("").AddComponent(new CorridorRenderer(tileset.Tileset, aboveFrontDict));
            aboveFrontRenderer.SetRenderLayer(RenderLayers.AboveFront);
        }
    }
}
