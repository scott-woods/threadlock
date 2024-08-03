using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using Nez.Textures;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Threadlock.Components.Hitboxes;
using Threadlock.Helpers;
using Threadlock.StaticData;

namespace Threadlock.Entities
{
    public class Projectile : Entity
    {
        //consts
        const float _maxTime = 10f;

        public IHitbox Hitbox { get => _hitbox as IHitbox; }

        //components
        SpriteAnimator _animator;
        ProjectileMover _mover;
        Collider _hitbox;
        SpriteTrail _trail;

        //misc
        Vector2 _direction;
        float _speed;
        bool _isBursting = false;
        float _timeSinceLaunched = 0f;

        public Projectile(Vector2 direction, ProjectileConfig config) : this(config.SpritePath, config.Speed, config.Radius, config.Damage, direction, config.DestroyOnWall, config.PhysicsLayer, config.HitLayers[0])
        {
        }

        public Projectile(string name, float speed, float radius, int damage, Vector2 direction, bool destroyOnWall, int physicsLayer, int collidesWithLayer)
        {
            //set values
            _direction = direction;
            _speed = speed;

            //create animator
            _animator = new SpriteAnimator();
            _animator.SetRenderLayer(RenderLayers.YSort);
            AnimatedSpriteHelper.ParseAnimationFile("Content/Textures/Projectiles", name, ref _animator);

            //create hitbox
            _hitbox = new CircleHitbox(damage, radius);

            //set hitbox physics layers
            _hitbox.PhysicsLayer = 0;
            Flags.SetFlag(ref _hitbox.PhysicsLayer, physicsLayer);
            _hitbox.CollidesWithLayers = 0;
            Flags.SetFlag(ref _hitbox.CollidesWithLayers, collidesWithLayer);
            if (destroyOnWall)
                Flags.SetFlag(ref _hitbox.CollidesWithLayers, PhysicsLayers.Environment);
        }

        #region LIFECYCLE

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            AddComponent(_animator);

            _mover = AddComponent(new ProjectileMover());

            AddComponent(_hitbox);

            _trail = AddComponent(new SpriteTrail(_animator));
            _trail.FadeDelay = 0;
            _trail.FadeDuration = .2f;
            _trail.MinDistanceBetweenInstances = 20f;
            _trail.InitialColor = Color.White * .5f;

            //start playing travel animation immediately
            _animator.Play("Travel");
        }

        public override void Update()
        {
            base.Update();

            if (_isBursting)
                return;

            if (_mover.Move(_direction * _speed * Time.DeltaTime))
            {
                Burst();
                return;
            }

            _timeSinceLaunched += Time.DeltaTime;

            //check for hit
            //var layersToCheck = 0;
            //foreach (var layer in _config.HitLayers)
            //    Flags.SetFlag(ref layersToCheck, layer);
            //var colliders = Physics.BoxcastBroadphaseExcludingSelf(_hitbox, layersToCheck);
            //if (colliders.Count > 0)
            //{
            //    Burst();
            //    foreach (var collider in colliders)
            //    {
            //        if (!_hitColliders.Contains(collider))
            //        {
            //            _hitColliders.Add(collider);
            //            OnHit?.Invoke(collider, _config.Damage);
            //        }
            //    }
            //}

            if (Physics.BoxcastBroadphaseExcludingSelf(_hitbox, 1 << PhysicsLayers.Environment).Any() || _timeSinceLaunched >= _maxTime)
                Burst();
        }

        #endregion

        public IEnumerator Fade(float fadeTime)
        {
            _hitbox.SetEnabled(false);
            _trail.SetFadeDelay(0f);
            _trail.SetFadeDuration(.01f);
            var tween = _animator.TweenColorTo(Color.Transparent, fadeTime);
            var trailTween = _trail.TweenColorTo(Color.Transparent, fadeTime);
            tween.Start();
            trailTween.Start();
            yield break;
            //var timer = 0f;
            //while (timer < fadeTime)
            //{
                
            //}
        }

        public void Reflect()
        {
            //reverse flags
            if (Flags.IsUnshiftedFlagSet(_hitbox.PhysicsLayer, PhysicsLayers.PlayerHitbox))
            {
                Flags.UnsetFlag(ref _hitbox.PhysicsLayer, PhysicsLayers.PlayerHitbox);
                Flags.SetFlag(ref _hitbox.PhysicsLayer, PhysicsLayers.EnemyHitbox);
            }
            else if (Flags.IsUnshiftedFlagSet(_hitbox.PhysicsLayer, PhysicsLayers.EnemyHitbox))
            {
                Flags.UnsetFlag(ref _hitbox.PhysicsLayer, PhysicsLayers.EnemyHitbox);
                Flags.SetFlag(ref _hitbox.PhysicsLayer, PhysicsLayers.PlayerHitbox);
            }

            if (Flags.IsUnshiftedFlagSet(_hitbox.CollidesWithLayers, PhysicsLayers.PlayerHurtbox))
            {
                Flags.UnsetFlag(ref _hitbox.CollidesWithLayers, PhysicsLayers.PlayerHurtbox);
                Flags.SetFlag(ref _hitbox.CollidesWithLayers, PhysicsLayers.EnemyHurtbox);
            }
            else if (Flags.IsUnshiftedFlagSet(_hitbox.CollidesWithLayers, PhysicsLayers.EnemyHurtbox))
            {
                Flags.UnsetFlag(ref _hitbox.CollidesWithLayers, PhysicsLayers.EnemyHurtbox);
                Flags.SetFlag(ref _hitbox.CollidesWithLayers, PhysicsLayers.PlayerHurtbox);
            }

            //reverse direction
            _direction = _direction * -1;
        }

        void Burst()
        {
            Game1.Schedule(.1f, timer =>
            {
                _hitbox.SetEnabled(false);
            });

            _isBursting = true;
            var validAnims = _animator.Animations.Where(a => a.Key.StartsWith("Burst")).ToList();
            if (validAnims != null && validAnims.Count > 0)
            {
                _animator.Play(validAnims.RandomItem().Key, SpriteAnimator.LoopMode.Once);
                _animator.OnAnimationCompletedEvent += OnAnimationCompleted;
            }
            else
                Destroy();
        }

        void OnAnimationCompleted(string animationName)
        {
            _animator.OnAnimationCompletedEvent -= OnAnimationCompleted;
            _animator.SetEnabled(false);
            _hitbox.SetEnabled(false);
            Destroy();
        }
    }

    public class ProjectileConfig
    {
        public float Speed { get; set; }
        public float Radius { get; set; }
        public string SpritePath { get; set; }
        public Sprite[] TravelSprites { get; set; }
        public Sprite[] BurstSprites { get; set; }
        public int Damage { get; set; }
        public int PhysicsLayer { get; set; }
        public List<int> HitLayers { get; set; }
        public bool DestroyOnWall { get; set; }
    }
}
