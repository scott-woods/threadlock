using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using Nez.Tweens;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Entities;
using Threadlock.Entities.Characters.Enemies;
using Threadlock.Helpers;
using Threadlock.StaticData;
using Random = Nez.Random;

namespace Threadlock.Components.EnemyActions
{
    public class EnemyAction3
    {
        //Requirements
        public bool RequiresLoS;
        public float MinDistance;
        public float MaxDistance = float.MaxValue;
        public float MinDistanceX;
        public float MaxDistanceX = float.MaxValue;
        public float MinDistanceY;
        public float MaxDistanceY = float.MaxValue;

        public string Name;
        public int Priority;
        public bool IsCombo;
        public float Cooldown;

        public string PreAttackAnimation;
        public float PreAttackDuration;
        public bool WaitForPreAttackAnimation;
        public MovementConfig PreAttackMovement;

        public string AttackAnimation;
        public float AttackDuration;
        public bool WaitForAttackAnimation;
        public MovementConfig AttackMovement;

        public string PostAttackAnimation;
        public float PostAttackDuration;
        public bool WaitForPostAttackAnimation;
        public MovementConfig PostAttackMovement;

        public List<string> ComboActions = new List<string>();

        public List<AttackProjectile> Projectiles = new List<AttackProjectile>();

        bool _isActive;
        public bool IsActive { get => _isActive; }

        bool _isOnCooldown;

        public void LoadAnimations(ref SpriteAnimator animator)
        {
            AnimatedSpriteHelper.LoadAnimations(ref animator, PreAttackAnimation, AttackAnimation, PostAttackAnimation);
        }

        public IEnumerator BeginExecution(Enemy enemy)
        {
            _isActive = true;

            if (IsCombo)
            {
                foreach (var actionString in ComboActions)
                {
                    if (AllEnemyActions.TryGetAction(actionString, out var action))
                    {
                        yield return Game1.StartCoroutine(action.BeginExecution(enemy));
                    }
                }
            }
            else
                yield return Game1.StartCoroutine(Execute(enemy));

            if (Cooldown > 0)
            {
                _isOnCooldown = true;
                Game1.Schedule(Cooldown, timer => _isOnCooldown = false);
            }

            _isActive = false;
        }

        /// <summary>
        /// determine if the enemy is able to execute based on the requirements
        /// </summary>
        /// <param name="enemy"></param>
        /// <returns></returns>
        public bool CanExecute(Enemy enemy)
        {
            if (_isOnCooldown)
                return false;

            if (RequiresLoS && !EntityHelper.HasLineOfSight(enemy, enemy.TargetEntity))
                return false;

            var dist = EntityHelper.DistanceToEntity(enemy, enemy.TargetEntity);
            if (dist < MinDistance || dist > MaxDistance)
                return false;

            var distX = Math.Abs(enemy.TargetEntity.Position.X - enemy.Position.X);
            if (distX < MinDistanceX || distX > MaxDistanceX)
                return false;

            var distY = Math.Abs(enemy.TargetEntity.Position.Y - enemy.Position.Y);
            if (distY < MinDistanceY || distY > MaxDistanceY)
                return false;

            return true;
        }

        public void Abort(Enemy enemy)
        {

        }

        IEnumerator Execute(Enemy enemy)
        {
            //get necessary components from enemy
            var animator = enemy.GetComponent<SpriteAnimator>();
            var velocityComponent = enemy.GetComponent<VelocityComponent>();

            //if we have a pre attack animation, play it
            AnimatedSpriteHelper.PlayAnimation(ref animator, PreAttackAnimation);

            //determine how long to wait for pre attack
            var preAttackAnimDuration = WaitForPreAttackAnimation ? AnimatedSpriteHelper.GetAnimationDuration(animator) : PreAttackDuration;
            var preAttackTimer = 0f;
            while ((!WaitForPreAttackAnimation && (preAttackTimer < PreAttackDuration)) || (WaitForPreAttackAnimation && (animator.CurrentAnimationName == PreAttackAnimation && animator.AnimationState != SpriteAnimator.State.Completed)))
            {
                preAttackTimer += Time.DeltaTime;

                if (PreAttackMovement != null)
                {
                    HandleMovement(enemy, velocityComponent, PreAttackMovement, preAttackTimer, preAttackAnimDuration);
                }

                yield return null;
            }

            var dirTowardsTarget = EntityHelper.DirectionToEntity(enemy, enemy.TargetEntity);

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
                        pos = attackProjectile.StartFromTarget ? enemy.TargetEntity.Position : enemy.Position;
                    else
                        projectileEntity.SetParent(enemy);

                    //add entity offset
                    pos += attackProjectile.EntityOffset;

                    //add offset to starting pos
                    pos += (dirTowardsTarget * attackProjectile.OffsetDistance);

                    //add more offset if we should predict the target
                    if (attackProjectile.PredictTarget)
                    {
                        if (enemy.TargetEntity.TryGetComponent<VelocityComponent>(out var vc))
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
                        Game1.Schedule(attackProjectile.Delay, timer => enemy.Scene.AddEntity(projectileEntity));
                    else
                        enemy.Scene.AddEntity(projectileEntity);
                }
            }

            //if we have an attack animation, play it
            AnimatedSpriteHelper.PlayAnimation(ref animator, AttackAnimation);

            //determine how long to wait for attack
            var attackAnimDuration = WaitForAttackAnimation ? AnimatedSpriteHelper.GetAnimationDuration(animator) : AttackDuration;
            var attackTimer = 0f;
            while ((!WaitForAttackAnimation && (attackTimer < AttackDuration)) || (WaitForAttackAnimation && (animator.CurrentAnimationName == AttackAnimation && animator.AnimationState != SpriteAnimator.State.Completed)))
            {
                attackTimer += Time.DeltaTime;

                if (AttackMovement != null)
                {
                    HandleMovement(enemy, velocityComponent, AttackMovement, attackTimer, attackAnimDuration);
                }

                yield return null;
            }

            //if we have a post attack animation, play it
            AnimatedSpriteHelper.PlayAnimation(ref animator, PostAttackAnimation);

            //determine how long to wait for post attack
            var postAttackAnimDuration = WaitForPostAttackAnimation ? AnimatedSpriteHelper.GetAnimationDuration(animator) : PostAttackDuration;
            var postAttackTimer = 0f;
            while ((!WaitForPostAttackAnimation && (postAttackTimer < PostAttackDuration)) || (WaitForPostAttackAnimation && (animator.CurrentAnimationName == PostAttackAnimation && animator.AnimationState != SpriteAnimator.State.Completed)))
            {
                postAttackTimer += Time.DeltaTime;

                if (PostAttackMovement != null)
                {
                    HandleMovement(enemy, velocityComponent, PostAttackMovement, postAttackTimer, postAttackAnimDuration);
                }

                yield return null;
            }
        }

        void HandleMovement(Enemy enemy, VelocityComponent velocityComponent, MovementConfig movementConfig, float time, float totalTime)
        {
            var dir = movementConfig.Direction switch
            {
                MovementDirection.ToTarget => EntityHelper.DirectionToEntity(enemy, enemy.TargetEntity),
                MovementDirection.AwayFromTarget => EntityHelper.DirectionToEntity(enemy.TargetEntity, enemy),
                _ => throw new ArgumentOutOfRangeException(nameof(movementConfig.Direction))
            };

            var speed = movementConfig.Speed;
            if (movementConfig.FinalSpeed != null)
            {
                speed = Lerps.Ease(movementConfig.EaseType, speed, movementConfig.FinalSpeed.Value, time, movementConfig.UseAnimationDuration ? totalTime : movementConfig.Duration);
            }

            velocityComponent.Move(dir, speed);
        }
    }
}
