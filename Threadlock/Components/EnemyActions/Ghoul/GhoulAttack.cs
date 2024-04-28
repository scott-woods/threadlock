using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Components.Hitboxes;
using Threadlock.Entities.Characters.Enemies;
using Threadlock.Helpers;
using Threadlock.StaticData;

namespace Threadlock.Components.EnemyActions.Ghoul
{
    public class GhoulAttack : EnemyAction
    {
        //const
        float _hitboxOffset = 9f;

        //components
        CircleHitbox _hitbox;
        SpriteAnimator _animator;
        VelocityComponent _velocityComponent;

        AnimationWaiter _animationWaiter;

        #region LIFECYCLE

        public override void Initialize()
        {
            base.Initialize();

            _hitbox = Entity.AddComponent(new CircleHitbox(1, 8));
            Flags.SetFlagExclusive(ref _hitbox.PhysicsLayer, PhysicsLayers.EnemyHitbox);
            Flags.SetFlagExclusive(ref _hitbox.CollidesWithLayers, PhysicsLayers.PlayerHurtbox);
            _hitbox.SetEnabled(false);
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            if (Enemy.TryGetComponent<SpriteAnimator>(out var animator))
            {
                _animator = animator;
                _animationWaiter = new AnimationWaiter(_animator);
            }

            if (Enemy.TryGetComponent<VelocityComponent>(out var vc))
                _velocityComponent = vc;
        }

        #endregion

        #region Enemy action implementation

        public override float CooldownTime => 0f;
        public override int Priority => 0;

        public override bool CanExecute()
        {
            var target = Enemy.TargetEntity;
            var distance = EntityHelper.DirectionToEntity(Enemy, target, false);
            if (Math.Abs(distance.X) <= 16 && Math.Abs(distance.Y) <= 8)
                return true;
            return false;
        }

        protected override IEnumerator ExecutionCoroutine()
        {
            //update hitbox offset
            var dir = EntityHelper.DirectionToEntity(Enemy, Enemy.TargetEntity, true, false);
            _hitbox.SetLocalOffset(dir * _hitboxOffset);
            Game1.AudioManager.PlaySound(Content.Audio.Sounds.Ghoul_claw);
            Core.StartCoroutine(_animationWaiter.WaitForAnimation("Attack"));
            while (_animator.IsAnimationActive("Attack") && _animator.AnimationState == SpriteAnimator.State.Running)
            {
                if (_animator.CurrentFrame == 2)
                    _hitbox.SetEnabled(true);
                else _hitbox.SetEnabled(false);

                yield return null;
            }

            _hitbox.SetEnabled(false);
        }

        protected override void Reset()
        {
            _hitbox.SetEnabled(false);
            _animationWaiter?.Cancel();
        }

        #endregion
    }
}
