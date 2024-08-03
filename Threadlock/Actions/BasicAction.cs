using Microsoft.Xna.Framework;
using Nez;
using Nez.Persistence;
using Nez.Sprites;
using Nez.Tweens;
using System;
using System.Collections;
using System.Collections.Generic;
using Threadlock.Components;
using Threadlock.Entities;
using Threadlock.Helpers;
using Threadlock.StaticData;
using Random = Nez.Random;

namespace Threadlock.Actions
{
    public abstract class BasicAction
    {
        public string Name;

        public ActionPhase PreActionPhase;
        public ActionPhase ActionPhase;
        public ActionPhase PostActionPhase;

        public List<string> AttackSounds;

        public List<AttackProjectile> Projectiles = new List<AttackProjectile>();

        public bool IsCombo;
        public List<string> ComboActions = new List<string>();

        [JsonExclude]
        public Entity Context;

        BasicAction _currentComboAction;
        ICoroutine _currentComboActionExecuteCoroutine;
        ICoroutine _comboCoroutine;
        ICoroutine _movementCoroutine;
        ActionPhase _currentPhase;
        ICoroutine _currentActionCoroutine;

        /// <summary>
        /// Execute the action
        /// </summary>
        /// <returns></returns>
        public IEnumerator Execute()
        {
            OnExecutionStarted();

            if (IsCombo)
            {
                _comboCoroutine = Core.StartCoroutine(HandleCombo());
                yield return _comboCoroutine;

                yield break;
            }

            //get targeting info
            var targetingInfo = GetTargetingInfo();

            //get necessary components from entity
            var animator = Context.GetComponent<SpriteAnimator>();
            var velocityComponent = Context.GetComponent<VelocityComponent>();

            //pre action phase
            if (PreActionPhase != null)
            {
                _currentPhase = PreActionPhase;
                _currentActionCoroutine = Core.StartCoroutine(PreActionPhase.ExecuteActionPhase(Context, targetingInfo));
                yield return _currentActionCoroutine;
            }

            //get dir towards target
            var dirTowardsTarget = Vector2.Zero;
            if (targetingInfo.Direction != null)
                dirTowardsTarget = targetingInfo.Direction.Value;
            else if (targetingInfo.Position != null)
                dirTowardsTarget = targetingInfo.Position.Value - Context.Position;
            else if (targetingInfo.TargetEntity != null)
                dirTowardsTarget = targetingInfo.TargetEntity.Position - Context.Position;

            //normalize dir towards target
            if (dirTowardsTarget != Vector2.Zero)
                dirTowardsTarget.Normalize();

            //handle projectiles
            foreach (var attackProjectile in Projectiles)
            {
                if (Projectiles2.TryGetProjectile(attackProjectile.ProjectileName, out var projectileConfig))
                {
                    //create projectile entity
                    var projectileEntity = ProjectileEntity.CreateProjectileEntity(projectileConfig, dirTowardsTarget, Context);

                    //determine if we should start from enemy or target pos
                    Vector2 pos = Vector2.Zero;
                    if (!projectileConfig.AttachToOwner)
                    {
                        //check if we should start from the target
                        if (attackProjectile.StartFromTarget && (targetingInfo.TargetEntity != null || targetingInfo.Position != null))
                            pos = targetingInfo.TargetEntity != null ? targetingInfo.TargetEntity.Position : targetingInfo.Position.Value;
                        else
                            pos = Context.Position;
                    }
                    else
                        projectileEntity.SetParent(Context);

                    //add entity offset
                    pos += attackProjectile.EntityOffset;

                    //add offset to starting pos
                    pos += dirTowardsTarget * attackProjectile.OffsetDistance;

                    //add more offset if we should predict the target
                    if (attackProjectile.PredictTarget && targetingInfo.TargetEntity != null)
                    {
                        if (targetingInfo.TargetEntity.TryGetComponent<VelocityComponent>(out var vc))
                        {
                            var predictDir = vc.Direction;
                            var predictOffset = Random.Range(attackProjectile.MinPredictionOffset, attackProjectile.MaxPredictionOffset);
                            pos += predictDir * predictOffset;
                        }
                    }

                    //set the projectile position
                    projectileEntity.SetPosition(pos);

                    //handle rotation
                    if (projectileConfig.ShouldRotate)
                    {
                        var rotationRadians = DirectionHelper.GetClampedAngle(dirTowardsTarget, projectileConfig.MaxRotation);
                        projectileEntity.SetRotation(rotationRadians);
                    }

                    //add to scene either after delay or immediately
                    if (attackProjectile.Delay > 0)
                        Core.Schedule(attackProjectile.Delay, timer => Context.Scene.AddEntity(projectileEntity));
                    else
                        Context.Scene.AddEntity(projectileEntity);
                }
            }

            //handle attack sound
            if (AttackSounds != null && AttackSounds.Count > 0)
                Game1.AudioManager.PlaySound(AttackSounds.RandomItem());

            //action phase
            if (ActionPhase != null)
            {
                _currentPhase = ActionPhase;
                _currentActionCoroutine = Core.StartCoroutine(ActionPhase.ExecuteActionPhase(Context, targetingInfo));
                yield return _currentActionCoroutine;
            }

            //post action phase
            if (PostActionPhase != null)
            {
                _currentPhase = PostActionPhase;
                _currentActionCoroutine = Core.StartCoroutine(PostActionPhase.ExecuteActionPhase(Context, targetingInfo));
                yield return _currentActionCoroutine;
            }

            OnExecutionEnded();
        }

        /// <summary>
        /// called if the action needs to be aborted immediately
        /// </summary>
        public virtual void Abort()
        {
            _currentComboActionExecuteCoroutine?.Stop();
            _currentComboActionExecuteCoroutine = null;

            _currentComboAction?.Abort();
            _currentComboAction = null;

            _comboCoroutine?.Stop();
            _comboCoroutine = null;

            _currentPhase?.Abort();
            _currentPhase = null;

            _currentActionCoroutine?.Stop();
            _currentActionCoroutine = null;

            _movementCoroutine?.Stop();
            _movementCoroutine = null;
        }

        /// <summary>
        /// load animations onto the sprite animator of the owner
        /// </summary>
        /// <param name="animator"></param>
        public virtual void LoadAnimations(ref SpriteAnimator animator)
        {
            PreActionPhase?.LoadAnimations(animator);
            ActionPhase?.LoadAnimations(animator);
            PostActionPhase?.LoadAnimations(animator);

            if (IsCombo && ComboActions.Count > 0)
            {
                foreach (var comboAction in ComboActions)
                {
                    if (AllEnemyActions.TryGetBaseEnemyAction(comboAction, out var childAction))
                        childAction.LoadAnimations(ref animator);
                    else if (AllPlayerActions.TryGetBasePlayerAction(comboAction, out var playerChildAction))
                        playerChildAction.LoadAnimations(ref animator);
                }
            }
        }

        /// <summary>
        /// called once when execution of the action starts
        /// </summary>
        protected virtual void OnExecutionStarted() { }

        /// <summary>
        /// called once when execution of the action ends
        /// </summary>
        protected virtual void OnExecutionEnded() { }

        /// <summary>
        /// handles a combo action container
        /// </summary>
        /// <returns></returns>
        protected IEnumerator HandleCombo()
        {
            foreach (var comboActionString in ComboActions)
            {
                if (AllEnemyActions.TryCreateEnemyAction(comboActionString, Context, out var enemyChildAction))
                {
                    _currentComboAction = enemyChildAction;
                }
                else if (AllPlayerActions.TryCreatePlayerAction(comboActionString, Context, out var playerChildAction))
                {
                    _currentComboAction = playerChildAction;
                }

                _currentComboActionExecuteCoroutine = Core.StartCoroutine(_currentComboAction.Execute());
                yield return _currentComboActionExecuteCoroutine;
            }
        }

        /// <summary>
        /// get data for which direction/entity/position the owner is targeting
        /// </summary>
        /// <returns></returns>
        protected abstract TargetingInfo GetTargetingInfo();
    }

    public class TargetingInfo
    {
        public Vector2? Direction;
        public Vector2? Position;
        public Entity TargetEntity;
    }

    public class ActionPhase
    {
        public string Animation;
        public MovementConfig2 Movement;
        public float Duration;

        public bool WaitForAnimation;
        public bool WaitForMovement;

        ICoroutine _animationCoroutine;
        ICoroutine _movementCoroutine;

        public IEnumerator ExecuteActionPhase(Entity entity, TargetingInfo targetingInfo)
        {
            //retrieve animator
            var animator = entity.GetComponent<SpriteAnimator>();

            //play animation
            var isAnimationCompleted = false;
            if (!string.IsNullOrWhiteSpace(Animation))
            {
                _animationCoroutine = Core.StartCoroutine(CoroutineHelper.CoroutineWrapper(AnimatedSpriteHelper.WaitForAnimation(animator, Animation), () => isAnimationCompleted = true));
            }

            //start movement
            var isMovementCompleted = false;
            if (Movement != null)
            {
                _movementCoroutine = Core.StartCoroutine(CoroutineHelper.CoroutineWrapper(Movement.HandleMovement(entity, targetingInfo), () => isMovementCompleted = true));
            }

            //wait for specified duration, checking if animation or movement is done if necessary
            var timer = 0f;
            while (timer < Duration || WaitForAnimation && !isAnimationCompleted || WaitForMovement && !isMovementCompleted)
            {
                timer += Time.DeltaTime;
                yield return null;
            }
        }

        public void Abort()
        {
            _animationCoroutine?.Stop();
            _animationCoroutine = null;

            _movementCoroutine?.Stop();
            _movementCoroutine = null;
        }

        public void LoadAnimations(SpriteAnimator animator)
        {
            AnimatedSpriteHelper.LoadAnimation(Animation, ref animator, true);
        }
    }

    public class MovementConfig2
    {
        public MovementType2 MovementType;
        public float Speed;
        public float? FinalSpeed;
        public float? TimeToFinalSpeed;
        public EaseType EaseType;
        public float Duration;
        public bool UseAnimationDuration = true;

        public IEnumerator HandleMovement(Entity entity, TargetingInfo targetingInfo)
        {
            //handle instant movement
            if (MovementType == MovementType2.Instant)
            {
                if (targetingInfo.Position != null)
                    entity.Position = targetingInfo.Position.Value;

                yield break;
            }

            //get velocity component
            var velocityComponent = entity.GetComponent<VelocityComponent>();
            if (velocityComponent == null)
            {
                yield break;
            }

            //determine duration (use animation duration or specified)
            var duration = Duration;
            //if (UseAnimationDuration && entity.TryGetComponent<SpriteAnimator>(out var animator))
            //    duration = AnimatedSpriteHelper.GetAnimationDuration(animator);

            var initialSpeed = 0f;
            var dir = Vector2.Zero;
            switch (MovementType)
            {
                case MovementType2.ToPoint:
                    if (targetingInfo.Position == null)
                    {
                        yield break;
                    }
                    var dist = Vector2.Distance(targetingInfo.Position.Value, entity.Position);
                    initialSpeed = dist / duration;
                    dir = targetingInfo.Direction != null ? targetingInfo.Direction.Value : targetingInfo.Position.Value - entity.Position;
                    break;
                case MovementType2.Directional:
                    initialSpeed = Math.Abs(Speed);
                    if (targetingInfo.Direction == null)
                    {
                        yield break;
                    }
                    dir = targetingInfo.Direction.Value;
                    break;
                case MovementType2.DirectionalReverse:
                    initialSpeed = Math.Abs(Speed);
                    if (targetingInfo.Direction == null)
                    {
                        yield break;
                    }
                    dir = targetingInfo.Direction.Value * -1;
                    break;
            }

            if (dir != Vector2.Zero)
                dir.Normalize();

            if (Speed < 0)
                dir *= -1;

            var timer = 0f;
            while (timer < duration)
            {
                timer += Time.DeltaTime;

                var speed = initialSpeed;
                if (FinalSpeed.HasValue)
                {
                    var timeToFinalSpeed = TimeToFinalSpeed != null ? TimeToFinalSpeed.Value : duration;
                    if (timer >= timeToFinalSpeed)
                        speed = FinalSpeed.Value;
                    else
                        speed = Lerps.Ease(EaseType, speed, FinalSpeed.Value, timer, timeToFinalSpeed);
                }

                velocityComponent.Move(dir, speed, true);

                yield return null;
            }
        }
    }

    public enum MovementType2
    {
        ToPoint,
        Directional,
        DirectionalReverse,
        Instant
    }
}
