using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using Nez.Systems;
using Nez.Textures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components.Hitboxes;
using Threadlock.Entities.Characters.Player.BasicWeapons;
using Threadlock.Helpers;
using Threadlock.StaticData;

namespace Threadlock.Entities
{
    public class Projectile : Entity
    {
        //consts
        const float _maxTime = 10f;

        public event Action<Collider, int> OnHit;

        //components
        SpriteAnimator _animator;
        ProjectileMover _mover;
        CircleHitbox _hitbox;
        SpriteTrail _trail;

        //misc
        Vector2 _direction;
        ProjectileConfig _config;
        bool _isBursting = false;
        float _timeSinceLaunched = 0f;
        List<Collider> _hitColliders = new List<Collider>();

        public Projectile(Vector2 direction, ProjectileConfig config)
        {
            _direction = direction;
            _config = config;
        }

        #region LIFECYCLE

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            _animator = AddComponent(new SpriteAnimator());
            _animator.SetRenderLayer(RenderLayers.YSort);

            _mover = AddComponent(new ProjectileMover());

            _hitbox = AddComponent(new CircleHitbox(_config.Damage, _config.Radius));
            Flags.SetFlagExclusive(ref _hitbox.PhysicsLayer, _config.PhysicsLayer);
            _hitbox.CollidesWithLayers = 0;
            if (_config.DestroyOnWall)
                Flags.SetFlag(ref _hitbox.CollidesWithLayers, PhysicsLayers.Environment);
            foreach (var layer in _config.HitLayers)
                Flags.SetFlag(ref _hitbox.CollidesWithLayers, layer);
            _hitbox.PushForce = 1f;

            _trail = AddComponent(new SpriteTrail(_animator));
            _trail.FadeDelay = 0;
            _trail.FadeDuration = .2f;
            _trail.MinDistanceBetweenInstances = 20f;
            _trail.InitialColor = Color.White * .5f;

            _animator.AddAnimation("Travel", _config.TravelSprites);
            _animator.AddAnimation("Burst", _config.BurstSprites);

            //start playing travel animation immediately
            _animator.Play("Travel");
        }

        public override void Update()
        {
            base.Update();

            if (_isBursting)
                return;

            _mover.Move(_direction * _config.Speed * Time.DeltaTime);
            _timeSinceLaunched += Time.DeltaTime;

            //check for hit
            var layersToCheck = 0;
            foreach (var layer in _config.HitLayers)
                Flags.SetFlag(ref layersToCheck, layer);
            var colliders = Physics.BoxcastBroadphaseExcludingSelf(_hitbox, layersToCheck);
            if (colliders.Count > 0)
            {
                Burst();
                foreach (var collider in colliders)
                {
                    if (!_hitColliders.Contains(collider))
                    {
                        _hitColliders.Add(collider);
                        OnHit?.Invoke(collider, _config.Damage);
                    }
                }
            }

            if (Physics.BoxcastBroadphaseExcludingSelf(_hitbox, 1 << PhysicsLayers.Environment).Any() || _timeSinceLaunched >= _maxTime)
                Burst();

            ////if already bursting, do nothing
            //if (_isBursting)
            //    return;

            ////increment timer
            //_timeSinceLaunched += Time.DeltaTime;

            ////try to move. if hitting something, or reached max time, burst
            //if (_mover.Move(_direction * _config.Speed * Time.DeltaTime) || _timeSinceLaunched >= _maxTime)
            //{
            //    Burst();
            //}
        }

        #endregion

        void Burst()
        {
            Game1.Schedule(.1f, timer =>
            {
                _hitbox.SetEnabled(false);
            });

            _isBursting = true;
            _animator.Play("Burst", SpriteAnimator.LoopMode.Once);
            _animator.OnAnimationCompletedEvent += OnAnimationCompleted;
        }

        void OnAnimationCompleted(string animationName)
        {
            _animator.OnAnimationCompletedEvent -= OnAnimationCompleted;
            _animator.SetEnabled(false);
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
