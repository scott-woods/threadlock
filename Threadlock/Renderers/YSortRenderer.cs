using Nez;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.StaticData;

namespace Threadlock.Renderers
{
    public class YSortRenderer : RenderLayerExcludeRenderer
    {
        public YSortRenderer(int renderOrder, params int[] excludedRenderLayers) : base(renderOrder, excludedRenderLayers)
        {
        }

        public override void Render(Scene scene)
        {
            var cam = Camera ?? scene.Camera;
            BeginRender(cam);

            var beforeYSortSection = new List<RenderableComponent>();
            var frontRenderers = new List<RenderableComponent>();
            var ySortRenderers = new List<RenderableComponent>();
            var afterYSortSection = new List<RenderableComponent>();

            //loop through all renderable components in the scene
            for (int i = 0; i < scene.RenderableComponents.Count; i++)
            {
                //get renderable
                var renderable = scene.RenderableComponents[i] as RenderableComponent;

                //check that this renderable should be rendered
                if (!ExcludedRenderLayers.Contains(renderable.RenderLayer) && renderable.Enabled &&
                    renderable.IsVisibleFromCamera(cam))
                {
                    //pre y sort section
                    if (renderable.RenderLayer > RenderLayers.YSort)
                    {
                        beforeYSortSection.Add(renderable);
                        continue;
                    }

                    //get Front tilemap renderers
                    if (renderable.RenderLayer == RenderLayers.Front)
                    {
                        frontRenderers.Add(renderable);
                        continue;
                    }

                    //get renderables in YSort layer
                    if (renderable.RenderLayer == RenderLayers.YSort)
                    {
                        ySortRenderers.Add(renderable);
                        continue;
                    }

                    //post y sort section
                    if (renderable.RenderLayer < RenderLayers.Front)
                    {
                        afterYSortSection.Add(renderable);
                        continue;
                    }
                }
            }

            //partition ysort renderables to before or after front layer
            var ySortBefore = new List<RenderableComponent>();
            var ySortAfter = new List<RenderableComponent>();

            //loop through y sort renderables
            foreach (var renderable in ySortRenderers)
            {
                //get bounds and origin this renderable is based on
                var bounds = renderable.Bounds;
                var origin = renderable.Entity.Position;
                if (renderable.Entity.TryGetComponent<OriginComponent>(out var originComponent))
                {
                    bounds = new RectangleF(originComponent.Origin.X, originComponent.Origin.Y, 1, 1);
                    origin = originComponent.Origin;
                }

                //if there are any tiles on a Front layer with a y value greater than the origin, render before Front layer
                if (frontRenderers.Any((r) =>
                {
                    if (r.GetType() == typeof(TiledMapRenderer))
                    {
                        var mapRenderer = r as TiledMapRenderer;
                        return mapRenderer.TiledMap.TileLayers.Any(l =>
                        {
                            var tiles = l.GetTilesIntersectingBounds(bounds);
                            return tiles.Any(t =>
                            {
                                return r.Entity.Position.Y + (t.Y * mapRenderer.TiledMap.TileHeight) + mapRenderer.TiledMap.TileHeight > origin.Y;
                            });
                        });
                    }
                    else
                    {
                        return r.Entity.Position.Y > origin.Y;
                    }
                }))
                {
                    ySortBefore.Add(renderable);
                }
                else
                    ySortAfter.Add(renderable);
            }

            //further sort the ysort renderables by Y value
            ySortBefore = GetSortedList(ySortBefore);
            ySortAfter = GetSortedList(ySortAfter);

            //merge the partitions
            var sortedComponents = beforeYSortSection
                .Concat(ySortBefore)
                .Concat(frontRenderers)
                .Concat(ySortAfter)
                .Concat(afterYSortSection)
                .ToList();

            //render components
            foreach (var component in sortedComponents)
                RenderAfterStateCheck(component, cam);

            //handle debug render
            if (ShouldDebugRender && Core.DebugRenderEnabled)
                DebugRender(scene, cam);

            EndRender();

            //var renderables = new List<RenderableComponent>();
            //var backMapRenderers = new List<TiledMapRenderer>();
            //var frontMapRenderers = new List<TiledMapRenderer>();
            //for (var i = 0; i < scene.RenderableComponents.Count; i++)
            //{
            //    var renderable = scene.RenderableComponents[i] as RenderableComponent;
            //    if (!ExcludedRenderLayers.Contains(renderable.RenderLayer) && renderable.Enabled &&
            //        renderable.IsVisibleFromCamera(cam))
            //    {
            //        if (renderable.GetType() == typeof(TiledMapRenderer))
            //        {
            //            var mapRenderer = renderable as TiledMapRenderer;
            //            switch (mapRenderer.RenderLayer)
            //            {
            //                case RenderLayers.Back:
            //                    break;
            //                case RenderLayers.Front:
            //                    break;
            //                case RenderLayers.AboveFront:
            //                    break;
            //            }
            //            bool mapAdded = false;
            //            for (int j = 0; j < mapRenderer.TiledMap.TileLayers.Count; j++)
            //            {
            //                var layer = mapRenderer.TiledMap.TileLayers[j];
            //                if (new[] { "Front" }.Contains(layer.Name) && mapRenderer.LayerIndicesToRender.Contains(j))
            //                {
            //                    frontMapRenderers.Add(mapRenderer);
            //                    mapAdded = true;
            //                    break;
            //                }
            //            }
            //            if (!mapAdded)
            //                backMapRenderers.Add(mapRenderer);
            //        }
            //        else
            //        {
            //            renderables.Add(renderable);
            //        }
            //    }
            //}

            //List<RenderableComponent> beforeMaps = new List<RenderableComponent>();
            //List<RenderableComponent> afterMaps = new List<RenderableComponent>();

            //foreach (var renderable in renderables)
            //{
            //    //get bounds and origin this renderable is based on
            //    var bounds = renderable.Bounds;
            //    var origin = renderable.Entity.Position;
            //    if (renderable.Entity.TryGetComponent<OriginComponent>(out var originComponent))
            //    {
            //        bounds = new RectangleF(originComponent.Origin.X, originComponent.Origin.Y, 1, 1);
            //        origin = originComponent.Origin;
            //    }

            //    //loop through map renderers
            //    bool canContinue = false;
            //    foreach (var mapRenderer in frontMapRenderers)
            //    {
            //        //check any front layers
            //        foreach (var layerName in new[] { "Front" })
            //        {
            //            if (mapRenderer.TiledMap.TileLayers.TryGetValue(layerName, out var layer))
            //            {
            //                //get tiles that intersect this renderable
            //                var tiles = layer.GetTilesIntersectingBounds(bounds);
            //                foreach (var tile in tiles)
            //                {
            //                    //if tile is below the origin, render before front layer
            //                    if ((tile.Y * mapRenderer.TiledMap.TileHeight) + mapRenderer.TiledMap.TileHeight > origin.Y)
            //                    {
            //                        beforeMaps.Add(renderable);
            //                        canContinue = true;
            //                        break;
            //                    }
            //                }
            //            }

            //            if (canContinue) break;
            //        }

            //        if (canContinue) break;
            //    }

            //    if (!canContinue)
            //        afterMaps.Add(renderable);
            //}

            ////sort lists
            //var sortedBefore = GetSortedList(beforeMaps);
            //var sortedAfter = GetSortedList(afterMaps);

            ////render back maps
            //foreach (var renderable in backMapRenderers)
            //    RenderAfterStateCheck(renderable, cam);

            ////render before maps
            //foreach (var renderable in sortedBefore)
            //    RenderAfterStateCheck(renderable, cam);

            ////render maps
            //foreach (var renderable in frontMapRenderers)
            //    RenderAfterStateCheck(renderable, cam);

            ////render after maps
            //foreach (var renderable in sortedAfter)
            //    RenderAfterStateCheck(renderable, cam);

            ////handle debug render
            //if (ShouldDebugRender && Core.DebugRenderEnabled)
            //    DebugRender(scene, cam);

            //EndRender();
        }

        List<RenderableComponent> GetSortedList(List<RenderableComponent> list)
        {
            //order by Y
            var sorted = list.OrderBy(r =>
            {
                if (r.Entity.TryGetComponent<OriginComponent>(out var originComponent))
                    return originComponent.Origin.Y;
                else return r.Entity.Position.Y;
            }).ToList();

            return sorted;
        }
    }
}
