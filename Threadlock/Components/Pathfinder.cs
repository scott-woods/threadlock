using Microsoft.Xna.Framework;
using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
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

        float _pathDesiredDistance = 4f;
        float _updateInterval = .25f;
        bool _onCooldown = false;

        GridGraphManager _gridGraphManager;
        VelocityComponent _velocityComponent;
        Collider _collider;

        List<Vector2> _path = new List<Vector2>();

        float[] _interestArray = new float[8];
        float[] _dangerArray = new float[8];

        List<Entity> _debugPoints = new List<Entity>();

        int _currentPathIndex;

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
                _currentPathIndex = 0;

                if (_gridGraphManager.TryFindPath(startPosition, target, out this._path))
                {
                    List<Vector2> adjustedPath = new List<Vector2>();

                    Vector2 currentBase = startPosition;
                    Vector2 nextVisiblePos = this._path[0];
                    for (int i = 0; i < this._path.Count; i++)
                    {
                        var environmentHit = Physics.Linecast(currentBase, this._path[i], (1 << PhysicsLayers.Environment) | (1 << PhysicsLayers.ProjectilePassableWall));

                        //if there is a collision, 
                        if (environmentHit.Collider != null)
                        {
                            adjustedPath.Add(nextVisiblePos);
                            currentBase = nextVisiblePos;
                            nextVisiblePos = this._path[i];
                        }
                        else
                            nextVisiblePos = this._path[i];
                    }

                    //always include the target position in the path
                    if (adjustedPath.Count <= 0 || adjustedPath.Last() != target)
                        adjustedPath.Add(target);

                    this._path = adjustedPath;
                }
                else
                    return;

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
                foreach (var point in this._path)
                {
                    var ent = Entity.Scene.CreateEntity("path-point");
                    ent.SetPosition(point);
                    ent.AddComponent(new PrototypeSpriteRenderer(2, 2));
                    _debugPoints.Add(ent);
                }
            }

            Vector2 nextPos = _path[_currentPathIndex];
            if (_currentPathIndex < _path.Count - 1)
            {
                if (Vector2.Distance(startPosition, nextPos) <= _pathDesiredDistance)
                {
                    _currentPathIndex++;
                    nextPos = _path[_currentPathIndex];
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
            _velocityComponent?.Move(weightedSum, speed, false, true);
        }
    }
}
