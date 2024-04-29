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

        //components
        CircleHitbox _hitbox;

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
            var dir = EntityHelper.DirectionToEntity(Enemy, Enemy.TargetEntity);

            _hitbox = Entity.Scene.CreateEntity("hitbox").AddComponent(new CircleHitbox(_damage, _hitboxRadius));
            Flags.SetFlagExclusive(ref _hitbox.PhysicsLayer, PhysicsLayers.EnemyHitbox);
            Flags.SetFlagExclusive(ref _hitbox.CollidesWithLayers, PhysicsLayers.PlayerHurtbox);
            _hitbox.Entity.SetPosition(Entity.Position + (dir * _hitboxOffset));
            _hitbox.Entity.SetEnabled(false);

            var projectileMover = _hitbox.Entity.AddComponent(new ProjectileMover());

            var velocityComponent = Entity.GetComponent<VelocityComponent>();
            var animator = Entity.GetComponent<SpriteAnimator>();

            animator.Play(_chargeName, SpriteAnimator.LoopMode.Once);
            while (animator.CurrentAnimationName == _chargeName && animator.AnimationState != SpriteAnimator.State.Completed)
                yield return null;

            animator.Play(_attack1Name, SpriteAnimator.LoopMode.Once);
            Game1.AudioManager.PlaySound(_attackSound);
            var animDuration = AnimatedSpriteHelper.GetAnimationDuration(animator);
            var timer = 0f;
            while (animator.CurrentAnimationName == _attack1Name && animator.AnimationState != SpriteAnimator.State.Completed)
            {
                timer += Time.DeltaTime;

                if (timer < animDuration)
                {
                    var speed = Lerps.Ease(EaseType.ExpoOut, _attack1MoveSpeed, 0, timer, animDuration);
                    velocityComponent.Move(dir, speed);
                    projectileMover.Move(dir * speed * Time.DeltaTime);
                }

                if (_attack1ActiveFrames.Contains(animator.CurrentFrame))
                    _hitbox.Entity.SetEnabled(true);
                else
                    _hitbox.Entity.SetEnabled(false);

                yield return null;
            }

            _hitbox.Entity.SetEnabled(false);

            //wait a moment before second part of attack
            yield return Coroutine.WaitForSeconds(_secondAttackDelay);

            //update dir
            dir = EntityHelper.DirectionToEntity(Enemy, Enemy.TargetEntity);
            _hitbox.Entity.SetPosition(Entity.Position + (dir * _hitboxOffset));

            animator.Play(_attack2Name, SpriteAnimator.LoopMode.Once);
            Game1.AudioManager.PlaySound(_attackSound);
            animDuration = AnimatedSpriteHelper.GetAnimationDuration(animator);
            timer = 0f;

            while (animator.CurrentAnimationName == _attack2Name && animator.AnimationState != SpriteAnimator.State.Completed)
            {
                timer += Time.DeltaTime;

                if (timer < animDuration)
                {
                    var speed = Lerps.Ease(EaseType.ExpoOut, _attack2MoveSpeed, 0, timer, animDuration);
                    velocityComponent.Move(dir, speed);
                    projectileMover.Move(dir * speed * Time.DeltaTime);
                }

                if (_attack2ActiveFrames.Contains(animator.CurrentFrame))
                    _hitbox.Entity.SetEnabled(true);
                else
                    _hitbox.Entity.SetEnabled(false);

                yield return null;
            }

            _hitbox.Entity.SetEnabled(false);
            _hitbox.Entity.Destroy();
        }

        protected override void Reset()
        {
            _hitbox?.Entity?.Destroy();
            _hitbox = null;
        }

        #endregion
    }
}
