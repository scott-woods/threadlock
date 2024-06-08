using Microsoft.Xna.Framework;
using Nez;
using Nez.AI.BehaviorTrees;
using Nez.AI.GOAP;
using Nez.Sprites;
using Nez.Textures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Threadlock.Components;
using Threadlock.Components.EnemyActions;
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

        BehaviorTree<Enemy> _tree;
        //ScriptedEnemyAction _activeAction;
        //ScriptedEnemyAction _queuedAction;
        //List<ScriptedEnemyAction> _scriptedActions;
        //List<EnemyAction2> _actions;
        //EnemyAction2 _activeAction;
        //EnemyAction2 _queuedAction;
        List<EnemyAction3> _actions;
        EnemyAction3 _activeAction;
        EnemyAction3 _queuedAction;

        public Enemy(EnemyConfig config) : base(config.Name)
        {
            BehaviorConfig = config.BehaviorConfig;
            BaseSpeed = config.BaseSpeed;

            //status
            var statusComponent = AddComponent(new StatusComponent(StatusPriority.Normal));
            statusComponent.Emitter.AddObserver(StatusEvents.Changed, OnStatusChanged);

            //RENDERERS
            _animator = AddComponent(new SpriteAnimator());
            _animator.SetLocalOffset(config.AnimatorOffset);
            _animator.SetRenderLayer(RenderLayers.YSort);
            foreach (var spriteSheet in config.SpriteSheets)
            {
                var texture = Game1.Scene.Content.LoadTexture(@$"Content\Textures\Characters\{config.Name}\{spriteSheet.FileName}.png");
                var sprites = Sprite.SpritesFromAtlas(texture, spriteSheet.CellWidth, spriteSheet.CellHeight);
                var totalColumns = texture.Width / spriteSheet.CellWidth;
                foreach (var anim in spriteSheet.Animations)
                    _animator.AddAnimation(anim.Name, AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, anim.Row, anim.Frames, totalColumns));
            }
            _animator.OnAnimationCompletedEvent += OnAnimationCompleted;

            AddComponent(new Shadow(_animator));

            AddComponent(new SelectionComponent(_animator, 10));

            AddComponent(new SpriteFlipper());


            //PHYSICS
            var mover = AddComponent(new Mover());

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
            Flags.SetFlagExclusive(ref hurtboxCollider.CollidesWithLayers, PhysicsLayers.PlayerHitbox);
            var hurtbox = AddComponent(new Hurtbox(hurtboxCollider, 0f, Nez.Content.Audio.Sounds.Chain_bot_damaged));

            AddComponent(new KnockbackComponent(velocityComponent, 150, .5f));


            //OTHER
            AddComponent(new HealthComponent(config.MaxHealth, config.MaxHealth));

            AddComponent(new DeathComponent("Die", Nez.Content.Audio.Sounds.Enemy_death_1));

            AddComponent(new Pathfinder(collider));

            AddComponent(new OriginComponent(collider));

            AddComponent(new LootDropper(LootTables.BasicEnemy));


            //ACTIONS
            //var assembly = Assembly.GetExecutingAssembly();
            //var actionTypes = assembly.GetTypes()
            //    .Where(t => t.IsSubclassOf(typeof(EnemyAction)) && !t.IsAbstract && t.Namespace == $"Threadlock.Components.EnemyActions.{config.Name}");
            //foreach (var actionType in actionTypes)
            //    AddComponent(Activator.CreateInstance(actionType) as EnemyAction);

            //_scriptedActions = config.Actions;
            //for (int i = 0; i < _scriptedActions.Count; i++)
            //    _scriptedActions[i].Enemy = this;

            //_actions = config.AvailableActions;

            _actions = new List<EnemyAction3>();
            foreach (var actionString in config.NewActions)
            {
                if (AllEnemyActions.TryGetAction(actionString, out var action))
                {
                    _actions.Add(action);
                    action.LoadAnimations(ref _animator);
                }
            }

            //BEHAVIOR
            _tree = BehaviorTrees.CreateBehaviorTree(this, config.BehaviorConfig.BehaviorTreeName);

            hurtbox.Emitter.AddObserver(HurtboxEventTypes.Hit, OnHurtboxHit);
        }

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            Game1.SceneManager.Emitter.AddObserver(GlobalManagers.SceneManagerEvents.SceneChangeStarted, OnSceneChange);

            StartActionCooldown();
        }

        public override void OnRemovedFromScene()
        {
            base.OnRemovedFromScene();

            Game1.SceneManager.Emitter.RemoveObserver(GlobalManagers.SceneManagerEvents.SceneChangeStarted, OnSceneChange);
        }

        public override void Update()
        {
            base.Update();

            _tree.Tick();
        }

        void StartActionCooldown()
        {
            //handle cooldown
            IsOnCooldown = true;
            Game1.Schedule(BehaviorConfig.ActionCooldown, timer => IsOnCooldown = false);
        }

        #region OBSERVERS

        void OnAnimationCompleted(string animationName)
        {
            if (Animations.TryGetAnimationConfig(animationName, out var config))
            {
                AnimatedSpriteHelper.PlayAnimation(ref _animator, config.ChainTo);
            }
        }

        void OnSceneChange()
        {
            _activeAction?.Abort(this);
            _activeAction = null;
        }

        void OnStatusChanged(StatusPriority status)
        {
            if (status != StatusPriority.Normal)
            {
                _activeAction?.Abort(this);
                _activeAction = null;
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

        public bool TryQueueAction()
        {
            //if on cooldown, can't queue any action
            if (IsOnCooldown)
                return false;

            //split actions into groups by priority
            var groups = _actions.OrderByDescending(a => a.Priority).GroupBy(a => a.Priority).Select(g => g.ToList());

            //try each priority group
            foreach (var group in groups)
            {
                //init valid actions list
                var validActions = new List<EnemyAction3>();

                //check each action in the group
                foreach (var action in group)
                {
                    //if the action can execute, add it to valid actions
                    if (action.CanExecute(this))
                        validActions.Add(action);
                }

                //if any valid actions in this group, pick a random one to do
                if (validActions.Any())
                {
                    _queuedAction = validActions.RandomItem();
                    return true;
                }
            }

            //no valid actions found, return false
            return false;

            //var actions = GetComponents<EnemyAction>();
            //var groups = actions.OrderBy(a => a.Priority).GroupBy(a => a.Priority).Select(g => g.ToList());
            //foreach (var group in groups)
            //{
            //    foreach (var action in group)
            //    {
            //        List<EnemyAction> validActions = new List<EnemyAction>();
            //        if (action.IsOnCooldown)
            //            continue;
            //        if (action.CanExecute())
            //            validActions.Add(action);

            //        if (validActions.Any())
            //        {
            //            _queuedAction = validActions.RandomItem();
            //            return true;
            //        }
            //    }
            //}

            //return false;
        }

        public bool CanExecuteAction(EnemyAction action)
        {
            if (IsOnCooldown)
                return false;

            if (_activeAction != null)
                return false;

            if (action.IsOnCooldown)
                return false;

            return action.CanExecute();
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

        public bool IsTooClose()
        {
            return false;
        }

        public bool IsTooFar()
        {
            return true;
        }

        #endregion

        #region TASKS

        public TaskStatus ExecuteQueuedAction()
        {
            if (_activeAction == null && _queuedAction == null)
                return TaskStatus.Failure;

            if (_activeAction == null)
            {
                _activeAction = _queuedAction;
                _queuedAction = null;
                Game1.StartCoroutine(_activeAction.BeginExecution(this));
            }

            //return running as long as action is active
            if (_activeAction.IsActive)
                return TaskStatus.Running;
            else
            {
                //set active and queued action to null
                _activeAction = null;
                _queuedAction = null;

                StartActionCooldown();

                //return task success
                return TaskStatus.Success;
            }
        }

        //public TaskStatus ExecuteAction(EnemyAction action)
        //{
        //    //if active action isn't set to this one, it's the first time calling this. set as active and start coroutine
        //    if (_activeAction != action)
        //    {
        //        _activeAction = action;

        //        Game1.StartCoroutine(action.BeginExecution());
        //    }

        //    //return running as long as action is active
        //    if (action.IsActive)
        //        return TaskStatus.Running;
        //    else
        //    {
        //        //set active action to null
        //        _activeAction = null;

        //        //handle cooldown
        //        IsOnCooldown = true;
        //        Game1.Schedule(BehaviorConfig.ActionCooldown, timer => IsOnCooldown = false);

        //        //return task success
        //        return TaskStatus.Success;
        //    }
        //}

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

        public TaskStatus MoveAway(Entity target, float speed)
        {
            if (target.TryGetComponent<OriginComponent>(out var originComponent))
                return MoveAway(originComponent.Origin, speed);
            else return MoveAway(target.Position, speed);
        }

        public TaskStatus MoveAway(Vector2 target, float speed)
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

            return TaskStatus.Success;
        }

        #endregion
    }
}
