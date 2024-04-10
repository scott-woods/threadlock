using Microsoft.Xna.Framework;
using Nez;
using Nez.Persistence;
using Nez.Tiled;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Threadlock.Components;
using Threadlock.Components.TiledComponents;
using Threadlock.Helpers;
using Threadlock.Models;
using Threadlock.SaveData;
using Threadlock.SceneComponents;
using Threadlock.SceneComponents.Dungenerator;
using Threadlock.StaticData;
using static Threadlock.SceneComponents.Dungenerator.CorridorPainter;

namespace Threadlock.Scenes
{
    public class ForestTest : BaseScene
    {
        public override void Initialize()
        {
            base.Initialize();

            var map = Content.LoadTiledMap(Nez.Content.Tiled.Tilemaps.FairyForest.Forest_test_2);
            var mapEntity = CreateEntity("map");
            TiledHelper.SetupMap(mapEntity, map);
        }

        public override void OnStart()
        {
            base.OnStart();

            var playerSpawner = AddSceneComponent(new PlayerSpawner());
            var player = playerSpawner.SpawnPlayer();

            var followCam = Camera.AddComponent(new CustomFollowCamera(player));

            //HandleDoorway();

            HandlePoint();
        }

        void HandlePoint()
        {
            var doorwayPoint = FindComponentOfType<DoorwayPoint>();
            if (doorwayPoint == null) return;

            var currentPos = doorwayPoint.Entity.Position;
            var targetPos = currentPos + new Vector2(128, 0);
            var pathPoints = new List<Vector2>() { currentPos };
            
            while (currentPos != targetPos)
            {
                currentPos += new Vector2(16, 0);
                pathPoints.Add(currentPos);
            }

            var pathWidth = 5;
            var halfWidth = pathWidth / 2;

            var minX = 0;
            var minY = 0;
            var maxX = 0;
            var maxY = 0;

            switch (doorwayPoint.Direction)
            {
                case DoorwayPointDirection.Up:
                    minX -= halfWidth;
                    maxX += halfWidth;
                    minY += 1;
                    maxY += halfWidth;
                    break;
                case DoorwayPointDirection.Down:
                    minX -= halfWidth;
                    maxX += halfWidth;
                    minY -= halfWidth;
                    maxY -= 1;
                    break;
                case DoorwayPointDirection.Left:
                    minX += 1;
                    maxX += halfWidth;
                    minY -= halfWidth;
                    maxY += halfWidth;
                    break;
                case DoorwayPointDirection.Right:
                    minX -= halfWidth;
                    maxX -= 1;
                    minY -= halfWidth;
                    maxY += halfWidth;
                    break;
            }

            List<Vector2> reservedPositions = new List<Vector2>();
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    var pos = new Vector2(x * 16, y * 16) + doorwayPoint.Entity.Position;
                    reservedPositions.Add(pos);
                }
            }

            var pathDict = GetLargerPath(pathPoints, reservedPositions, pathWidth);

            var finalPath = pathDict.SelectMany(p => p.Value).Distinct().ToList();

            PaintTiles(finalPath, reservedPositions);
        }

        void HandleDoorway()
        {
            var doorway = FindComponentOfType<FlexDoorway>();
            if (doorway == null) return;

            doorway.GetTiles();
        }

        public static Dictionary<Vector2, List<Vector2>> GetLargerPath(List<Vector2> positions, List<Vector2> reservedPositions, int size)
        {
            Dictionary<Vector2, List<Vector2>> posDictionary = new Dictionary<Vector2, List<Vector2>>();
            List<Vector2> visitedPositions = new List<Vector2>();
            var halfWidth = size / 2;
            for (int i = 1; i < positions.Count + 1; i++)
            {
                posDictionary[positions[i - 1]] = new List<Vector2>();
                for (int x = -halfWidth; x <= halfWidth; x++)
                {
                    for (int y = -halfWidth; y <= halfWidth; y++)
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

        List<Vector2> HandleLayer(TiledMapRenderer renderer, int renderLayer, string layerName, List<Vector2> path)
        {
            List<Vector2> positions = new List<Vector2>();

            if (renderer.RenderLayer == renderLayer)
            {
                var layer = renderer.TiledMap.TileLayers.FirstOrDefault(l => l.Name.StartsWith(layerName));
                if (layer != null)
                {
                    var tiles = layer.Tiles.Where(t => t != null);
                    foreach (var tile in tiles)
                    {
                        //get tile pos in world space
                        var adjustedTilePos = renderer.Entity.Position + new Vector2(tile.X * renderer.TiledMap.TileWidth, tile.Y * renderer.TiledMap.TileHeight);

                        //if on the path, remove tile from the layer. it will be handled by path generation
                        if (path.Contains(adjustedTilePos))
                            layer.RemoveTile(tile.X, tile.Y);
                        else if (!positions.Contains(adjustedTilePos))
                            positions.Add(adjustedTilePos);
                    }
                }
            }

            return positions;
        }

        void PaintTiles(List<Vector2> path, List<Vector2> reservedPositions)
        {
            List<Vector2> backPositions = new List<Vector2>(path);
            List<Vector2> wallPositions = new List<Vector2>();
            List<Vector2> aboveFrontPositions = new List<Vector2>();

            var renderers = FindComponentsOfType<TiledMapRenderer>();
            foreach (var renderer in renderers)
            {
                //handle Back renderers
                backPositions.AddRange(HandleLayer(renderer, RenderLayers.Back, "Back", path));
                wallPositions.AddRange(HandleLayer(renderer, RenderLayers.Walls, "Walls", path));
                aboveFrontPositions.AddRange(HandleLayer(renderer, RenderLayers.AboveFront, "AboveFront", path));
            }

            if (File.Exists("Content/Data/FairyForestTiles2.json"))
            {
                var json = File.ReadAllText("Content/Data/FairyForestTiles2.json");
                var tileDict = Json.FromJson<Dictionary<TileOrientation, TileConfiguration>>(json);

                Dictionary<string, List<SingleDungeonTile>> layerTilesDict = new Dictionary<string, List<SingleDungeonTile>>();
                foreach (var pathPos in path)
                {
                    var orientation = CorridorPainter.GetTileOrientation(pathPos, path.Concat(reservedPositions).ToList());

                    if (tileDict.TryGetValue(orientation, out var tileConfig))
                    {
                        var tiles = tileConfig.Tiles;
                        foreach (var tile in tiles)
                        {
                            Vector2 offset = Vector2.Zero;
                            var offsetArray = tile.Offset.Split(' ');
                            if (offsetArray.Length == 2)
                                offset = new Vector2(Convert.ToInt32(offsetArray[0]), Convert.ToInt32(offsetArray[1]));

                            var pos = pathPos + (offset * 16);
                            var id = tile.TileIds.RandomItem();

                            var singleTile = new SingleDungeonTile(pos, id, tile.Layer == "Walls");

                            if (!layerTilesDict.ContainsKey(tile.Layer))
                                layerTilesDict[tile.Layer] = new List<SingleDungeonTile>();
                            layerTilesDict[tile.Layer].Add(singleTile);
                        }
                    }
                }

                foreach (var renderer in renderers)
                {
                    foreach (var layer in renderer.TiledMap.TileLayers)
                    {
                        foreach (var tile in layerTilesDict.SelectMany(t => t.Value))
                        {
                            var layerTile = layer.GetTileAtWorldPosition(tile.Position);
                            if (layerTile != null)
                                layer.RemoveTile(layerTile.X, layerTile.Y);
                        }
                    }
                }

                //open tileset
                using (var stream = TitleContainer.OpenStream(Nez.Content.Tiled.Tilesets.Fairy_forest_tileset))
                {
                    var xDocTileset = XDocument.Load(stream);

                    string tsxDir = Path.GetDirectoryName(Nez.Content.Tiled.Tilesets.Fairy_forest_tileset);
                    var tileset = new TmxTileset().LoadTmxTileset(null, xDocTileset.Element("tileset"), 0, tsxDir);
                    tileset.TmxDirectory = tsxDir;

                    if (layerTilesDict.TryGetValue("Back", out var backTiles))
                    {
                        var dict = new Dictionary<Vector2, SingleTile>();
                        foreach (var backTile in backTiles)
                            dict.Add(backTile.Position, new SingleTile(backTile.TileId, backTile.IsCollider));
                        var corridorRenderer = CreateEntity("").AddComponent(new CorridorRenderer(tileset, dict));
                        corridorRenderer.SetRenderLayer(RenderLayers.Back);
                    }
                    if (layerTilesDict.TryGetValue("Walls", out var wallTiles))
                    {
                        var dict = new Dictionary<Vector2, SingleTile>();
                        foreach (var wallTile in wallTiles)
                            dict.Add(wallTile.Position, new SingleTile(wallTile.TileId, wallTile.IsCollider));
                        var corridorRenderer = CreateEntity("").AddComponent(new CorridorRenderer(tileset, dict));
                        corridorRenderer.SetRenderLayer(RenderLayers.Walls);
                    }
                    if (layerTilesDict.TryGetValue("AboveFront", out var aboveFrontTiles))
                    {
                        var dict = new Dictionary<Vector2, SingleTile>();
                        foreach (var aboveFrontTile in aboveFrontTiles)
                            dict.Add(aboveFrontTile.Position, new SingleTile(aboveFrontTile.TileId, aboveFrontTile.IsCollider));
                        var corridorRenderer = CreateEntity("").AddComponent(new CorridorRenderer(tileset, dict));
                        corridorRenderer.SetRenderLayer(RenderLayers.AboveFront);
                    }
                }
                
            }
        }
    }
}
