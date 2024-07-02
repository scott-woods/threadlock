using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using Nez.Tweens;
using System;
using System.Collections;
using System.Collections.Generic;
using Threadlock.Components;
using Threadlock.Components.Hitboxes;
using Threadlock.Helpers;
using Threadlock.SaveData;
using Threadlock.StaticData;

namespace Threadlock.Entities.Characters.Player.BasicWeapons
{
    public class Sword : BasicWeapon
    {
        //constants
        const float _parryWindow = .09f;
        const float _parryDuration = .5f;
        const float _moveSpeed = 150f;
        const float _finisherMoveSpeed = 200f;
        const int _damage = 1;
        const float _normalPushForce = 1f;
        const float _finisherPushForce = 1.5f;
        const float _hitboxOffset = 12f;
        const float _progressRequiredToContinue = .7f;
        const float _progressRequiredForFinisher = 1f;
        EaseType _normalEaseType = EaseType.CubicOut;
        EaseType _finisherEaseType = EaseType.CubicOut;

        //passed components
        SpriteAnimator _animator;
        VelocityComponent _velocityComponent;
        Hurtbox _hurtbox;

        //added components
        CircleHitbox _hitbox;
        Collider _parryCollider;

        //coroutines
        ICoroutine _attackCoroutine;

        #region LIFECYCLE

        public override void Initialize()
        {
            base.Initialize();

            _hitbox = Entity.AddComponent(new CircleHitbox(_damage, 12));
            WatchHitbox(_hitbox);
            Flags.SetFlagExclusive(ref _hitbox.PhysicsLayer, PhysicsLayers.PlayerHitbox);
            Flags.SetFlagExclusive(ref _hitbox.CollidesWithLayers, PhysicsLayers.EnemyHurtbox);
            _hitbox.SetEnabled(false);
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            _animator = Entity.GetComponent<SpriteAnimator>();
            AnimatedSpriteHelper.LoadAnimationsGlobal(ref _animator, "Player_Thrust", "Player_Slash");
            _velocityComponent = Entity.GetComponent<VelocityComponent>();
            _hurtbox = Entity.GetComponent<Hurtbox>();

            _parryCollider = Entity.AddComponent(_hurtbox.Collider.Clone() as Collider);
            Flags.SetFlagExclusive(ref _parryCollider.PhysicsLayer, PhysicsLayers.None);
            Flags.SetFlagExclusive(ref _parryCollider.CollidesWithLayers, PhysicsLayers.EnemyHitbox);
            _parryCollider.IsTrigger = true;
            _parryCollider.SetEnabled(false);
        }

        #endregion

        #region BASIC WEAPON

        public override bool CanMove => false;

        public override bool Poll()
        {
            if (Controls.Instance.Melee.IsPressed)
            {
                QueuedAction = SwordAttack;
                return true;
            }
            else if (Controls.Instance.AltAttack.IsPressed)
            {
                QueuedAction = Parry;
                return true;
            }

            return false;
        }

        public override void OnUnequipped()
        {

        }

        public override void Reset()
        {
            //hitbox
            _hitbox.SetEnabled(false);
            _hitbox.PushForce = _normalPushForce;

            //parry collider
            _parryCollider.SetEnabled(false);

            //coroutines
            _attackCoroutine?.Stop();
            _attackCoroutine = null;
        }

        #endregion

        IEnumerator SwordAttack()
        {
            _attackCoroutine = Game1.StartCoroutine(StartMeleeAttack(1, Player.GetFacingDirection()));
            yield return _attackCoroutine;
        }

        /// <summary>
        /// called for each swing of the sword
        /// </summary>
        /// <param name="comboCount"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        IEnumerator StartMeleeAttack(int comboCount, Vector2 dir)
        {
            //update hitbox
            _hitbox.PushForce = comboCount == 3 ? _finisherPushForce : _normalPushForce;
            _hitbox.SetLocalOffset(dir * _hitboxOffset);
            _hitbox.Direction = dir;

            //play animation
            var animation = comboCount == 2 ? "Player_Slash" : "Player_Thrust";
            AnimatedSpriteHelper.PlayAnimation(ref _animator, animation);
            
            var animDuration = AnimatedSpriteHelper.GetAnimationDuration(_animator);
            var movementTime = animDuration;

            //while animation is playing...
            var initialSpeed = comboCount == 3 ? _finisherMoveSpeed : _moveSpeed;
            bool shouldAttackAgain = false;
            Vector2 nextAttackDir = Vector2.Zero;
            var requiredProgress = comboCount == 3 ? _progressRequiredForFinisher : _progressRequiredToContinue;
            var elapsedTime = 0f;
            var easeType = comboCount == 3 ? _finisherEaseType : _normalEaseType;
            while (AnimatedSpriteHelper.IsAnimationPlaying(_animator, animation))
            {
                //soon as we're not on the first frame, start checking for next attack
                if (comboCount < 3 && elapsedTime > 0)
                {
                    if (Controls.Instance.Melee.IsPressed)
                    {
                        shouldAttackAgain = true;
                        nextAttackDir = Player.GetFacingDirection();
                    }
                }

                //handle hitbox
                _hitbox.SetEnabled(_animator.CurrentFrame == 0);

                //move player
                var progress = elapsedTime / animDuration;
                if (elapsedTime <= movementTime)
                {
                    var movementLerp = Lerps.Ease(easeType, initialSpeed, 0, Math.Clamp(elapsedTime, 0, movementTime), movementTime);
                    _velocityComponent.Move(dir, movementLerp, true);
                }

                //once more than halfway through, if we've queued up another, go ahead and start it
                if (shouldAttackAgain && progress > requiredProgress)
                    break;

                elapsedTime += Time.DeltaTime;

                yield return null;
            }

            //disable hitbox just in case it's somehow still enabled
            _hitbox.SetEnabled(false);

            //if should continue combo, start new coroutine
            if (shouldAttackAgain)
            {
                _attackCoroutine = Game1.StartCoroutine(StartMeleeAttack(comboCount + 1, nextAttackDir));
                yield return _attackCoroutine;
            }
            else
                yield break;
        }

        IEnumerator Parry()
        {
            //TODO: play parry start animation

            //play sound
            Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds._40_Block_04);

            //disable hurtbox
            _hurtbox.SetEnabled(false);

            //enable parry collider
            _parryCollider.SetEnabled(true);

            //wait for the parry window time frame
            var timer = 0f;
            while (timer <= _parryDuration)
            {
                if (timer <= _parryWindow)
                {
                    //check for collisions with the parry collider
                    if (_parryCollider.CollidesWithAny(out var collisionResult))
                    {
                        //check if the collider is a projectile
                        if (collisionResult.Collider.Entity is Projectile projectile)
                        {
                            //reflect the projectile and play a sound
                            projectile.Reflect();

                            //play reflect sound
                            Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds._19_Slash_01);

                            //TODO: play successful parry animation

                            //wait a moment before finishing up
                            yield return Coroutine.WaitForSeconds(.25f);

                            break;
                        }
                    }
                }
                else
                {
                    //disable parry collider
                    _parryCollider.SetEnabled(false);

                    //re enable hurtbox
                    _hurtbox.SetEnabled(true);
                }

                //increment timer
                timer += Time.DeltaTime;
                yield return null;
            }

            //disable parry collider
            _parryCollider.SetEnabled(false);

            //re enable hurtbox
            _hurtbox.SetEnabled(true);
        }
    }
}
