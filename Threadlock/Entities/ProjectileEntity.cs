using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using Nez.Tweens;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Threadlock.Components;
using Threadlock.Components.Hitboxes;
using Threadlock.Entities.Characters.Player;
using Threadlock.Helpers;
using Threadlock.StaticData;

namespace Threadlock.Entities
{
    public abstract class ProjectileEntity : Entity
    {
        protected ProjectileConfig2 Config;
        protected Entity Owner;

        //components
        protected Collider Hitbox;
        protected SpriteAnimator Animator;
        protected CollisionWatcher CollisionWatcher;

        protected bool IsBursting;
        protected Vector2 Direction;

        public ProjectileEntity(ProjectileConfig2 config, Vector2 initialDirection, Entity owner)
        {
            Config = config;
            Direction = initialDirection;
            Owner = owner;
        }

        public static ProjectileEntity CreateProjectileEntity(ProjectileConfig2 config, Vector2 initialDirection, Entity owner = null)
        {
            switch (config)
            {
                case InstantProjectileConfig instantConfig:
                    return new InstantProjectileEntity(instantConfig, initialDirection, owner);
                case StraightProjectileConfig straightConfig:
                    return new StraightProjectileEntity(straightConfig, initialDirection, owner);
                case ExplosionProjectileConfig explosionConfig:
                    return new ExplosionProjectileEntity(explosionConfig, initialDirection, owner);
                default:
                    throw new ArgumentException("Unknown projectile type.");
            }
        }

        #region LIFECYCLE

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
            if (Config.Radius.HasValue)
                Hitbox = AddComponent(new CircleHitbox(Config.Damage, Config.Radius.Value));
            else if (Config.Size != Vector2.Zero)
                Hitbox = AddComponent(new BoxHitbox(Config.Damage, Config.Size.X, Config.Size.Y));
            else if (Config.Points != null && Config.Points.Count() > 0)
            {
                var points = Config.Points.ToArray();
                if (Config.ShouldRotate && Direction.X != 0)
                    points = Config.Points.Select(p => new Vector2(p.X, p.Y * Math.Sign(Direction.X))).ToArray();
                Hitbox = AddComponent(new PolygonHitbox(Config.Damage, points));
            }

            if (Hitbox is IHitbox hitbox)
                hitbox.Direction = Direction;

            //handle hitbox physics layers
            Hitbox.PhysicsLayer = 0;
            Hitbox.CollidesWithLayers = 0;
            foreach (var layer in Config.PhysicsLayers)
                Flags.SetFlag(ref Hitbox.PhysicsLayer, PhysicsLayers.GetLayerByName(layer));
            if (Config.AffectsPlayer)
                Flags.SetFlag(ref Hitbox.CollidesWithLayers, PhysicsLayers.PlayerHurtbox);
            if (Config.AffectsEnemies)
                Flags.SetFlag(ref Hitbox.CollidesWithLayers, PhysicsLayers.EnemyHurtbox);
            if (Config.DestroyOnWalls)
                Flags.SetFlag(ref Hitbox.CollidesWithLayers, PhysicsLayers.Environment);

            CollisionWatcher = AddComponent(new CollisionWatcher());
            CollisionWatcher.OnTriggerEntered += OnCollision;

            foreach (var effect in Config.HitEffects)
            {
                if (effect.Layers != null && effect.Layers.Count > 0)
                {
                    foreach (var layer in effect.Layers)
                        Flags.SetFlag(ref Hitbox.CollidesWithLayers, PhysicsLayers.GetLayerByName(layer));
                }
            }

            //hitbox should start disabled
            Hitbox.SetEnabled(false);

            Core.StartCoroutine(Fire());
        }

        #endregion

        protected virtual IEnumerator Fire()
        {
            //schedule end after lifespan
            if (Config.Lifespan > 0)
                Core.Schedule(Config.Lifespan, timer => End());

            //play launch animation if we have one
            AnimatedSpriteHelper.PlayAnimation(ref Animator, Config.LaunchAnimation);
            if (Config.LaunchDuration > 0)
                yield return Coroutine.WaitForSeconds(Config.LaunchDuration);

            //enable hitbox
            Hitbox.SetEnabled(true);

            //set hitbox to disable if necessary
            if (Config.HitboxActiveDuration > 0)
                Core.Schedule(Config.HitboxActiveDuration, timer => Hitbox.SetEnabled(false));

            //play main animation if we have one
            AnimatedSpriteHelper.PlayAnimation(ref Animator, Config.Animation);
        }

        /// <summary>
        /// plays destroy animation if any and destroys entity
        /// </summary>
        public virtual void End()
        {
            IsBursting = true;
            Hitbox.SetEnabled(false);

            if (Config.DestroyAnimations.Any())
                AnimatedSpriteHelper.PlayAnimation(ref Animator, Config.DestroyAnimations.RandomItem());
            else
                Destroy();
        }

        /// <summary>
        /// called by a hurtbox when it takes damage from this projectile
        /// </summary>
        /// <param name="hitCollider"></param>
        /// <param name="collisionResult"></param>
        /// <param name="damage"></param>
        public void OnHit(Collider hitCollider, CollisionResult collisionResult, int damage)
        {
            foreach (var effect in Config.HitEffects)
            {
                if (effect.RequiresDamage)
                    effect.Apply(this, hitCollider);
            }

            if (Config.HitVfx.Count > 0)
            {
                var hitVfx = Config.HitVfx.RandomItem();
                var hitVfxPos = collisionResult.Point != Vector2.Zero ? collisionResult.Point : hitCollider.Entity.Position;
                var hitVfxEntity = Scene.AddEntity(new HitVfx(hitVfx));
                hitVfxEntity.SetPosition(hitVfxPos);
            }

            if (Owner != null && Owner.TryGetComponent<ApComponent>(out var apComponent))
                apComponent.OnAttackHit(damage);

            if (Config.DestroyOnHit)
                End();
        }

        #region OBSERVERS

        void OnAnimationCompleted(string animationName)
        {
            if (Config.DestroyAnimations != null && Config.DestroyAnimations.Contains(animationName))
                Destroy();
            else if (Config.Animation == animationName && Config.DestroyAfterAnimation)
                Destroy();
        }

        void OnCollision(Collider other, Collider local)
        {
            if (local != Hitbox)
                return;

            if (Config.DestroyOnWalls && (1 << PhysicsLayers.Environment).IsFlagSet(other.PhysicsLayer))
            {
                End();
            }
            else
            {
                foreach (var effect in Config.HitEffects)
                {
                    if (effect.IsColliderValid(other))
                        effect.Apply(this, other);
                }
            }
        }

        #endregion
    }

    public class InstantProjectileEntity : ProjectileEntity
    {
        ColliderTriggerHelper _triggerHelper;

        public InstantProjectileEntity(InstantProjectileConfig config, Vector2 initialDirection, Entity owner) : base(config, initialDirection, owner)
        {
            _triggerHelper = new ColliderTriggerHelper(this);
        }

        public override void Update()
        {
            base.Update();

            _triggerHelper?.Update();
        }
    }

    public class StraightProjectileEntity : ProjectileEntity
    {
        ProjectileMover _projectileMover;

        float _speed;
        StraightProjectileConfig _config;

        public StraightProjectileEntity(StraightProjectileConfig config, Vector2 initialDirection, Entity owner) : base(config, initialDirection, owner)
        {
            _config = config;
        }

        public override void OnAddedToScene()
        {
            _projectileMover = AddComponent(new ProjectileMover());

            base.OnAddedToScene();
        }

        protected override IEnumerator Fire()
        {
            yield return base.Fire();

            if (_config.InitialSpeed.HasValue)
            {
                _speed = _config.InitialSpeed.Value;

                if (_config.TimeToFinalSpeed.HasValue)
                {
                    var easeType = _config.EaseType.HasValue ? _config.EaseType.Value : EaseType.Linear;
                    var timer = 0f;
                    while (timer < _config.TimeToFinalSpeed.Value)
                    {
                        timer += Time.DeltaTime;
                        _speed = Lerps.Ease(easeType, _config.InitialSpeed.Value, _config.Speed, timer, _config.TimeToFinalSpeed.Value);
                        yield return null;
                    }
                }
            }
            else
                _speed = _config.Speed;
        }

        public override void Update()
        {
            base.Update();

            if (!IsBursting)
                _projectileMover.Move(Direction * _speed * Time.DeltaTime);
            //    return;

            //if (_mover.Move(Direction * _speed * Time.DeltaTime))
            //    End();
        }
    }

    public class ExplosionProjectileEntity : ProjectileEntity
    {
        ExplosionProjectileConfig _config;
        ColliderTriggerHelper _triggerHelper;

        public ExplosionProjectileEntity(ExplosionProjectileConfig config, Vector2 initialDirection, Entity owner) : base(config, initialDirection, owner)
        {
            _config = config;
        }

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            _triggerHelper = new ColliderTriggerHelper(this);
        }

        public override void Update()
        {
            base.Update();

            _triggerHelper.Update();
        }

        protected override IEnumerator Fire()
        {
            yield return base.Fire();

            var hitbox = Hitbox as CircleHitbox;
            hitbox.SetRadius(_config.InitialRadius);

            var timer = 0f;
            while (timer <= _config.ExplosionTime)
            {
                timer += Time.DeltaTime;
                var radius = Lerps.Ease(_config.EaseType, _config.InitialRadius, _config.FinalRadius, timer, _config.ExplosionTime);
                hitbox.SetRadius(radius);

                yield return null;
            }
        }
    }
}
