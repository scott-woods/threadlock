using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components.EnemyActions;
using Threadlock.Components.Hitboxes;
using Threadlock.Helpers;
using Threadlock.StaticData;

namespace Threadlock.Entities
{
    public abstract class ProjectileEntity : Entity
    {
        protected ProjectileConfig2 Config;
        protected Vector2 Direction;

        protected Collider Hitbox;
        protected SpriteAnimator Animator;

        protected bool IsBursting;

        public ProjectileEntity(ProjectileConfig2 config, Vector2 initialDirection)
        {
            Config = config;
            Direction = initialDirection;
        }

        public static ProjectileEntity CreateProjectileEntity(ProjectileConfig2 config, Vector2 initialDirection)
        {
            switch (config)
            {
                case InstantProjectileConfig instantConfig:
                    return new InstantProjectileEntity(instantConfig, initialDirection);
                case StraightProjectileConfig straightConfig:
                    return new StraightProjectileEntity(straightConfig, initialDirection);
                default:
                    throw new ArgumentException("Unknown projectile type.");
            }
        }

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            //create animator
            Animator = AddComponent(new SpriteAnimator());
            var animationOffset = Config.AnimationOffset;
            if (Config.ShouldRotate)
            {
                if (Direction.X < 0)
                {
                    animationOffset.Y *= -1;
                    Animator.FlipY = true;
                }

                animationOffset = Mathf.RotateAround(animationOffset, Vector2.Zero, MathHelper.ToDegrees(DirectionHelper.GetClampedAngle(Direction, Config.MaxRotation)));
            }
            Animator.SetLocalOffset(animationOffset);
            Animator.OnAnimationCompletedEvent += OnAnimationCompleted;

            //load animations
            AnimatedSpriteHelper.LoadAnimations(ref Animator, Config.DestroyAnimations.Concat(new List<string> { Config.LaunchAnimation, Config.Animation }).ToArray());

            //disable animator if there are no animations to show
            if (Animator.Animations.Count == 0)
                Animator.SetEnabled(false);

            //create hitbox
            if (Config.Radius > 0)
                Hitbox = AddComponent(new CircleHitbox(Config.Damage, Config.Radius));
            else if (Config.Size != Vector2.Zero)
                Hitbox = AddComponent(new BoxHitbox(Config.Damage, Config.Size.X, Config.Size.Y));
            else if (Config.Points != null && Config.Points.Count() > 0)
            {
                var points = Config.Points.ToArray();
                if (Config.ShouldRotate && Direction.X != 0)
                    points = Config.Points.Select(p => new Vector2(p.X, p.Y * Math.Sign(Direction.X))).ToArray();
                Hitbox = AddComponent(new PolygonHitbox(Config.Damage, points));
            }

            //handle hitbox physics layers
            Hitbox.PhysicsLayer = 0;
            Hitbox.CollidesWithLayers = 0;
            if (Config.AffectsPlayer)
            {
                Flags.SetFlag(ref Hitbox.PhysicsLayer, PhysicsLayers.EnemyHitbox);
                Flags.SetFlag(ref Hitbox.CollidesWithLayers, PhysicsLayers.PlayerHurtbox);
            }
            if (Config.AffectsEnemies)
            {
                Flags.SetFlag(ref Hitbox.PhysicsLayer, PhysicsLayers.PlayerHitbox);
                Flags.SetFlag(ref Hitbox.CollidesWithLayers, PhysicsLayers.EnemyHurtbox);
            }
            if (Config.DestroyOnWalls)
            {
                Flags.SetFlag(ref Hitbox.CollidesWithLayers, PhysicsLayers.Environment);
            }

            //hitbox should start disabled
            Hitbox.SetEnabled(false);

            Game1.StartCoroutine(Fire());
        }

        protected virtual IEnumerator Fire()
        {
            //schedule end after lifespan
            if (Config.Lifespan > 0)
                Game1.Schedule(Config.Lifespan, timer => End());

            //play launch animation if we have one
            AnimatedSpriteHelper.PlayAnimation(ref Animator, Config.LaunchAnimation);
            yield return Coroutine.WaitForSeconds(Config.LaunchDuration);

            //enable hitbox
            Hitbox.SetEnabled(true);

            //set hitbox to disable if necessary
            if (Config.HitboxActiveDuration > 0)
                Game1.Schedule(Config.HitboxActiveDuration, timer => Hitbox.SetEnabled(false));

            //play main animation if we have one
            yield return AnimatedSpriteHelper.WaitForAnimation(Animator, Config.Animation);

            //destroy after animation if necessary
            if (Config.DestroyAfterAnimation)
                End();
        }

        protected virtual void End()
        {
            IsBursting = true;
            Hitbox.SetEnabled(false);

            if (Config.DestroyAnimations.Any())
                AnimatedSpriteHelper.PlayAnimation(ref Animator, Config.DestroyAnimations.RandomItem());
            else
                Destroy();
        }

        void OnAnimationCompleted(string animationName)
        {
            if (Config.DestroyAnimations != null && Config.DestroyAnimations.Contains(animationName))
                Destroy();
        }
    }

    public class InstantProjectileEntity : ProjectileEntity
    {
        public InstantProjectileEntity(InstantProjectileConfig config, Vector2 initialDirection) : base(config, initialDirection)
        { }
    }

    public class StraightProjectileEntity : ProjectileEntity
    {
        ProjectileMover _mover;

        float _speed;

        public StraightProjectileEntity(StraightProjectileConfig config, Vector2 initialDirection) : base(config, initialDirection)
        {
            _speed = config.Speed;
        }

        public override void OnAddedToScene()
        {
            _mover = AddComponent(new ProjectileMover());

            base.OnAddedToScene();
        }

        public override void Update()
        {
            base.Update();

            if (IsBursting)
                return;

            if (_mover.Move(Direction * _speed * Time.DeltaTime))
                End();
        }
    }
}
