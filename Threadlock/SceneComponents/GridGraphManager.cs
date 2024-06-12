using Microsoft.Xna.Framework;
using Nez;
using Nez.AI.Pathfinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Helpers;

namespace Threadlock.SceneComponents
{
    public class GridGraphManager : SceneComponent
    {
        AstarGridGraph _graph;
        Vector2 _gridOffset;

        public override void OnEnabled()
        {
            base.OnEnabled();

            InitializeGraph();
        }

        void InitializeGraph()
        {
            //get all map renderers in the scene
            var mapRenderers = Scene.FindComponentsOfType<TiledMapRenderer>();

            //filter to only those with collision
            mapRenderers = mapRenderers
                .Where(r => r.CollisionLayer != null)
                .ToList();

            Vector2 topLeft = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 bottomRight = new Vector2(float.MinValue, float.MinValue);
            foreach (var renderer in mapRenderers)
            {
                var pos = renderer.Entity.Position / 16;
                var mapSize = new Vector2(renderer.TiledMap.Width, renderer.TiledMap.Height);
                var bottomRightPos = pos + mapSize;

                if (pos.X < topLeft.X)
                    topLeft.X = pos.X;
                if (pos.Y < topLeft.Y)
                    topLeft.Y = pos.Y;
                if (bottomRightPos.X > bottomRight.X)
                    bottomRight.X = bottomRightPos.X;
                if (bottomRightPos.Y > bottomRight.Y)
                    bottomRight.Y = bottomRightPos.Y;
            }

            var size = bottomRight - topLeft;

            _gridOffset = topLeft;

            _graph = new AstarGridGraph((int)size.X, (int)size.Y);

            foreach (var renderer in mapRenderers)
            {
                var tiles = TiledHelper.GetLayerTilesWithPositions(renderer.CollisionLayer);
                foreach (var tile in tiles)
                {
                    var x = tile.Item1.X;
                    var y = tile.Item1.Y;
                    var pos = new Vector2(x, y);
                    pos += (renderer.Entity.Position / 16);
                    pos -= _gridOffset;

                    if (!_graph.Walls.Contains(pos.ToPoint()))
                        _graph.Walls.Add(pos.ToPoint());
                }
            }
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
