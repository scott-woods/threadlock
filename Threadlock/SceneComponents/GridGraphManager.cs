using Microsoft.Xna.Framework;
using Nez;
using Nez.AI.Pathfinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.SceneComponents
{
    public class GridGraphManager : SceneComponent
    {
        AstarGridGraph _graph;

        public override void OnEnabled()
        {
            base.OnEnabled();

            InitializeGraph();
        }

        void InitializeGraph()
        {
            //get all map renderers in the scene
            var mapRenderers = Scene.FindComponentsOfType<TiledMapRenderer>();

            //determine total width and height of the scene
            float leftMostPoint = mapRenderers[0].Entity.Position.X / mapRenderers[0].TiledMap.TileWidth;
            float rightMostPoint = (mapRenderers[0].Entity.Position.X / mapRenderers[0].TiledMap.TileWidth) + mapRenderers[0].TiledMap.Width;
            float topMostPoint = mapRenderers[0].Entity.Position.Y / mapRenderers[0].TiledMap.TileHeight;
            float bottomMostPoint = (mapRenderers[0].Entity.Position.Y / mapRenderers[0].TiledMap.TileHeight) + mapRenderers[0].TiledMap.Height;
            foreach (var renderer in mapRenderers)
            {
                var leftPoint = renderer.Entity.Position.X / renderer.TiledMap.TileWidth;
                var rightPoint = (renderer.Entity.Position.X / renderer.TiledMap.TileWidth) + renderer.TiledMap.Width;
                var topPoint = renderer.Entity.Position.Y / renderer.TiledMap.TileHeight;
                var bottomPoint = (renderer.Entity.Position.Y / renderer.TiledMap.TileHeight) + renderer.TiledMap.Height;

                if (leftPoint < leftMostPoint) leftMostPoint = leftPoint;
                if (rightPoint > rightMostPoint) rightMostPoint = rightPoint;
                if (topPoint < topMostPoint) topMostPoint = topPoint;
                if (bottomPoint > bottomMostPoint) bottomMostPoint = bottomPoint;
            }

            //create graph that is total size of the scene
            var width = (int)(rightMostPoint - leftMostPoint);
            var height = (int)(bottomMostPoint - topMostPoint);
            _graph = new AstarGridGraph(width, height);

            //add walls
            foreach (var renderer in mapRenderers)
            {
                if (renderer.CollisionLayer == null)
                    continue;

                var collisionLayer = renderer.CollisionLayer;
                for (var y = 0; y < collisionLayer.Map.Height; y++)
                {
                    for (var x = 0; x < collisionLayer.Map.Width; x++)
                    {
                        if (collisionLayer.GetTile(x, y) != null)
                            _graph.Walls.Add(new Point(x, y));
                    }
                }
            }
        }

        public Point WorldToGridPosition(Vector2 worldPosition)
        {
            var x = Mathf.FastFloorToInt(worldPosition.X / 16f);
            var y = Mathf.FastFloorToInt(worldPosition.Y / 16f);
            return new Point(x, y);
        }

        public Vector2 GridToWorldPosition(Point gridPosition)
        {
            return new Vector2(gridPosition.X, gridPosition.Y) * 16f;
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
