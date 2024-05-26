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
using Threadlock.Components.Hitboxes;
using Threadlock.Helpers;
using Threadlock.StaticData;

namespace Threadlock.Components.EnemyActions.Assassin
{
    public class AssassinBasicMelee : EnemyAction
    {
        const float _minDistance = 24;
        const float _hitboxRadius = 8f;
        const float _hitboxOffset = 13;
        const int _damage = 1;
        const string _chargeName = "Land";
        const string _attack1Name = "Attack_1";
        const string _attack2Name = "Attack_2";
        readonly List<int> _attack1ActiveFrames = new List<int> { 0 };
        readonly List<int> _attack2ActiveFrames = new List<int> { 0 };
        const float _attack1MoveSpeed = 450f;
        const float _attack2MoveSpeed = 520f;
        const float _secondAttackDelay = .2f;
        const string _attackSound = Nez.Content.Audio.Sounds._19_Slash_01;

        //existing components
        SpriteAnimator _animator;
        VelocityComponent _velocityComponent;

        //components
        CircleHitbox _hitbox;

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            //get pre existing components
            if (Entity.TryGetComponent<SpriteAnimator>(out var animator))
                _animator = animator;
            if (Entity.TryGetComponent<VelocityComponent>(out var velocityComponent))
                _velocityComponent = velocityComponent;

            //add hitbox
            _hitbox = Entity.AddComponent(new CircleHitbox(_damage, _hitboxRadius));
            Flags.SetFlagExclusive(ref _hitbox.PhysicsLayer, PhysicsLayers.EnemyHitbox);
            Flags.SetFlagExclusive(ref _hitbox.CollidesWithLayers, PhysicsLayers.PlayerHurtbox);
            _hitbox.SetEnabled(false);
        }

        #region Enemy action implementation

        public override float CooldownTime => 0f;

        public override int Priority => 0;

        public override bool CanExecute()
        {
            var dist = EntityHelper.DistanceToEntity(Enemy, Enemy.TargetEntity);
            if (dist <= _minDistance)
                return true;

            return false;
        }

        protected override IEnumerator ExecutionCoroutine()
        {
            //get direction to target
            var dir = EntityHelper.DirectionToEntity(Enemy, Enemy.TargetEntity);

            //update hitbox offset
            _hitbox.SetLocalOffset(dir * _hitboxOffset);

            //play charge animation
            _animator.Play(_chargeName, SpriteAnimator.LoopMode.Once);
            while (_animator.CurrentAnimationName == _chargeName && _animator.AnimationState != SpriteAnimator.State.Completed)
                yield return null;

            //perform attack 1
            _animator.Play(_attack1Name, SpriteAnimator.LoopMode.Once);
            Game1.AudioManager.PlaySound(_attackSound);
            var animDuration = AnimatedSpriteHelper.GetAnimationDuration(_animator);
            var timer = 0f;
            while (_animator.CurrentAnimationName == _attack1Name && _animator.AnimationState != SpriteAnimator.State.Completed)
            {
                timer += Time.DeltaTime;

                if (timer < animDuration)
                {
                    var speed = Lerps.Ease(EaseType.ExpoOut, _attack1MoveSpeed, 0, timer, animDuration);

                    _velocityComponent.Move(dir, speed, true);
                }

                if (_attack1ActiveFrames.Contains(_animator.CurrentFrame))
                    _hitbox.SetEnabled(true);
                else
                    _hitbox.SetEnabled(false);

                yield return null;
            }

            _hitbox.SetEnabled(false);

            //wait a moment before second part of attack
            yield return Coroutine.WaitForSeconds(_secondAttackDelay);

            //update dir
            dir = EntityHelper.DirectionToEntity(Enemy, Enemy.TargetEntity);
            _hitbox.SetLocalOffset(dir * _hitboxOffset);

            _animator.Play(_attack2Name, SpriteAnimator.LoopMode.Once);
            Game1.AudioManager.PlaySound(_attackSound);
            animDuration = AnimatedSpriteHelper.GetAnimationDuration(_animator);
            timer = 0f;

            while (_animator.CurrentAnimationName == _attack2Name && _animator.AnimationState != SpriteAnimator.State.Completed)
            {
                timer += Time.DeltaTime;

                if (timer < animDuration)
                {
                    var speed = Lerps.Ease(EaseType.ExpoOut, _attack2MoveSpeed, 0, timer, animDuration);
                    _velocityComponent.Move(dir, speed, true);
                }

                if (_attack2ActiveFrames.Contains(_animator.CurrentFrame))
                    _hitbox.SetEnabled(true);
                else
                    _hitbox.SetEnabled(false);

                yield return null;
            }

            _hitbox.SetEnabled(false);
        }

        protected override void Reset()
        {
            _hitbox.SetEnabled(false);
        }

        #endregion
    }
}
