using Microsoft.Xna.Framework;
using Nez;
using Nez.AI.Pathfinding;
using Nez.Tiled;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Helpers;
using Threadlock.StaticData;
using static Threadlock.StaticData.Tiles.Forge;

namespace Threadlock.SceneComponents
{
    public class GridGraphManager : SceneComponent
    {
        AstarGridGraph _graph;
        Vector2 _gridOffset;
        List<TiledMapRenderer> _mapRenderers = new List<TiledMapRenderer>();
        Dictionary<TmxMap, AstarGridGraph> _graphDictionary;

        public override void OnEnabled()
        {
            base.OnEnabled();

            InitializeGraph();
        }

        public void InitializeGraph()
        {
            //get all map renderers in the scene
            _mapRenderers = Scene.FindComponentsOfType<TiledMapRenderer>();

            //init graph dictionary
            _graphDictionary = new Dictionary<TmxMap, AstarGridGraph>();

            //keep track of seen layers (because the same map can have multiple collision layers spread across different renderers)
            var seenLayers = new List<TmxLayer>();

            //loop through renderers
            foreach (var renderer in _mapRenderers)
            {
                //if no collision layer, skip
                if (renderer.CollisionLayer == null)
                    continue;

                //if we've already handled this layer, skip
                if (seenLayers.Contains(renderer.CollisionLayer))
                    continue;

                //if we've already made a graph for this map, just add to that one
                if (_graphDictionary.TryGetValue(renderer.TiledMap, out var existingGraph))
                {
                    for (var y = 0; y < renderer.TiledMap.Height; y++)
                    {
                        for (var x = 0; x < renderer.TiledMap.Width; x++)
                        {
                            if (renderer.CollisionLayer.GetTile(x, y) != null)
                            {
                                var wallPos = new Point(x, y);
                                if (!existingGraph.Walls.Contains(wallPos))
                                    existingGraph.Walls.Add(wallPos);
                            }
                        }
                    }
                }
                else
                {
                    //create a graph for this map and add it to the dictionary
                    var graph = new AstarGridGraph(renderer.CollisionLayer);
                    _graphDictionary.Add(renderer.TiledMap, graph);
                }
            }

            ////get all map renderers in the scene
            //var mapRenderers = Scene.FindComponentsOfType<TiledMapRenderer>();

            ////filter to only those with collision
            //mapRenderers = mapRenderers
            //    .Where(r => r.CollisionLayer != null)
            //    .ToList();

            //Vector2 topLeft = new Vector2(float.MaxValue, float.MaxValue);
            //Vector2 bottomRight = new Vector2(float.MinValue, float.MinValue);
            //foreach (var renderer in mapRenderers)
            //{
            //    var pos = renderer.Entity.Position / 16;
            //    var mapSize = new Vector2(renderer.TiledMap.Width, renderer.TiledMap.Height);
            //    var bottomRightPos = pos + mapSize;

            //    if (pos.X < topLeft.X)
            //        topLeft.X = pos.X;
            //    if (pos.Y < topLeft.Y)
            //        topLeft.Y = pos.Y;
            //    if (bottomRightPos.X > bottomRight.X)
            //        bottomRight.X = bottomRightPos.X;
            //    if (bottomRightPos.Y > bottomRight.Y)
            //        bottomRight.Y = bottomRightPos.Y;
            //}

            //var size = bottomRight - topLeft;

            //_gridOffset = topLeft;

            //_graph = new AstarGridGraph((int)size.X, (int)size.Y);

            //foreach (var renderer in mapRenderers)
            //{
            //    var tiles = TiledHelper.GetLayerTilesWithPositions(renderer.CollisionLayer);
            //    foreach (var tile in tiles)
            //    {
            //        var x = tile.Item1.X;
            //        var y = tile.Item1.Y;
            //        var pos = new Vector2(x, y);
            //        pos += (renderer.Entity.Position / 16);
            //        pos -= _gridOffset;

            //        if (!_graph.Walls.Contains(pos.ToPoint()))
            //            _graph.Walls.Add(pos.ToPoint());
            //    }
            //}
        }

        public Point WorldToGridPosition(Vector2 worldPosition)
        {
            var x = Mathf.FastFloorToInt(worldPosition.X / 16f);
            var y = Mathf.FastFloorToInt(worldPosition.Y / 16f);
            var pos = new Vector2(x, y);
            pos -= _gridOffset;
            return pos.ToPoint();
        }

        public Vector2 GridToWorldPosition(Point gridPosition)
        {
            var pos = new Vector2(gridPosition.X, gridPosition.Y) + _gridOffset;
            return pos * 16f;
        }

        public bool TryFindPath(Vector2 start, Vector2 end, out List<Vector2> path)
        {
            path = new List<Vector2>();

            var renderer = GetRendererByPosition(start);
            if (renderer == null)
                return false;

            if (_graphDictionary.TryGetValue(renderer.TiledMap, out var graph))
            {
                var adjustedStart = start - renderer.Entity.Position;
                var startPoint = renderer.TiledMap.WorldToTilePosition(adjustedStart);

                var adjustedEnd = end - renderer.Entity.Position;
                var endPoint = renderer.TiledMap.WorldToTilePosition(adjustedEnd);

                var gridPath = graph.Search(startPoint, endPoint);

                if (gridPath == null)
                    return false;

                foreach (var item in gridPath)
                {
                    var worldPos = renderer.TiledMap.TileToWorldPosition(item) + renderer.Entity.Position + new Vector2(8, 8);
                    path.Add(worldPos);
                }

                return true;
            }

            return false;
        }

        public bool IsPositionInLayer(Vector2 position, string layerName)
        {
            var renderer = GetRendererByPosition(position);
            if (renderer == null)
                return false;

            var adjustedWorldPos = position - renderer.Entity.Position;
            var tilePos = renderer.TiledMap.WorldToTilePosition(adjustedWorldPos);

            foreach (var layer in renderer.TiledMap.TileLayers.Where(l => l.Name.StartsWith(layerName)))
            {
                if (layer.GetTile(tilePos.X, tilePos.Y) != null)
                    return true;
            }

            return false;
        }

        public bool IsPositionValid(Vector2 position)
        {
            var renderer = GetRendererByPosition(position);
            if (renderer == null)
                return false;

            var adjustedWorldPos = position - renderer.Entity.Position;
            var tilePos = renderer.TiledMap.WorldToTilePosition(adjustedWorldPos);

            if (_graphDictionary.TryGetValue(renderer.TiledMap, out var graph))
            {
                if (graph.Walls.Contains(tilePos))
                    return false;
            }

            return true;
        }

        TiledMapRenderer GetRendererByPosition(Vector2 position)
        {
            return _mapRenderers.FirstOrDefault(r => r.Bounds.Contains(position));
        }

        public List<Vector2> FindPath(Vector2 startPoint, Vector2 endPoint)
        {
            List<Vector2> path = new List<Vector2>();

            var gridPath = _graph.Search(WorldToGridPosition(startPoint), WorldToGridPosition(endPoint));

            //return empty path if grid path is null
            if (gridPath == null)
            {
                return path;
            }

            //translate to path in world position
            foreach (var pathItem in gridPath)
            {
                var worldPos = GridToWorldPosition(pathItem);
                worldPos += new Vector2(8, 8);

                path.Add(worldPos);
            }

            //add target as final point
            if (path.Last() != endPoint)
                path.Add(endPoint);

            return path;
        }
    }
}
