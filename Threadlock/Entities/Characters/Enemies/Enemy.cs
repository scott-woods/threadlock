using Microsoft.Xna.Framework;
using Nez;
using Nez.AI.BehaviorTrees;
using Nez.Sprites;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Threadlock.Actions;
using Threadlock.Components;
using Threadlock.DebugTools;
using Threadlock.Helpers;
using Threadlock.Models;
using Threadlock.StaticData;
using TaskStatus = Nez.AI.BehaviorTrees.TaskStatus;

namespace Threadlock.Entities.Characters.Enemies
{
    public class Enemy : Entity
    {
        public float BaseSpeed { get; }
        public BehaviorConfig BehaviorConfig;
        public bool IsOnCooldown = false;
        public bool WantsLineOfSight;
        public float MinDistance;
        public float MaxDistance;
        public bool IsPursued;

        EnemyData _config;

        SpriteAnimator _animator;

        ITimer _pursuitTimer;

        public virtual Entity TargetEntity
        {
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
                Entity targetEntity = null;
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

        BehaviorTree<Enemy> _tree;
        List<EnemyAction> _actions;
        EnemyAction _queuedAction;

        bool _spawning = false;

        ICoroutine _actionCoroutine;
        ICoroutine _handleActionCoroutine;
        bool _isActionFinished = false;

        Mover _mover;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="config"></param>
        public Enemy(EnemyConfig config) : base(config.Name)
        {
            //_config = config;

            BehaviorConfig = config.BehaviorConfig;
            BaseSpeed = config.BaseSpeed;

            //status
            var statusComponent = AddComponent(new StatusComponent(StatusPriority.Normal));
            statusComponent.Emitter.AddObserver(StatusEvents.Changed, OnStatusChanged);

            //RENDERERS
            _animator = AddComponent(new SpriteAnimator());
            _animator.SetLocalOffset(config.AnimatorOffset);
            _animator.SetRenderLayer(RenderLayers.YSort);
            AnimatedSpriteHelper.LoadAnimations(ref _animator, config.IdleAnimation, config.MoveAnimation, config.HitAnimation, config.DeathAnimation, config.SpawnAnimation);

            AddComponent(new Shadow(_animator));

            AddComponent(new SelectionComponent(_animator, 10));

            AddComponent(new SpriteFlipper());


            //PHYSICS
            _mover = AddComponent(new Mover());

            var projectileMover = AddComponent(new ProjectileMover());

            var velocityComponent = AddComponent(new VelocityComponent());

            var collider = AddComponent(new BoxCollider(config.ColliderOffset.X, config.ColliderOffset.Y, config.ColliderSize.X, config.ColliderSize.Y));
            Flags.SetFlagExclusive(ref collider.PhysicsLayer, PhysicsLayers.EnemyCollider);
            collider.CollidesWithLayers = 0;
            Flags.SetFlag(ref collider.CollidesWithLayers, PhysicsLayers.Environment);
            Flags.SetFlag(ref collider.CollidesWithLayers, PhysicsLayers.EnemyCollider);
            Flags.SetFlag(ref collider.CollidesWithLayers, PhysicsLayers.ProjectilePassableWall);

            var hurtboxCollider = AddComponent(new BoxCollider(config.HurtboxSize.X, config.HurtboxSize.Y));
            hurtboxCollider.IsTrigger = true;
            Flags.SetFlagExclusive(ref hurtboxCollider.PhysicsLayer, PhysicsLayers.EnemyHurtbox);
            //Flags.SetFlagExclusive(ref hurtboxCollider.CollidesWithLayers, PhysicsLayers.PlayerHitbox);
            hurtboxCollider.CollidesWithLayers = 0;
            var hurtbox = AddComponent(new Hurtbox(hurtboxCollider, 0f, Nez.Content.Audio.Sounds.Chain_bot_damaged));
            hurtbox.Emitter.AddObserver(HurtboxEventTypes.Hit, OnHurtboxHit);

            AddComponent(new KnockbackComponent(velocityComponent, config.HitAnimation, 150, .5f));


            //OTHER
            AddComponent(new HealthComponent(config.MaxHealth, config.MaxHealth));

            AddComponent(new DeathComponent(config.DeathAnimation, Nez.Content.Audio.Sounds.Enemy_death_1));

            AddComponent(new Pathfinder(collider));

            AddComponent(new OriginComponent(collider));

            AddComponent(new LootDropper(LootTables.BasicEnemy));


            //ACTIONS
            _actions = new List<EnemyAction>();
            foreach (var actionString in config.Actions)
            {
                if (AllEnemyActions.TryCreateEnemyAction(actionString, this, out var action))
                {
                    _actions.Add(action);
                    action.LoadAnimations(ref _animator);
                }
            }

            //BEHAVIOR
            _tree = BehaviorTrees.CreateBehaviorTree(this, config.BehaviorConfig.BehaviorTreeName);
        }

        public Enemy(EnemyData config) : base(config.Name)
        {
            _config = config;

            BehaviorConfig = config.BehaviorConfig;
            BaseSpeed = config.BaseSpeed;

            //status
            var statusComponent = AddComponent(new StatusComponent(StatusPriority.Normal));
            statusComponent.Emitter.AddObserver(StatusEvents.Changed, OnStatusChanged);

            //RENDERERS
            _animator = AddComponent(new SpriteAnimator());
            _animator.SetLocalOffset(config.AnimatorOffset);
            _animator.SetRenderLayer(RenderLayers.YSort);
            AnimatedSpriteHelper.LoadAnimations(ref _animator, config.IdleAnimation, config.MoveAnimation, config.HitAnimation, config.DeathAnimation, config.SpawnAnimation);

            AddComponent(new Shadow(_animator));

            AddComponent(new SelectionComponent(_animator, 10));

            AddComponent(new SpriteFlipper());


            //PHYSICS
            _mover = AddComponent(new Mover());

            var projectileMover = AddComponent(new ProjectileMover());

            var velocityComponent = AddComponent(new VelocityComponent());

            var collider = AddComponent(new BoxCollider(config.ColliderOffset.X, config.ColliderOffset.Y, config.ColliderSize.X, config.ColliderSize.Y));
            Flags.SetFlagExclusive(ref collider.PhysicsLayer, PhysicsLayers.EnemyCollider);
            collider.CollidesWithLayers = 0;
            Flags.SetFlag(ref collider.CollidesWithLayers, PhysicsLayers.Environment);
            Flags.SetFlag(ref collider.CollidesWithLayers, PhysicsLayers.EnemyCollider);
            Flags.SetFlag(ref collider.CollidesWithLayers, PhysicsLayers.ProjectilePassableWall);

            var hurtboxCollider = AddComponent(new BoxCollider(config.HurtboxSize.X, config.HurtboxSize.Y));
            hurtboxCollider.IsTrigger = true;
            Flags.SetFlagExclusive(ref hurtboxCollider.PhysicsLayer, PhysicsLayers.EnemyHurtbox);
            //Flags.SetFlagExclusive(ref hurtboxCollider.CollidesWithLayers, PhysicsLayers.PlayerHitbox);
            hurtboxCollider.CollidesWithLayers = 0;
            var hurtbox = AddComponent(new Hurtbox(hurtboxCollider, 0f, Nez.Content.Audio.Sounds.Chain_bot_damaged));
            hurtbox.Emitter.AddObserver(HurtboxEventTypes.Hit, OnHurtboxHit);

            AddComponent(new KnockbackComponent(velocityComponent, config.HitAnimation, 150, .5f));


            //OTHER
            AddComponent(new HealthComponent(config.MaxHealth, config.MaxHealth));

            AddComponent(new DeathComponent(config.DeathAnimation, Nez.Content.Audio.Sounds.Enemy_death_1));

            AddComponent(new Pathfinder(collider));

            AddComponent(new OriginComponent(collider));

            AddComponent(new LootDropper(LootTables.BasicEnemy));


            //ACTIONS
            _actions = new List<EnemyAction>();
            foreach (var actionString in config.Actions)
            {
                if (AllEnemyActions.TryCreateEnemyAction(actionString, this, out var action))
                {
                    _actions.Add(action);
                    action.LoadAnimations(ref _animator);
                }
            }

            //BEHAVIOR
            _tree = BehaviorTrees.CreateBehaviorTree(this, config.BehaviorConfig.BehaviorTreeName);
        }

        #region LIFECYCLE

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            StartActionCooldown();

            Game1.StartCoroutine(Spawn());
        }

        public override void OnRemovedFromScene()
        {
            base.OnRemovedFromScene();

            ResetAction();
        }

        public override void Update()
        {
            base.Update();

            if (!_spawning)
                _tree.Tick();
        }

        #endregion

        void ResetAction()
        {
            _queuedAction?.Abort();
            _queuedAction = null;

            _isActionFinished = false;

            _handleActionCoroutine?.Stop();
            _handleActionCoroutine = null;

            _actionCoroutine?.Stop();
            _actionCoroutine = null;
        }

        #region OBSERVERS

        void OnStatusChanged(StatusPriority status)
        {
            if (status != StatusPriority.Normal)
            {
                ResetAction();
            }
        }

        void OnHurtboxHit(HurtboxHit hit)
        {
            if (BehaviorConfig.RunsWhenPursued)
            {
                _pursuitTimer?.Stop();
                _pursuitTimer = null;

                IsPursued = true;
                _pursuitTimer = Game1.Schedule(BehaviorConfig.PursuitTime, timer =>
                {
                    IsPursued = false;
                    _pursuitTimer = null;
                });
            }
        }

        #endregion

        #region CHECKS

        public StatusPriority CheckStatus()
        {
            if (TryGetComponent<StatusComponent>(out var status))
                return status.CurrentStatusPriority;
            else return StatusPriority.Normal;
        }

        public bool ShouldRunSubTree()
        {
            if (!DebugSettings.EnemyAIEnabled)
                return false;

            if (TryGetComponent<StatusComponent>(out var status))
            {
                if (status.CurrentStatusPriority != StatusPriority.Normal)
                    return false;
            }

            return true;
        }

        #endregion

        IEnumerator HandleAction(EnemyAction action)
        {
            if (action == null)
                yield break;

            _isActionFinished = false;

            _actionCoroutine = Game1.StartCoroutine(action.Execute());
            yield return _actionCoroutine;

            _isActionFinished = true;
        }

        #region TASKS

        public TaskStatus GetInRange()
        {
            var currentPos = Position;
            if (TryGetComponent<OriginComponent>(out var oc))
                currentPos = oc.Origin;

            var idealPositions = new List<Vector2>();

            foreach (var action in _actions)
            {
                if (action.IsOnCooldown)
                    continue;

                idealPositions.Add(action.GetIdealPosition());
            }

            var targetPos = idealPositions.MinBy(p => Vector2.Distance(p, currentPos));

            if (targetPos == currentPos)
                return Idle(true);
            else
                return MoveToTarget(targetPos, BaseSpeed);
        }

        /// <summary>
        /// sets the queued action if successful
        /// </summary>
        /// <returns></returns>
        public TaskStatus TryQueueAction()
        {
            //don't queue anything if on cooldown
            if (IsOnCooldown)
                return TaskStatus.Failure;

            //split actions into groups by priority
            var groups = _actions.OrderByDescending(a => a.Priority).GroupBy(a => a.Priority).Select(g => g.ToList());

            //try each priority group
            foreach (var group in groups)
            {
                //init valid actions list
                var validActions = new List<EnemyAction>();

                //check each action in the group
                foreach (var action in group)
                {
                    //if the action can execute, add it to valid actions
                    if (action.CanExecute())
                        validActions.Add(action);
                }

                //if any valid actions in this group, pick a random one to do
                if (validActions.Any())
                {
                    _queuedAction = validActions.RandomItem();
                    return TaskStatus.Success;
                }
            }

            //no valid actions found, return failure
            return TaskStatus.Failure;
        }

        public TaskStatus ExecuteQueuedAction()
        {
            //called the first time to start the coroutine
            if (_handleActionCoroutine == null)
            {
                _handleActionCoroutine = Game1.StartCoroutine(HandleAction(_queuedAction));
                return TaskStatus.Running;
            }
            else
            {
                if (_isActionFinished)
                {
                    ResetAction();
                    StartActionCooldown();
                    return TaskStatus.Success;
                }
                else
                    return TaskStatus.Running;
            }
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

        public virtual TaskStatus MoveToTarget(Entity target, float speed)
        {
            if (target.TryGetComponent<OriginComponent>(out var originComponent))
                return MoveToTarget(originComponent.Origin, speed);
            else return MoveToTarget(target.Position, speed);
        }

        public virtual TaskStatus MoveToTarget(Vector2 target, float speed)
        {
            //handle animation
            AnimatedSpriteHelper.PlayAnimation(ref _animator, _config.MoveAnimation);

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

        public TaskStatus MoveAway(Entity target, float speed)
        {
            if (target.TryGetComponent<OriginComponent>(out var originComponent))
                return MoveAway(originComponent.Origin, speed);
            else return MoveAway(target.Position, speed);
        }

        public TaskStatus MoveAway(Vector2 target, float speed)
        {
            //handle animation
            AnimatedSpriteHelper.PlayAnimation(ref _animator, _config.MoveAnimation);

            var enemyPos = Position;
            if (TryGetComponent<OriginComponent>(out var enemyOrigin))
                enemyPos = enemyOrigin.Origin;

            var dir = enemyPos - target;
            dir.Normalize();

            if (TryGetComponent<VelocityComponent>(out var velocityComponent))
                velocityComponent.Move(dir, speed);

            return TaskStatus.Running;
        }

        public virtual TaskStatus Idle(bool trackTarget = false)
        {
            AnimatedSpriteHelper.PlayAnimation(ref _animator, _config.IdleAnimation);

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

        void StartActionCooldown()
        {
            //handle cooldown
            IsOnCooldown = true;
            Game1.Schedule(BehaviorConfig.ActionCooldown, timer => IsOnCooldown = false);
        }

        IEnumerator Spawn()
        {
            _spawning = true;

            if (!string.IsNullOrWhiteSpace(_config.SpawnAnimation))
                yield return AnimatedSpriteHelper.WaitForAnimation(_animator, _config.SpawnAnimation);
            else
                AnimatedSpriteHelper.PlayAnimation(ref _animator, _config.IdleAnimation);

            _spawning = false;
        }
    }
}
