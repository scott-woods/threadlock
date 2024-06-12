using Microsoft.Xna.Framework;
using Nez;
using Nez.DeferredLighting;
using Nez.Tiled;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Threadlock.Components;
using Threadlock.Components.TiledComponents;
using Threadlock.Models;
using Threadlock.StaticData;

namespace Threadlock.Helpers
{
    public static class TiledHelper
    {
        public static List<Entity> SetupMap(Entity mapEntity, TmxMap map, bool addEntitiesToScene = true)
        {
            List<TmxLayer> backLayers = new List<TmxLayer>();
            List<TmxLayer> wallLayers = new List<TmxLayer>();
            List<TmxLayer> passableWallLayers = new List<TmxLayer>();
            List<TmxLayer> frontLayers = new List<TmxLayer>();
            List<TmxLayer> aboveFrontLayers = new List<TmxLayer>();

            foreach (var layer in map.TileLayers)
            {
                if (layer.Name.StartsWith("Back"))
                    backLayers.Add(layer);
                if (layer.Name.StartsWith("Walls"))
                {
                    if (layer.Properties != null && layer.Properties.TryGetValue("Passable", out var passable))
                    {
                        passableWallLayers.Add(layer);
                    }
                    else
                        wallLayers.Add(layer);
                }
                if (layer.Name.StartsWith("Front"))
                {
                    if (layer.Properties != null && layer.Properties.TryGetValue("Hide", out var hide))
                        continue;
                    frontLayers.Add(layer);
                }
                if (layer.Name.StartsWith("AboveFront"))
                    aboveFrontLayers.Add(layer);
            }

            if (backLayers.Any())
            {
                var mapRenderer = mapEntity.AddComponent(new TiledMapRenderer(map));
                mapRenderer.SetLayersToRender(backLayers.Select(l => l.Name).ToArray());
                mapRenderer.RenderLayer = RenderLayers.Back;
                mapRenderer.AutoUpdateTilesets = false;
            }

            //wall layers need one renderer each, since a tiledmaprenderer can only have one collision layer
            foreach (var wallLayer in wallLayers)
            {
                var wallRenderer = mapEntity.AddComponent(new TiledMapRenderer(map, wallLayer.Name));
                if (wallLayer.Properties != null && wallLayer.Properties.ContainsKey("Hide"))
                    wallRenderer.LayerIndicesToRender = Array.Empty<int>();
                else
                    wallRenderer.SetLayersToRender(wallLayer.Name);
                wallRenderer.RenderLayer = RenderLayers.Walls;
                wallRenderer.AutoUpdateTilesets = false;
                Flags.SetFlagExclusive(ref wallRenderer.PhysicsLayer, PhysicsLayers.Environment);
            }

            foreach (var passableLayer in passableWallLayers)
            {
                var passableWallRenderer = mapEntity.AddComponent(new TiledMapRenderer(map, passableLayer.Name));
                passableWallRenderer.SetLayersToRender(passableLayer.Name);
                passableWallRenderer.RenderLayer = RenderLayers.Walls;
                passableWallRenderer.AutoUpdateTilesets = false;
                Flags.SetFlagExclusive(ref passableWallRenderer.PhysicsLayer, PhysicsLayers.ProjectilePassableWall);
            }

            if (frontLayers.Any())
            {
                var frontRenderer = mapEntity.AddComponent(new TiledMapRenderer(map));
                frontRenderer.SetLayersToRender(frontLayers.Select(l => l.Name).ToArray());
                frontRenderer.RenderLayer = RenderLayers.Front;
                frontRenderer.AutoUpdateTilesets = false;
            }

            if (aboveFrontLayers.Any())
            {
                var aboveFrontRenderer = mapEntity.AddComponent(new TiledMapRenderer(map));
                aboveFrontRenderer.SetLayersToRender(aboveFrontLayers.Select(l => l.Name).ToArray());
                aboveFrontRenderer.RenderLayer = RenderLayers.AboveFront;
                aboveFrontRenderer.AutoUpdateTilesets = false;
            }

            return CreateEntitiesForTiledObjects(mapEntity, map, addEntitiesToScene);
        }

        public static List<Entity> CreateEntitiesForTiledObjects(Entity mapEntity, TmxMap map, bool addEntitiesToScene = true)
        {
            var entities = new List<Entity>();

            foreach (var obj in map.ObjectGroups.SelectMany(g => g.Objects))
            {
                if (string.IsNullOrWhiteSpace(obj.Type)) continue;
                var type = Type.GetType("Threadlock.Components.TiledComponents." + obj.Type);
                if (type == null) continue;
                var instance = Activator.CreateInstance(type) as TiledComponent;
                instance.TmxObject = obj;
                instance.MapEntity = mapEntity;
                instance.ParentMap = map;

                var position = new Vector2();
                switch (obj.ObjectType)
                {
                    case TmxObjectType.Basic:
                    case TmxObjectType.Ellipse:
                    case TmxObjectType.Tile:
                        //position = mapRenderer.Entity.Position + new Vector2(obj.X + obj.Width / 2, obj.Y + obj.Height / 2);
                        position = new Vector2(obj.X, obj.Y);
                        break;
                    case TmxObjectType.Polygon:
                        //position = mapRenderer.Entity.Position + new Vector2(obj.X, obj.Y);
                        //position = new Vector2(obj.Points.Select(p => p.X).Min(), obj.Points.Select(p => p.Y).Min());
                        position = new Vector2(obj.X, obj.Y);
                        break;
                    default:
                        //position = mapRenderer.Entity.Position + new Vector2(obj.X, obj.Y);
                        position = new Vector2(obj.X, obj.Y);
                        break;

                }

                var entity = new Entity(obj.Name);
                entity.SetPosition(position);
                entity.SetParent(mapEntity);
                entity.AddComponent(instance);

                if (addEntitiesToScene)
                    mapEntity.Scene.AddEntity(entity);

                entities.Add(entity);
            }

            return entities;
        }

        public static List<Entity> CreateEntitiesForTiledObjects(TiledMapRenderer mapRenderer, bool addEntitiesToScene = true)
        {
            return CreateEntitiesForTiledObjects(mapRenderer.Entity, mapRenderer.TiledMap, addEntitiesToScene);
        }

        /// <summary>
        /// get a list of world space positions on a specific layer
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="layerName"></param>
        /// <returns></returns>
        public static List<Vector2> GetTilePositionsByLayer(Entity entity, string layerName)
        {
            //init list
            var tilePositions = new List<Vector2>();

            var renderers = entity.GetComponents<TiledMapRenderer>();
            List<TmxMap> seenMaps = new List<TmxMap>();
            foreach (var renderer in renderers)
            {
                if (renderer.TiledMap != null && !seenMaps.Contains(renderer.TiledMap))
                {
                    seenMaps.Add(renderer.TiledMap);

                    //get layer
                    var layer = renderer.TiledMap.TileLayers.FirstOrDefault(l => l.Name == layerName);

                    if (layer != null)
                    {
                        for (int x = 0; x < layer.Map.Width; x++)
                        {
                            for (int y = 0; y < layer.Map.Height; y++)
                            {
                                var tile = layer.GetTile(x, y);
                                if (tile != null)
                                    tilePositions.Add((new Vector2(x * tile.Tileset.TileWidth, y * tile.Tileset.TileHeight) + entity.Position));
                            }
                        }
                    }
                }
                    
            }

            return tilePositions;
        }

        /// <summary>
        /// checks that a position is not in any tiled map walls, and that it is on a non-null tile
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public static bool ValidatePosition(Scene scene, Vector2 position)
        {
            var renderers = scene.FindComponentsOfType<TiledMapRenderer>();
            bool isInMap = false;
            foreach (var renderer in renderers)
            {
                if (!renderer.Bounds.Contains(position))
                    continue;

                //if the position is in a wall
                if (renderer.CollisionLayer != null)
                    if (renderer.GetTileAtWorldPosition(position) != null)
                        return false;

                //check that a layer actually has a tile at this position
                if (renderer.TiledMap.TileLayers.Any(l => l.GetTileAtWorldPosition(position - renderer.Entity.Position) != null))
                    isInMap = true;
            }

            var corridorRenderers = scene.FindComponentsOfType<CorridorRenderer>();
            foreach (var renderer in corridorRenderers)
            {
                if (renderer.IsTileAtWorldPosition(position))
                {
                    isInMap = true;
                    break;
                }
            }

            if (!isInMap)
                return false;

            return true;
        }

        public static List<Tuple<Vector2, TmxLayerTile>> GetLayerTilesWithPositions(TmxLayer layer)
        {
            List<Tuple<Vector2, TmxLayerTile>> tiles = new List<Tuple<Vector2, TmxLayerTile>>();

            for (int x = 0; x < layer.Map.Width; x++)
            {
                for (int y = 0; y < layer.Map.Height; y++)
                {
                    var tile = layer.GetTile(x, y);
                    if (tile != null)
                        tiles.Add(Tuple.Create(new Vector2(x, y), tile));
                }
            }

            return tiles;
        }

        public static List<Entity> SetupLightingTiles(Entity mapEntity, TmxMap map, bool addEntitiesToScene = true)
        {
            var entities = new List<Entity>();

            foreach (var layer in map.TileLayers)
            {
                if (layer.Name.StartsWith("Prototype"))
                    continue;

                var tiles = GetLayerTilesWithPositions(layer);
                foreach (var tileTuple in tiles)
                {
                    var tile = tileTuple.Item2;
                    var tilePos = tileTuple.Item1;

                    if (tile.TilesetTile == null)
                        continue;

                    var pos = new Vector2((tilePos.X * tile.Tileset.TileWidth) + (tile.Tileset.TileWidth / 2), (tilePos.Y * tile.Tileset.TileHeight) + (tile.Tileset.TileHeight / 2));
                    pos += mapEntity.Position;

                    if (tile.TilesetTile.Type == "LightSource")
                    {
                        ParseLightingProperties(tile.TilesetTile.Properties, out var lightColor, out var lightRadius, out var lightIntensity);

                        var lightEntity = new Entity("light");
                        lightEntity.SetPosition(pos);

                        var light = lightEntity.AddComponent(new PointLight(lightColor));
                        light.DebugRenderEnabled = false;
                        light.SetRenderLayer(RenderLayers.Light);
                        light.SetRadius(lightRadius);
                        light.SetIntensity(lightIntensity);

                        if (addEntitiesToScene)
                            mapEntity.Scene.AddEntity(lightEntity);

                        entities.Add(lightEntity);
                    }
                }
            }

            return entities;
        }

        public static void ParseLightingProperties(Dictionary<string, string> properties, out Color lightColor, out float lightRadius, out float lightIntensity)
        {
            lightColor = Color.White;
            lightIntensity = .5f;
            lightRadius = 50;

            if (properties != null)
            {
                if (properties.TryGetValue("Color", out var colorString))
                {
                    var rgb = colorString.Split(' ').Select(c => Convert.ToInt32(c)).ToList();
                    if (rgb.Count == 3)
                        lightColor = new Color(rgb[0], rgb[1], rgb[2]);
                    else if (rgb.Count == 4)
                        lightColor = new Color(rgb[0], rgb[1], rgb[2], rgb[3]);
                }

                if (properties.TryGetValue("Intensity", out var intensity))
                    lightIntensity = float.Parse(intensity);

                if (properties.TryGetValue("Radius", out var radius))
                    lightRadius = float.Parse(radius);
            }
        }
        
        public static TmxTilesetExt GetTileset(string path)
        {
            var stream = TitleContainer.OpenStream(path);

            var xDocTileset = XDocument.Load(stream);

            string tsxDir = Path.GetDirectoryName(path);
            var tileset = new TmxTileset().LoadTmxTileset(null, xDocTileset.Element("tileset"), 0, tsxDir);
            tileset.TmxDirectory = tsxDir;

            return new TmxTilesetExt(tileset);
        }

        public static RectangleF GetCollisionBounds(TmxMap map, Vector2 position)
        {
            var rect = new RectangleF();
            rect.Location = position;
            if (map != null)
            {
                var collisionLayers = map.TileLayers
                    .Where(l => l.Name.StartsWith("Walls"));

                var minX = int.MaxValue;
                var minY = int.MaxValue;
                var maxX = int.MinValue;
                var maxY = int.MinValue;

                for (int x = 0; x < map.Width; x++)
                {
                    for (int y = 0; y < map.Height; y++)
                    {
                        if (collisionLayers.Any(l => l.GetTile(x, y) != null))
                        {
                            minX = Math.Min(minX, x * map.TileWidth);
                            minY = Math.Min(minY, y * map.TileHeight);
                            maxX = Math.Max(maxX, (x + 1) * map.TileWidth);
                            maxY = Math.Max(maxY, (y + 1) * map.TileHeight);
                        }
                    }
                }

                rect.Location += new Vector2(minX, minY);
                rect.Size = new Vector2(maxX - minX, maxY - minY);
            }

            return rect;
        }
    }
}
