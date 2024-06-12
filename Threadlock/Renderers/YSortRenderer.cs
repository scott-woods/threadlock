using Microsoft.Xna.Framework;
using Nez;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Helpers;
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

            var sortedComponents = GetSortedRenderablesByExcludeLayers(scene, cam, ExcludedRenderLayers.ToList());

            //render components
            foreach (var component in sortedComponents)
                RenderAfterStateCheck(component, cam);

            //handle debug render
            if (ShouldDebugRender && Core.DebugRenderEnabled)
                DebugRender(scene, cam);

            EndRender();
        }

        public static List<RenderableComponent> GetSortedRenderablesByLayers(Scene scene, Camera cam, List<int> renderLayers)
        {
            List<RenderableComponent> renderables = new List<RenderableComponent>();

            //loop through all renderable components in the scene
            for (int i = 0; i < scene.RenderableComponents.Count; i++)
            {
                //get renderable
                var renderable = scene.RenderableComponents[i] as RenderableComponent;

                //check that this renderable should be rendered
                if (renderLayers.Contains(renderable.RenderLayer) && renderable.Enabled && renderable.IsVisibleFromCamera(cam))
                    renderables.Add(renderable);
            }

            return GetSortedRenderables(scene, cam, renderables);
        }

        public static List<RenderableComponent> GetSortedRenderablesByExcludeLayers(Scene scene, Camera cam, List<int> excludeLayers)
        {
            List<RenderableComponent> renderables = new List<RenderableComponent>();
            for (int i = 0; i < scene.RenderableComponents.Count; i++)
            {
                var renderable = scene.RenderableComponents[i] as RenderableComponent;

                if (!excludeLayers.Contains(renderable.RenderLayer) && renderable.Enabled && renderable.IsVisibleFromCamera(cam))
                    renderables.Add(renderable);
            }

            return GetSortedRenderables(scene, cam, renderables);
        }

        public static List<RenderableComponent> GetSortedRenderables(Scene scene, Camera cam, List<RenderableComponent> renderables)
        {
            var beforeYSortSection = new List<RenderableComponent>();
            var frontRenderers = new List<RenderableComponent>();
            var ySortRenderers = new List<RenderableComponent>();
            var afterYSortSection = new List<RenderableComponent>();

            //loop through all renderable components in the scene
            for (int i = 0; i < renderables.Count; i++)
            {
                //get renderable
                var renderable = renderables[i];

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
                        var frontLayers = mapRenderer.TiledMap.TileLayers.Where(l => l.Name == "Front");
                        return frontLayers.Any(l =>
                        {
                            var adjustedBounds = new RectangleF(bounds.Location - mapRenderer.Entity.Position, bounds.Size);
                            var tiles = TiledHelper.GetLayerTilesWithPositions(l).Where(t => adjustedBounds.Contains(t.Item1 * new Vector2(t.Item2.Tileset.TileWidth, t.Item2.Tileset.TileHeight)));
                            return tiles.Any(t => r.Entity.Position.Y + (t.Item1.Y * t.Item2.Tileset.TileHeight) + t.Item2.Tileset.TileHeight > origin.Y);
                        });
                    }
                    else if (r.GetType() == typeof(CorridorRenderer))
                    {
                        var corridorRenderer = r as CorridorRenderer;
                        var tiles = corridorRenderer.GetTilesIntersectingBounds(bounds);
                        return tiles.Any(t => t.Y + corridorRenderer.Tileset.TileHeight > origin.Y);
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

            return sortedComponents;
        }

        public static List<RenderableComponent> GetSortedList(List<RenderableComponent> list)
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
