using Microsoft.Xna.Framework;
using Nez;
using Nez.Tiled;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Components.TiledComponents;
using Threadlock.StaticData;

namespace Threadlock.Helpers
{
    public static class TiledHelper
    {
        public static void SetupMap(Entity mapEntity, TmxMap map)
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
                        if (passable.ToLower() == "true")
                            passableWallLayers.Add(layer);
                    }
                    else
                        wallLayers.Add(layer);
                }
                if (layer.Name.StartsWith("Front"))
                    frontLayers.Add(layer);
                if (layer.Name.StartsWith("AboveFront"))
                    aboveFrontLayers.Add(layer);
            }

            if (backLayers.Any())
            {
                var mapRenderer = mapEntity.AddComponent(new TiledMapRenderer(map));
                mapRenderer.SetLayersToRender(backLayers.Select(l => l.Name).ToArray());
                mapRenderer.RenderLayer = RenderLayers.Back;
                CreateEntitiesForTiledObjects(mapRenderer);
            }

            //wall layers need one renderer each, since a tiledmaprenderer can only have one collision layer
            foreach (var wallLayer in wallLayers)
            {
                var wallRenderer = mapEntity.AddComponent(new TiledMapRenderer(map, wallLayer.Name));
                wallRenderer.SetLayersToRender(wallLayer.Name);
                wallRenderer.RenderLayer = RenderLayers.Walls;
                Flags.SetFlagExclusive(ref wallRenderer.PhysicsLayer, PhysicsLayers.Environment);
            }

            foreach (var passableLayer in passableWallLayers)
            {
                var passableWallRenderer = mapEntity.AddComponent(new TiledMapRenderer(map, passableLayer.Name));
                passableWallRenderer.SetLayersToRender(passableLayer.Name);
                passableWallRenderer.RenderLayer = RenderLayers.Walls;
                Flags.SetFlagExclusive(ref passableWallRenderer.PhysicsLayer, PhysicsLayers.ProjectilePassableWall);
            }

            if (frontLayers.Any())
            {
                var frontRenderer = mapEntity.AddComponent(new TiledMapRenderer(map));
                frontRenderer.SetLayersToRender(frontLayers.Select(l => l.Name).ToArray());
                frontRenderer.RenderLayer = RenderLayers.Front;
            }

            if (aboveFrontLayers.Any())
            {
                var aboveFrontRenderer = mapEntity.AddComponent(new TiledMapRenderer(map));
                aboveFrontRenderer.SetLayersToRender(aboveFrontLayers.Select(l => l.Name).ToArray());
                aboveFrontRenderer.RenderLayer = RenderLayers.AboveFront;
            }
        }

        public static void CreateEntitiesForTiledObjects(TiledMapRenderer mapRenderer)
        {
            foreach (var obj in mapRenderer.TiledMap.ObjectGroups.SelectMany(g => g.Objects))
            {
                if (string.IsNullOrWhiteSpace(obj.Type)) continue;
                var type = Type.GetType("Threadlock.Components.TiledComponents." + obj.Type);
                if (type == null) continue;
                var instance = Activator.CreateInstance(type) as TiledComponent;
                instance.TmxObject = obj;
                instance.MapEntity = mapRenderer.Entity;
                instance.ParentMap = mapRenderer.TiledMap;

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

                var entity = mapRenderer.Entity.Scene.CreateEntity(obj.Name, position);
                entity.SetParent(mapRenderer.Entity);
                entity.AddComponent(instance);
            }
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
                        foreach (var tile in layer.Tiles.Where(t => t != null))
                            tilePositions.Add(renderer.Entity.Position + new Vector2(tile.X * 16, tile.Y * 16));
                    }
                }
                    
            }

            return tilePositions;
        }

        /// <summary>
        /// check that a world space position is not in a collision tile or a null tile for a specific map renderer
        /// </summary>
        /// <param name="mapRenderer"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public static bool ValidatePosition(TiledMapRenderer mapRenderer, Vector2 position)
        {
            if (mapRenderer != null)
            {
                if (!mapRenderer.Bounds.Contains(position))
                    return false;
                var collidingTile = mapRenderer.GetTileAtWorldPosition(position);
                if (collidingTile != null)
                    return false;
                if (!mapRenderer.TiledMap.TileLayers.Any(l => l.GetTileAtWorldPosition(position - mapRenderer.Entity.Position) != null))
                    return false;
            }

            return true;
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
    }
}
