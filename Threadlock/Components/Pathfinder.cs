using Microsoft.Xna.Framework;
using Nez;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.DebugTools;
using Threadlock.SceneComponents;
using Threadlock.StaticData;

namespace Threadlock.Components
{
    public class Pathfinder : Component
    {
        //consts
        List<Vector2> _directions = new List<Vector2>()
        {
            new Vector2(0, -1),
            new Vector2(1, -1),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1),
            new Vector2(-1, 1),
            new Vector2(-1, 0),
            new Vector2(-1, -1),
        };
        float _raycastRadius = 32f;

        float _pathDesiredDistance = 8f;
        float _updateInterval = .25f;
        bool _onCooldown = false;

        GridGraphManager _gridGraphManager;
        VelocityComponent _velocityComponent;
        Collider _collider;

        List<Vector2> _path = new List<Vector2>();

        float[] _interestArray = new float[8];
        float[] _dangerArray = new float[8];

        List<Entity> _debugPoints = new List<Entity>();

        public Pathfinder(Collider collider)
        {
            _collider = collider;
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            _gridGraphManager = Entity.Scene.GetOrCreateSceneComponent<GridGraphManager>();

            if (Entity.TryGetComponent<VelocityComponent>(out var velocityComponent))
                _velocityComponent = velocityComponent;
        }

        public void FollowPath(Vector2 target, float speed)
        {
            //get our starting position
            Vector2 startPosition = Entity.Position;
            if (Entity.TryGetComponent<OriginComponent>(out var originComponent))
                startPosition = originComponent.Origin;

            //if not on cooldown, get a new path
            if (!_onCooldown)
            {
                _path = _gridGraphManager.FindPath(startPosition, target);
                _onCooldown = true;
                Game1.Schedule(_updateInterval, timer =>
                {
                    _onCooldown = false;
                });
            }

            //debug points
            foreach (var point in _debugPoints)
                point.Destroy();
            _debugPoints.Clear();
            if (Game1.DebugRenderEnabled)
            {
                foreach (var point in _path)
                {
                    var ent = Entity.Scene.CreateEntity("path-point");
                    ent.SetPosition(point);
                    ent.AddComponent(new PrototypeSpriteRenderer(2, 2));
                    _debugPoints.Add(ent);
                }
            }

            var finalPath = _path;

            //smooth path
            //int currentIndex = 0;
            //while (currentIndex < _path.Count - 1)
            //{
            //    int furthestVisibleIndex = currentIndex;
            //    for (int i = currentIndex + 1; i < _path.Count; i++)
            //    {
            //        var environmentHit = Physics.Linecast(_path[currentIndex], _path[i], 1 << PhysicsLayers.Environment);
            //        if (environmentHit.Collider == null)
            //            furthestVisibleIndex = i;
            //        else
            //            break;
            //    }

            //    //safeguard against getting stuck in while loop
            //    if (furthestVisibleIndex == currentIndex)
            //    {
            //        currentIndex++;
            //    }
            //    else
            //    {
            //        currentIndex = furthestVisibleIndex;
            //    }

            //    finalPath.Add(_path[currentIndex]);
            //}

            Vector2 nextPos = target;
            for (int i = 0; i < finalPath.Count; i++)
            {
                var pos = finalPath[i];

                if (Vector2.Distance(startPosition, pos) <= _pathDesiredDistance)
                {
                    continue;
                }
                else
                {
                    var raycastHit = Physics.Linecast(startPosition, pos, 1 << PhysicsLayers.Environment);
                    if (raycastHit.Collider == null)
                    {
                        nextPos = pos;
                    }
                }
            }

            Array.Clear(_dangerArray);

            for (int i = 0; i < _directions.Count; i++)
            {
                var direction = _directions[i];
                direction.Normalize();

                var desiredDir = nextPos - startPosition;
                desiredDir.Normalize();

                _interestArray[i] = Vector2.Dot(desiredDir, direction);

                var raycast = Physics.Linecast(startPosition, startPosition + (direction * _raycastRadius), 1 << PhysicsLayers.Environment);
                if (raycast.Collider != null)
                {
                    var distanceToObstacle = Vector2.Distance(startPosition, raycast.Point);
                    float weight = distanceToObstacle <= _collider.Bounds.Width ? 1 : (_raycastRadius - distanceToObstacle) / _raycastRadius;

                    var directionToObstacle = raycast.Point - startPosition;
                    directionToObstacle.Normalize();

                    _dangerArray[i] = Vector2.Dot(directionToObstacle, direction) * weight;
                }
            }
            
            var contextMap = new float[8];
            for (int i = 0; i <  contextMap.Length; i++)
            {
                contextMap[i] = Math.Clamp(_interestArray[i] - _dangerArray[i], 0, 1);
            }

            Vector2 weightedSum = Vector2.Zero;
            for (int i = 0; i < contextMap.Length; i++)
            {
                weightedSum += _directions[i] * contextMap[i];
            }

            weightedSum.Normalize();
            _velocityComponent?.Move(weightedSum, speed);
        }
    }
}
