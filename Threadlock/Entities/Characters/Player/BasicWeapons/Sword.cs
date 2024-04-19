using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez;
using Nez.Sprites;
using Nez.Systems;
using Nez.Textures;
using Nez.Tweens;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        readonly Dictionary<int, string> _soundDictionary = new Dictionary<int, string>()
        {
            [1] = Content.Audio.Sounds._32_Swoosh_sword_2,
            [2] = Content.Audio.Sounds._33_Swoosh_Sword_3,
            [3] = Content.Audio.Sounds._31_swoosh_sword_1
        };

        public override bool CanMove => false;

        //passed components
        SpriteAnimator _animator;
        VelocityComponent _velocityComponent;

        //added components
        CircleHitbox _hitbox;

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
            _velocityComponent = Entity.GetComponent<VelocityComponent>();
        }

        #endregion

        #region BASIC WEAPON

        public override bool Poll()
        {
            if (Controls.Instance.Melee.IsPressed)
            {
                _attackCoroutine = Game1.StartCoroutine(StartMeleeAttack(1, Player.GetFacingDirection()));
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

            //coroutines
            _attackCoroutine?.Stop();
            _attackCoroutine = null;
        }

        #endregion

        IEnumerator StartMeleeAttack(int comboCount, Vector2 dir)
        {
            //update hitbox
            _hitbox.PushForce = comboCount == 3 ? _finisherPushForce : _normalPushForce;
            _hitbox.SetLocalOffset(dir * _hitboxOffset);
            _hitbox.Direction = dir;

            //play sound
            Game1.AudioManager.PlaySound(_soundDictionary[comboCount]);

            //play animation
            var animation = comboCount == 2 ? "Slash" : "Thrust";
            animation += DirectionHelper.GetDirectionStringByVector(dir);
            _animator.Play(animation, SpriteAnimator.LoopMode.Once);
            var animDuration = AnimatedSpriteHelper.GetAnimationDuration(_animator);
            var movementTime = animDuration;

            //while animation is playing...
            var initialSpeed = comboCount == 3 ? _finisherMoveSpeed : _moveSpeed;
            bool shouldAttackAgain = false;
            Vector2 nextAttackDir = Vector2.Zero;
            var requiredProgress = comboCount == 3 ? _progressRequiredForFinisher : _progressRequiredToContinue;
            var elapsedTime = 0f;
            var easeType = comboCount == 3 ? _finisherEaseType : _normalEaseType;
            while (_animator.CurrentAnimationName == animation && _animator.AnimationState != SpriteAnimator.State.Completed)
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
                if (_animator.CurrentFrame == 0 && !_hitbox.Enabled)
                    _hitbox.SetEnabled(true);
                else if (_animator.CurrentFrame != 0 && _hitbox.Enabled)
                    _hitbox.SetEnabled(false);

                //move player
                var progress = elapsedTime / animDuration;
                if (elapsedTime <= movementTime)
                {
                    var movementLerp = Lerps.Ease(easeType, initialSpeed, 0, Math.Clamp(elapsedTime, 0, movementTime), movementTime);
                    _velocityComponent.Move(dir, movementLerp);
                }

                //once more than halfway through, if we've queued up another, go ahead and start it
                if (shouldAttackAgain && progress > requiredProgress)
                    break;

                elapsedTime += Time.DeltaTime;

                yield return null;
            }

            //disable hitbox just in case it's somehow still enabled
            if (_hitbox.Enabled)
                _hitbox.SetEnabled(false);

            //if should continue combo, start new coroutine
            if (shouldAttackAgain)
                _attackCoroutine = Game1.StartCoroutine(StartMeleeAttack(comboCount + 1, nextAttackDir));
            else
                CompletionEmitter.Emit(BasicWeaponEventTypes.Completed);
        }
    }
}
