﻿using Microsoft.Xna.Framework;
using Nez;
using Nez.Persistence;
using Nez.Sprites;
using Nez.Tweens;
using System;
using System.Collections;
using System.Collections.Generic;
using Threadlock.Components;
using Threadlock.Components.EnemyActions;
using Threadlock.Entities;
using Threadlock.Helpers;
using Threadlock.StaticData;
using Random = Nez.Random;

namespace Threadlock
{
    public abstract class BasicAction
    {
        public string Name;

        public string PreAttackAnimation;
        public float PreAttackDuration;
        public bool WaitForPreAttackAnimation;
        public MovementConfig2 PreAttackMovement;

        public string AttackAnimation;
        public float AttackDuration;
        public bool WaitForAttackAnimation;
        public MovementConfig2 AttackMovement;
        public string AttackSound;

        public string PostAttackAnimation;
        public float PostAttackDuration;
        public bool WaitForPostAttackAnimation;
        public MovementConfig2 PostAttackMovement;

        public List<AttackProjectile> Projectiles = new List<AttackProjectile>();

        public bool IsCombo;
        public List<string> ComboActions = new List<string>();

        [JsonExclude]
        public Entity Context;

        BasicAction _currentComboAction;
        ICoroutine _currentComboActionExecuteCoroutine;
        ICoroutine _comboCoroutine;
        ICoroutine _movementCoroutine;

        /// <summary>
        /// Execute the action
        /// </summary>
        /// <returns></returns>
        public IEnumerator Execute()
        {
            OnExecutionStarted();

            if (IsCombo)
            {
                _comboCoroutine = Game1.StartCoroutine(HandleCombo());
                yield return _comboCoroutine;

                yield break;
            }

            //get targeting info
            var targetingInfo = GetTargetingInfo();

            //get necessary components from entity
            var animator = Context.GetComponent<SpriteAnimator>();
            var velocityComponent = Context.GetComponent<VelocityComponent>();

            //if we have a pre attack animation, play it
            AnimatedSpriteHelper.PlayAnimation(ref animator, PreAttackAnimation);

            //handle pre attack movement
            if (PreAttackMovement != null)
                _movementCoroutine = Game1.StartCoroutine(PreAttackMovement.HandleMovement(Context, targetingInfo, PreAttackAnimation));

            //determine how long to wait for pre attack
            var preAttackAnimDuration = WaitForPreAttackAnimation ? AnimatedSpriteHelper.GetAnimationDuration(animator) : PreAttackDuration;
            var preAttackTimer = 0f;
            while ((!WaitForPreAttackAnimation && (preAttackTimer < PreAttackDuration)) || (WaitForPreAttackAnimation && AnimatedSpriteHelper.IsAnimationPlaying(animator, PreAttackAnimation)))
            {
                preAttackTimer += Time.DeltaTime;

                yield return null;
            }

            //stop pre attack movement if not null
            _movementCoroutine?.Stop();

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
                    pos += (dirTowardsTarget * attackProjectile.OffsetDistance);

                    //add more offset if we should predict the target
                    if (attackProjectile.PredictTarget && targetingInfo.TargetEntity != null)
                    {
                        if (targetingInfo.TargetEntity.TryGetComponent<VelocityComponent>(out var vc))
                        {
                            var predictDir = vc.Direction;
                            var predictOffset = Random.Range(attackProjectile.MinPredictionOffset, attackProjectile.MaxPredictionOffset);
                            pos += (predictDir * predictOffset);
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
                        Game1.Schedule(attackProjectile.Delay, timer => Context.Scene.AddEntity(projectileEntity));
                    else
                        Context.Scene.AddEntity(projectileEntity);
                }
            }

            //handle attack sound
            Game1.AudioManager.PlaySound(AttackSound);

            //if we have an attack animation, play it
            AnimatedSpriteHelper.PlayAnimation(ref animator, AttackAnimation);

            if (AttackMovement != null)
                _movementCoroutine = Game1.StartCoroutine(AttackMovement.HandleMovement(Context, targetingInfo, AttackAnimation));

            //determine how long to wait for attack
            var attackAnimDuration = WaitForAttackAnimation ? AnimatedSpriteHelper.GetAnimationDuration(animator) : AttackDuration;
            var attackTimer = 0f;
            while ((!WaitForAttackAnimation && (attackTimer < AttackDuration)) || (WaitForAttackAnimation && AnimatedSpriteHelper.IsAnimationPlaying(animator, AttackAnimation)))
            {
                attackTimer += Time.DeltaTime;
                yield return null;
            }

            _movementCoroutine?.Stop();

            //if we have a post attack animation, play it
            AnimatedSpriteHelper.PlayAnimation(ref animator, PostAttackAnimation);

            if (PostAttackMovement != null)
                _movementCoroutine = Game1.StartCoroutine(PostAttackMovement?.HandleMovement(Context, targetingInfo, PostAttackAnimation));

            //determine how long to wait for post attack
            var postAttackAnimDuration = WaitForPostAttackAnimation ? AnimatedSpriteHelper.GetAnimationDuration(animator) : PostAttackDuration;
            var postAttackTimer = 0f;
            while ((!WaitForPostAttackAnimation && (postAttackTimer < PostAttackDuration)) || (WaitForPostAttackAnimation && AnimatedSpriteHelper.IsAnimationPlaying(animator, PostAttackAnimation)))
            {
                postAttackTimer += Time.DeltaTime;
                yield return null;
            }

            _movementCoroutine?.Stop();

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

            _movementCoroutine?.Stop();
            _movementCoroutine = null;
        }

        /// <summary>
        /// load animations onto the sprite animator of the owner
        /// </summary>
        /// <param name="animator"></param>
        public virtual void LoadAnimations(ref SpriteAnimator animator)
        {
            AnimatedSpriteHelper.LoadAnimationsGlobal(ref animator, PreAttackAnimation, AttackAnimation, PostAttackAnimation);

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

                _currentComboActionExecuteCoroutine = Game1.StartCoroutine(_currentComboAction.Execute());
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

    public class MovementConfig2
    {
        public MovementType2 MovementType;
        public float Speed;
        public float? FinalSpeed;
        public EaseType EaseType;
        public float Duration;
        public bool UseAnimationDuration = true;

        public IEnumerator HandleMovement(Entity entity, TargetingInfo targetingInfo, string animation)
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
                yield break;

            //determine duration (use animation duration or specified)
            var duration = Duration;
            if (UseAnimationDuration && entity.TryGetComponent<SpriteAnimator>(out var animator))
                duration = AnimatedSpriteHelper.GetAnimationDuration(animator);

            var initialSpeed = 0f;
            var dir = Vector2.Zero;
            switch (MovementType)
            {
                case MovementType2.ToPoint:
                    if (targetingInfo.Position == null)
                        yield break;
                    var dist = Vector2.Distance(targetingInfo.Position.Value, entity.Position);
                    initialSpeed = dist / duration;
                    dir = targetingInfo.Direction != null ? targetingInfo.Direction.Value : (targetingInfo.Position.Value - entity.Position);
                    break;
                case MovementType2.Directional:
                    initialSpeed = Speed;
                    if (targetingInfo.Direction == null)
                        yield break;
                    dir = targetingInfo.Direction.Value;
                    break;
                case MovementType2.DirectionalReverse:
                    initialSpeed = Speed;
                    if (targetingInfo.Direction == null)
                        yield break;
                    dir = targetingInfo.Direction.Value * -1;
                    break;
            }

            if (dir != Vector2.Zero)
                dir.Normalize();

            var timer = 0f;
            while (timer < duration)
            {
                timer += Time.DeltaTime;

                var speed = initialSpeed;
                if (FinalSpeed.HasValue)
                    speed = Lerps.Ease(EaseType, speed, FinalSpeed.Value, timer, duration);
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
