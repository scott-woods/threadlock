using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using Nez.Textures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components.Hitboxes;
using Threadlock.Helpers;
using Threadlock.StaticData;

namespace Threadlock.Components.EnemyActions.Spitter
{
    public class SpitAttackProjectile : Entity
    {
        //consts
        const int _damage = 1;
        const float _speed = 210f;
        const float _radius = 3f;
        const float _maxTime = 10f;

        //components
        SpriteAnimator _animator;
        ProjectileMover _mover;
        CircleHitbox _hitbox;
        SpriteTrail _trail;

        //misc
        Vector2 _direction;
        bool _isBursting = false;
        float _timeSinceLaunched = 0f;

        public SpitAttackProjectile(Vector2 direction)
        {
            _direction = direction;
        }

        #region LIFECYCLE

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            _animator = AddComponent(new SpriteAnimator());

            _mover = AddComponent(new ProjectileMover());

            _hitbox = AddComponent(new CircleHitbox(_damage, _radius));
            Flags.SetFlagExclusive(ref _hitbox.PhysicsLayer, PhysicsLayers.EnemyHitbox);
            _hitbox.CollidesWithLayers = 0;
            Flags.SetFlag(ref _hitbox.CollidesWithLayers, PhysicsLayers.PlayerHurtbox);
            Flags.SetFlag(ref _hitbox.CollidesWithLayers, PhysicsLayers.Environment);
            _hitbox.PushForce = 1f;

            _trail = AddComponent(new SpriteTrail(_animator));
            _trail.FadeDelay = 0;
            _trail.FadeDuration = .2f;
            _trail.MinDistanceBetweenInstances = 20f;
            _trail.InitialColor = Color.White * .5f;

            var texture = Scene.Content.LoadTexture(Content.Textures.Characters.Spitter.Spitter_projectile);
            var sprites = Sprite.SpritesFromAtlas(texture, 16, 16);
            _animator.AddAnimation("Travel", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 0, 4, 7));
            _animator.AddAnimation("Burst_1", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 1, 7, 7));
            _animator.AddAnimation("Burst_2", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 2, 6, 7));

            //start playing travel animation immediately
            _animator.Play("Travel");
        }

        public override void Update()
        {
            base.Update();

            //if already bursting, do nothing
            if (_isBursting)
                return;

            //increment timer
            _timeSinceLaunched += Time.DeltaTime;

            //try to move. if hitting something, or reached max time, burst
            if (_mover.Move(_direction * _speed * Time.DeltaTime) || _timeSinceLaunched >= _maxTime)
            {
                Burst();
                return;
            }
        }

        #endregion

        void Burst()
        {
            _isBursting = true;
            _animator.Play("Burst_1", SpriteAnimator.LoopMode.Once);
            _animator.OnAnimationCompletedEvent += OnAnimationCompleted;
        }

        void OnAnimationCompleted(string animationName)
        {
            _animator.OnAnimationCompletedEvent -= OnAnimationCompleted;
            _animator.SetEnabled(false);
            Destroy();
        }
    }
}
