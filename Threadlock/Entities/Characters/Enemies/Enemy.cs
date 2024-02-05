using Microsoft.Xna.Framework;
using Nez;
using Nez.AI.BehaviorTrees;
using Nez.Sprites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.StaticData;
using TaskStatus = Nez.AI.BehaviorTrees.TaskStatus;

namespace Threadlock.Entities.Characters.Enemies
{
    public abstract class Enemy<T> : BaseEnemy where T : Enemy<T>
    {
        Entity _targetEntity;
        public Entity TargetEntity {
            get
            {
                //get all entities with the enemy target tag
                var targets = Scene.FindEntitiesWithTag(EntityTags.EnemyTarget);

                //loop through potential targets and determine the closest one
                var minDistance = float.MaxValue;
                Entity targetEntity = Player.Player.Instance;
                foreach (var target in targets)
                {
                    //get position
                    Vector2 pos = target.Position;

                    var dist = Vector2.Distance(Position, target.Position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        targetEntity = target;
                    }
                }

                return targetEntity;
            }
        }

        protected abstract float _speed { get; }

        BehaviorTree<T> _tree;
        BehaviorTree<T> _subTree;

        EnemyAction<T> _activeAction;

        //components
        StatusComponent _statusComponent;

        #region LIFECYCLE

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            //add components
            _statusComponent = AddComponent(new StatusComponent(StatusPriority.Normal));

            //create sub tree
            _subTree = CreateSubTree();

            //create tree
            _tree = CreateBehaviorTree();
        }

        public override void Update()
        {
            base.Update();

            _tree.Tick();
        }

        #endregion

        #region VIRTUAL METHODS

        public virtual bool ShouldAbort()
        {
            return _statusComponent.CurrentStatusPriority != StatusPriority.Normal;
        }

        public virtual bool ShouldRunSubTree()
        {
            //var gameStateManager = Game1.GameStateManager;
            //if (gameStateManager.GameState != GameState.Combat) return false;
            //if (c.StatusComponent.CurrentStatus.Type != Status.StatusType.Normal) return false;
            //return true;

            if (_statusComponent.CurrentStatusPriority != StatusPriority.Normal) return false;
            return true;
        }

        public virtual BehaviorTree<T> CreateBehaviorTree()
        {
            var tree = BehaviorTreeBuilder<T>.Begin(this as T)
                .Selector(AbortTypes.Self)
                    .ConditionalDecorator(c => c.ShouldAbort(), true)
                        .Action(c => c.AbortActions())
                    //.ConditionalDecorator(c =>
                    //{
                    //    var gameStateManager = Game1.GameStateManager;
                    //    return gameStateManager.GameState != GameState.Combat;
                    //}, true)
                    //    .Sequence()
                    //        .Action(c => c.AbortActions())
                    //        .Action(c => c.Idle())
                    //    .EndComposite()
                    .ConditionalDecorator(c => c.ShouldRunSubTree(), true)
                        .SubTree(_subTree)
                .EndComposite()
                .Build();

            tree.UpdatePeriod = 0;

            return tree;
        }

        #endregion

        #region ABSTRACT METHODS

        public abstract BehaviorTree<T> CreateSubTree();

        #endregion

        #region TASKS

        public virtual TaskStatus AbortActions()
        {
            _activeAction?.Abort();
            _activeAction = null;

            return TaskStatus.Success;
        }

        public virtual TaskStatus ExecuteAction(EnemyAction<T> action)
        {
            _activeAction = action;

            var status = action.Execute();

            if (status == TaskStatus.Success || status == TaskStatus.Failure)
            {
                _activeAction = null;
            }

            return status;
        }

        public virtual TaskStatus MoveToTarget(Entity target)
        {
            if (target.TryGetComponent<OriginComponent>(out var originComponent))
                return MoveToTarget(originComponent.Origin);
            else return MoveToTarget(target.Position);
        }

        public virtual TaskStatus MoveToTarget(Vector2 target)
        {
            //handle animation
            if (TryGetComponent<SpriteAnimator>(out var animator))
            {
                if (animator.Animations.ContainsKey("Run") && !animator.IsAnimationActive("Run"))
                {
                    animator.Play("Run");
                }
            }

            //follow path
            //var gridGraphManager = MapEntity.GetComponent<GridGraphManager>();

            //var targetPos = _targetEntity.Position;
            //var leftTarget = targetPos + new Vector2(-10, 0);
            //var rightTarget = targetPos + new Vector2(10, 0);
            //var leftDistance = Vector2.Distance(OriginComponent.Origin, leftTarget);
            //var rightDistance = Vector2.Distance(OriginComponent.Origin, rightTarget);

            //var target = leftDistance > rightDistance ? rightTarget : leftTarget;
            //var altTarget = target == rightTarget ? leftTarget : rightTarget;

            //Vector2 finalTarget = targetPos;

            //if (!gridGraphManager.IsPositionInWall(target))
            //{
            //    finalTarget = target;
            //}
            //else if (!gridGraphManager.IsPositionInWall(altTarget))
            //{
            //    finalTarget = altTarget;
            //}

            //Pathfinder.FollowPath(finalTarget, true);
            //VelocityComponent.Move();

            if (TryGetComponent<Pathfinder>(out var pathfinder))
            {
                pathfinder.FollowPath(target, _speed);
            }
            else if (TryGetComponent<VelocityComponent>(out var velocityComponent))
            {
                var dir = target - Position;
                dir.Normalize();
                velocityComponent.Move(dir, _speed);
            }

            return TaskStatus.Running;
        }

        #endregion
    }
}
