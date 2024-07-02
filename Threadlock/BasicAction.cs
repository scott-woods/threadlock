using Nez;
using Nez.Sprites;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Components.EnemyActions;
using Threadlock.Entities.Characters.Enemies;
using Threadlock.Entities;
using Threadlock.Helpers;
using Threadlock.StaticData;
using Nez.Tweens;
using Microsoft.Xna.Framework;
using Random = Nez.Random;

namespace Threadlock
{
    public abstract class BasicAction
    {
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

        public abstract TargetingInfo GetTargetingInfo(Entity entity);

        public virtual void LoadAnimations(ref SpriteAnimator animator)
        {
            AnimatedSpriteHelper.LoadAnimations(ref animator, PreAttackAnimation, AttackAnimation, PostAttackAnimation);

            if (IsCombo && ComboActions.Count > 0)
            {
                foreach (var comboAction in ComboActions)
                {
                    if (AllEnemyActions.TryGetAction(comboAction, out var childAction))
                        childAction.LoadAnimations(ref animator);
                    else if (AllPlayerActions.TryGetAction(comboAction, out var playerChildAction))
                        playerChildAction.LoadAnimations(ref animator);
                }
            }
        }

        public virtual IEnumerator Execute(Entity entity)
        {
            if (IsCombo)
            {
                foreach (var comboActionString in ComboActions)
                {
                    if (AllEnemyActions.TryGetAction(comboActionString, out var enemyChildAction))
                    {
                        var childAction = enemyChildAction.Clone() as BasicAction;
                        yield return childAction.Execute(entity);
                    }
                    else if (AllPlayerActions.TryGetAction(comboActionString, out var playerChildAction))
                    {
                        var childAction = playerChildAction.Clone() as BasicAction;
                        yield return childAction.Execute(entity);
                    }
                }

                yield break;
            }

            //get targeting info
            var targetingInfo = GetTargetingInfo(entity);

            //get necessary components from entity
            var animator = entity.GetComponent<SpriteAnimator>();
            var velocityComponent = entity.GetComponent<VelocityComponent>();

            //if we have a pre attack animation, play it
            AnimatedSpriteHelper.PlayAnimation(ref animator, PreAttackAnimation);

            //handle pre attack movement
            ICoroutine preAttackMovementCoroutine = null;
            if (PreAttackMovement != null)
                preAttackMovementCoroutine = Game1.StartCoroutine(PreAttackMovement.HandleMovement(entity, targetingInfo, PreAttackAnimation));

            //determine how long to wait for pre attack
            var preAttackAnimDuration = WaitForPreAttackAnimation ? AnimatedSpriteHelper.GetAnimationDuration(animator) : PreAttackDuration;
            var preAttackTimer = 0f;
            while ((!WaitForPreAttackAnimation && (preAttackTimer < PreAttackDuration)) || (WaitForPreAttackAnimation && AnimatedSpriteHelper.IsAnimationPlaying(animator, PreAttackAnimation)))
            {
                preAttackTimer += Time.DeltaTime;

                yield return null;
            }

            //stop pre attack movement if not null
            preAttackMovementCoroutine?.Stop();

            //get dir towards target
            var dirTowardsTarget = Vector2.Zero;
            if (targetingInfo.Direction != null)
                dirTowardsTarget = targetingInfo.Direction.Value;
            else if (targetingInfo.Position != null)
                dirTowardsTarget = targetingInfo.Position.Value - entity.Position;
            else if (targetingInfo.TargetEntity != null)
                dirTowardsTarget = targetingInfo.TargetEntity.Position - entity.Position;

            //normalize dir towards target
            if (dirTowardsTarget != Vector2.Zero)
                dirTowardsTarget.Normalize();

            //handle projectiles
            foreach (var attackProjectile in Projectiles)
            {
                if (Projectiles2.TryGetProjectile(attackProjectile.ProjectileName, out var projectileConfig))
                {
                    //create projectile entity
                    var projectileEntity = ProjectileEntity.CreateProjectileEntity(projectileConfig, dirTowardsTarget);

                    //determine if we should start from enemy or target pos
                    Vector2 pos = Vector2.Zero;
                    if (!projectileConfig.AttachToOwner)
                    {
                        //check if we should start from the target
                        if (attackProjectile.StartFromTarget && (targetingInfo.TargetEntity != null || targetingInfo.Position != null))
                            pos = targetingInfo.TargetEntity != null ? targetingInfo.TargetEntity.Position : targetingInfo.Position.Value;
                        else
                            pos = entity.Position;
                    }
                    else
                        projectileEntity.SetParent(entity);

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
                        Game1.Schedule(attackProjectile.Delay, timer => entity.Scene.AddEntity(projectileEntity));
                    else
                        entity.Scene.AddEntity(projectileEntity);
                }
            }

            //handle attack sound
            Game1.AudioManager.PlaySound(AttackSound);

            //if we have an attack animation, play it
            AnimatedSpriteHelper.PlayAnimation(ref animator, AttackAnimation);

            ICoroutine attackMovementCoroutine = null;
            if (AttackMovement != null)
                attackMovementCoroutine = Game1.StartCoroutine(AttackMovement.HandleMovement(entity, targetingInfo, AttackAnimation));

            //determine how long to wait for attack
            var attackAnimDuration = WaitForAttackAnimation ? AnimatedSpriteHelper.GetAnimationDuration(animator) : AttackDuration;
            var attackTimer = 0f;
            while ((!WaitForAttackAnimation && (attackTimer < AttackDuration)) || (WaitForAttackAnimation && AnimatedSpriteHelper.IsAnimationPlaying(animator, AttackAnimation)))
            {
                attackTimer += Time.DeltaTime;
                yield return null;
            }

            attackMovementCoroutine?.Stop();

            //if we have a post attack animation, play it
            AnimatedSpriteHelper.PlayAnimation(ref animator, PostAttackAnimation);

            ICoroutine postAttackMovementCoroutine = null;
            if (PostAttackMovement != null)
                postAttackMovementCoroutine = Game1.StartCoroutine(PostAttackMovement?.HandleMovement(entity, targetingInfo, PostAttackAnimation));

            //determine how long to wait for post attack
            var postAttackAnimDuration = WaitForPostAttackAnimation ? AnimatedSpriteHelper.GetAnimationDuration(animator) : PostAttackDuration;
            var postAttackTimer = 0f;
            while ((!WaitForPostAttackAnimation && (postAttackTimer < PostAttackDuration)) || (WaitForPostAttackAnimation && AnimatedSpriteHelper.IsAnimationPlaying(animator, PostAttackAnimation)))
            {
                postAttackTimer += Time.DeltaTime;
                yield return null;
            }

            postAttackMovementCoroutine?.Stop();
        }
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
