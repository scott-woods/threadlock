using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using Nez.Tweens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Entities.Characters.Player.BasicWeapons;

namespace Threadlock.Entities.Characters.Player
{
    public class Dash : Component, IUpdatable
    {
        const float _initialDashSpeed = 425f;
        const float _shortDashCooldown = .025f;
        const float _dashCooldown = .65f;
        const float _successionLifespan = .65f;

        public bool IsOnCooldown = false;

        Action _dashCompleteCallback;
        VelocityComponent _velocityComponent;
        SpriteAnimator _spriteAnimator;
        SpriteTrail _spriteTrail;

        int _maxSuccession;
        bool _isDashing = false;
        float _dashTimer = 0f;
        int _successionCount = 0;

        float _elapsedTime = 0f;

        public Dash(int maxSuccession)
        {
            _maxSuccession = maxSuccession;
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            _velocityComponent = Entity.GetComponent<VelocityComponent>();
            _spriteAnimator = Entity.GetComponent<SpriteAnimator>();
            _spriteTrail = Entity.GetComponent<SpriteTrail>();
        }

        public void ExecuteDash(Action dashCompleteCallback)
        {
            _dashCompleteCallback = dashCompleteCallback;

            _isDashing = true;
            _successionCount += 1;
            Core.Schedule(_successionLifespan, timer => _successionCount -= 1);

            //configure trail
            _spriteTrail.FadeDelay = 0;
            _spriteTrail.FadeDuration = .2f;
            _spriteTrail.MinDistanceBetweenInstances = 20f;
            _spriteTrail.InitialColor = Color.White * .5f;
            _spriteTrail.EnableSpriteTrail();

            //play sound
            Game1.AudioManager.PlaySound(Content.Audio.Sounds.Player_dash);

            //animation
            var animation = "Roll";
            if (_velocityComponent.Direction.X != 0)
            {
                animation = "Roll";
            }
            else if (_velocityComponent.Direction.Y != 0)
            {
                animation = _velocityComponent.Direction.Y >= 0 ? "RollDown" : "RollUp";
            }

            if (!Entity.TryGetComponent<Sword>(out var swordAttack))
                animation += "NoSword";

            _elapsedTime = 0;

            _spriteAnimator.Color = Color.White * .8f;
            _spriteAnimator.Play(animation, SpriteAnimator.LoopMode.Once);
            _spriteAnimator.OnAnimationCompletedEvent += OnAnimationFinished;
        }

        public void Update()
        {
            if (_isDashing)
            {
                //increment time
                _elapsedTime += Time.DeltaTime;

                //get animation duration
                //var animationDuration = _animator.CurrentAnimation.Sprites.Count() / _animator.CurrentAnimation.FrameRate;
                var animationDuration = 8 / _spriteAnimator.CurrentAnimation.FrameRates[0];

                //if elapsed time is less than duration, we are still attacking
                if (_elapsedTime < animationDuration)
                {
                    //get lerp factor
                    float lerpFactor = _elapsedTime / animationDuration;

                    //determine move speed based on which combo we are on
                    var initialSpeed = _initialDashSpeed;

                    float currentSpeed = Lerps.Lerp(initialSpeed, 0, lerpFactor);
                    _velocityComponent.Move(_velocityComponent.Direction, currentSpeed);
                }
            }
        }

        public void Abort()
        {
            _spriteAnimator.Color = Color.White;
            _spriteTrail.DisableSpriteTrail();
            _isDashing = false;
            _spriteAnimator.OnAnimationCompletedEvent -= OnAnimationFinished;
            _spriteAnimator.Stop();
        }

        void OnAnimationFinished(string animationName)
        {
            _spriteAnimator.OnAnimationCompletedEvent -= OnAnimationFinished;

            var cooldown = _successionCount >= _maxSuccession ? _dashCooldown : _shortDashCooldown;

            IsOnCooldown = true;
            Core.Schedule(cooldown, timer => IsOnCooldown = false);

            _spriteAnimator.Color = Color.White;
            _spriteTrail.DisableSpriteTrail();
            _isDashing = false;
            _elapsedTime = 0;
            _dashCompleteCallback?.Invoke();
        }
    }
}
