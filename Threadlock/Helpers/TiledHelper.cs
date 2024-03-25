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

namespace Threadlock.Helpers
{
    public static class TiledHelper
    {
        public static void CreateEntitiesForTiledObjects(TiledMapRenderer mapRenderer)
        {
            foreach (var obj in mapRenderer.TiledMap.ObjectGroups.SelectMany(g => g.Objects))
            {
                if (string.IsNullOrWhiteSpace(obj.Type)) return;
                var type = Type.GetType("Threadlock.Components.TiledComponents." + obj.Type);
                var instance = Activator.CreateInstance(type) as TiledComponent;
                instance.TmxObject = obj;
                instance.MapEntity = mapRenderer.Entity;

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
