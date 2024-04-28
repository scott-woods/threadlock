using Nez.Systems;
using Nez;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Nez.Sprites;
using Microsoft.Xna.Framework;
using Threadlock.Helpers;
using Threadlock.Entities.Characters.Enemies;

namespace Threadlock.Components.EnemyActions.OrbMage
{
    public class OrbMageAttack : EnemyAction
    {
        //constants
        const int _showVfxFrame = 6;
        const float _predictionOffset = 64;
        const float _attackRange = 128f;
        const float _sweepAttackRange = 64f;

        //entities
        OrbMageAttackVfx _attackVfx;

        //components
        SpriteAnimator _animator;

        //misc
        AnimationWaiter _animationWaiter;

        #region LIFECYCLE

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            if (Entity.TryGetComponent<SpriteAnimator>(out var animator))
            {
                _animator = animator;
                _animationWaiter = new AnimationWaiter(animator);
            }
        }

        #endregion

        #region Enemy action implementation

        public override float CooldownTime => 0f;
        public override int Priority => 1;

        public override bool CanExecute()
        {
            var dist = EntityHelper.DistanceToEntity(Enemy, Enemy.TargetEntity);
            return dist <= _attackRange && dist > _sweepAttackRange;
        }

        protected override IEnumerator ExecutionCoroutine()
        {
            //create vfx entity
            _attackVfx = Entity.Scene.AddEntity(new OrbMageAttackVfx());

            //play animation
            if (_animationWaiter != null)
                Core.StartCoroutine(_animationWaiter.WaitForAnimation("Attack"));

            //wait for vfx frame
            while (_animator.IsAnimationActive("Attack") && _animator.CurrentFrame < _showVfxFrame)
                yield return null;

            //target player
            var targetPos = Enemy.TargetEntity.Position;
            if (Enemy.TargetEntity.TryGetComponent<OriginComponent>(out var origin))
                targetPos = origin.Origin;
            targetPos += new Vector2(0, -17);

            if (Enemy.TargetEntity.TryGetComponent<VelocityComponent>(out var vc))
            {
                targetPos += vc.Direction * Nez.Random.Range(0, _predictionOffset);
            }

            _attackVfx.SetPosition(targetPos);

            //wait for vfx to finish
            yield return _attackVfx.Play();
        }

        protected override void Reset()
        {
            _animationWaiter?.Cancel();
        }

        #endregion
    }
}
