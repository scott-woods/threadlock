using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using Nez.Tweens;
using System;
using System.Collections;
using Threadlock.Components;
using Threadlock.Entities.Characters.Player.BasicWeapons;
using Threadlock.Helpers;

namespace Threadlock.Entities.Characters.Player
{
    public class Dash : Component
    {
        const float _initialDashSpeed = 425f;
        const float _shortDashCooldown = .025f;
        const float _immunityBeginProgress = .2f;
        const float _immunityEndProgress = .8f;

        public bool IsOnCooldown = false;

        VelocityComponent _velocityComponent;
        SpriteAnimator _spriteAnimator;
        SpriteTrail _spriteTrail;
        Hurtbox _hurtbox;

        int _maxSuccession;

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
            if (Entity.TryGetComponent<Hurtbox>(out var hurtbox))
                _hurtbox = hurtbox;
        }

        public IEnumerator StartDash()
        {
            //sprite trail
            _spriteTrail.FadeDelay = 0;
            _spriteTrail.FadeDuration = .2f;
            _spriteTrail.MinDistanceBetweenInstances = 20f;
            _spriteTrail.InitialColor = Color.White * .5f;
            _spriteTrail.EnableSpriteTrail();

            //play sound
            Game1.AudioManager.PlaySound(Content.Audio.Sounds.Player_dash);
            var animationName = $"Roll{DirectionHelper.GetDirectionStringByVector(_velocityComponent.Direction)}";
            if (!Entity.TryGetComponent<Sword>(out var swordAttack))
                animationName += "NoSword";

            //start animation
            _spriteAnimator.Color = Color.White * .8f;
            _spriteAnimator.Play(animationName, SpriteAnimator.LoopMode.Once);

            //move and handle hurtbox while animation is playing
            var animDuration = AnimatedSpriteHelper.GetAnimationDuration(_spriteAnimator);
            var timer = 0f;
            while (_spriteAnimator.CurrentAnimationName == animationName && _spriteAnimator.AnimationState != SpriteAnimator.State.Completed)
            {
                //handle timer
                timer += Time.DeltaTime;
                var progress = timer / animDuration;

                //handle hurtbox
                if (progress >= _immunityBeginProgress && progress <= _immunityEndProgress)
                    _hurtbox?.SetEnabled(false);
                else
                    _hurtbox?.SetEnabled(true);

                //get current speed
                var currentSpeed = Lerps.Lerp(_initialDashSpeed, 0, progress);
                if (timer >= animDuration)
                    currentSpeed = 0;

                //move
                _velocityComponent.Move(_velocityComponent.Direction, currentSpeed);

                //wait until next frame
                yield return null;
            }

            //should already be enabled, but make sure hurtbox is enabled
            _hurtbox?.SetEnabled(true);

            //reset animator and sprite trail
            _spriteAnimator.Color = Color.White;
            _spriteTrail.DisableSpriteTrail();

            //start cooldown
            IsOnCooldown = true;
            Game1.Schedule(_shortDashCooldown, timer => IsOnCooldown = false);
        }

        public void Abort()
        {
            //hurtbox
            _hurtbox?.SetEnabled(true);

            //animator and sprite trail
            _spriteAnimator.Color = Color.White;
            _spriteTrail.DisableSpriteTrail();
        }
    }
}
