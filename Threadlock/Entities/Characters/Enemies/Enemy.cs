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
using Threadlock.DebugTools;
using Threadlock.Entities.Characters.Enemies.ChainBot;
using Threadlock.Entities.Characters.Player.States;
using Threadlock.Helpers;
using Threadlock.StaticData;
using TaskStatus = Nez.AI.BehaviorTrees.TaskStatus;

namespace Threadlock.Entities.Characters.Enemies
{
    public abstract class Enemy<T> : BaseEnemy where T : Enemy<T>
    {
        Entity _targetEntity;
        public virtual Entity TargetEntity {
            get
            {
                //get all entities with the enemy target tag
                var targets = Scene.FindEntitiesWithTag(EntityTags.EnemyTarget);

                //where to consider enemy position from
                var enemyPos = Position;
                if (TryGetComponent<OriginComponent>(out var enemyOc))
                    enemyPos = enemyOc.Origin;

                //loop through potential targets and determine the closest one
                var minDistance = float.MaxValue;
                Entity targetEntity = Player.Player.Instance;
                foreach (var target in targets)
                {
                    //get position
                    Vector2 targetPos = target.Position;
                    if (target.TryGetComponent<OriginComponent>(out var oc))
                        targetPos = oc.Origin;

                    var dist = Vector2.Distance(enemyPos, targetPos);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        targetEntity = target;
                    }
                }

                return targetEntity;
            }
        }

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

            //additional setup
            Setup();

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

            if (!DebugSettings.EnemyAIEnabled)
                return false;

            if (_statusComponent.CurrentStatusPriority != StatusPriority.Normal) return false;
            return true;
        }

        public virtual BehaviorTree<T> CreateBehaviorTree()
        {
            var tree = BehaviorTreeBuilder<T>.Begin(this as T)
                .Selector()
                    .Sequence(AbortTypes.LowerPriority)
                        .Conditional(c => c.ShouldAbort())
                        .Action(c => c.AbortActions())
                    .EndComposite()
                    .Sequence(AbortTypes.LowerPriority)
                        .Conditional(c => c.ShouldRunSubTree())
                        .SubTree(_subTree)
                    .EndComposite()
                    .Sequence(AbortTypes.LowerPriority)
                        .Action(c => c.Idle())
                    .EndComposite()
                    //.ConditionalDecorator(c => c.ShouldAbort(), true)
                    //    .Action(c => c.AbortActions())
                    ////.ConditionalDecorator(c =>
                    ////{
                    ////    var gameStateManager = Game1.GameStateManager;
                    ////    return gameStateManager.GameState != GameState.Combat;
                    ////}, true)
                    ////    .Sequence()
                    ////        .Action(c => c.AbortActions())
                    ////        .Action(c => c.Idle())
                    ////    .EndComposite()
                    //.ConditionalDecorator(c => c.ShouldRunSubTree(), true)
                    //    .SubTree(_subTree)

                .EndComposite()
                .Build();

            tree.UpdatePeriod = 0;

            return tree;
        }

        //public virtual Entity GetTarget()
        //{
        //    return Player.Player.Instance;
        //}

        #endregion

        #region ABSTRACT METHODS

        public abstract BehaviorTree<T> CreateSubTree();

        public abstract void Setup();

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

        /// <summary>
        /// watch a target, but don't move towards it
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public virtual TaskStatus TrackTarget(Vector2 target)
        {
            if (TryGetComponent<VelocityComponent>(out var velocityComponent))
            {
                var dir = target - Position;
                dir.Normalize();
                velocityComponent.LastNonZeroDirection = dir;
            }

            return TaskStatus.Running;
        }

        public virtual TaskStatus TrackTarget(Entity target)
        {
            var pos = target.Position;
            if (target.TryGetComponent<OriginComponent>(out var originComponent))
                pos = originComponent.Origin;
            return TrackTarget(pos);
        }

        public virtual TaskStatus MoveToTarget(Entity target, float speed)
        {
            if (target.TryGetComponent<OriginComponent>(out var originComponent))
                return MoveToTarget(originComponent.Origin, speed);
            else return MoveToTarget(target.Position, speed);
        }

        public virtual TaskStatus MoveToTarget(Vector2 target, float speed)
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
                pathfinder.FollowPath(target, speed);
            }
            else if (TryGetComponent<VelocityComponent>(out var velocityComponent))
            {
                var dir = target - Position;
                dir.Normalize();
                velocityComponent.Move(dir, speed);
            }

            return TaskStatus.Running;
        }

        public virtual TaskStatus MoveAway(Entity target, float speed, float desiredDistance)
        {
            if (target.TryGetComponent<OriginComponent>(out var originComponent))
                return MoveAway(originComponent.Origin, speed, desiredDistance);
            else return MoveAway(target.Position, speed, desiredDistance);
        }

        public virtual TaskStatus MoveAway(Vector2 target, float speed, float desiredDistance)
        {
            //handle animation
            if (TryGetComponent<SpriteAnimator>(out var animator))
            {
                if (animator.Animations.ContainsKey("Run") && !animator.IsAnimationActive("Run"))
                {
                    animator.Play("Run");
                }
            }

            var enemyPos = Position;
            if (TryGetComponent<OriginComponent>(out var enemyOrigin))
                enemyPos = enemyOrigin.Origin;

            var dir = enemyPos - target;
            dir.Normalize();

            if (TryGetComponent<VelocityComponent>(out var velocityComponent))
                velocityComponent.Move(dir, speed);

            if (EntityHelper.DistanceToEntity(this, TargetEntity) > desiredDistance)
                return TaskStatus.Success;

            return TaskStatus.Running;
        }

        public virtual TaskStatus Idle(bool trackTarget = false)
        {
            if (TryGetComponent<SpriteAnimator>(out var animator))
            {
                if (!animator.IsAnimationActive("Idle"))
                    animator.Play("Idle");
            }

            if (trackTarget)
            {
                var pos = TargetEntity.Position;
                if (TargetEntity.TryGetComponent<OriginComponent>(out var originComponent))
                    pos = originComponent.Origin;

                if (TryGetComponent<VelocityComponent>(out var velocityComponent))
                {
                    var dir = pos - Position;
                    dir.Normalize();
                    velocityComponent.LastNonZeroDirection = dir;
                }
            }

            return TaskStatus.Running;
        }

        #endregion
    }
}
