using Nez;
using Nez.Sprites;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Entities.Characters.Enemies;
using Threadlock.Helpers;

namespace Threadlock.Components.EnemyActions.OrbMage
{
    public class OrbMageSweepAttack : EnemyAction
    {
        //consts
        const int _pickDirectionFrame = 4;
        const int _startFrame = 8;
        const float _sweepAttackRange = 64f;

        //components
        SpriteAnimator _animator;

        OrbMageSweepAttackVfx _attackVfx;

        AnimationWaiter _animationWaiter;

        #region LIFECYCLE

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            if (Enemy.TryGetComponent<SpriteAnimator>(out var animator))
                _animator = animator;

            _animationWaiter = new AnimationWaiter(_animator);
        }

        #endregion

        #region Enemy action implementation

        public override float CooldownTime => 0f;
        public override int Priority => 0;

        public override bool CanExecute()
        {
            return EntityHelper.DistanceToEntity(Enemy, Enemy.TargetEntity) <= _sweepAttackRange;
        }

        protected override IEnumerator ExecutionCoroutine()
        {
            Core.StartCoroutine(_animationWaiter.WaitForAnimation("SweepAttack"));

            while (_animator.CurrentFrame < _pickDirectionFrame)
                yield return null;

            var dir = EntityHelper.DirectionToEntity(Enemy, Enemy.TargetEntity);

            _attackVfx = Entity.Scene.AddEntity(new OrbMageSweepAttackVfx(dir));

            while (_animator.CurrentFrame < _startFrame)
                yield return null;

            //create vfx entity
            _attackVfx.SetPosition(Entity.Position);
            yield return _attackVfx.Play();
        }

        protected override void Reset()
        {
            _animationWaiter.Cancel();
            _attackVfx?.Destroy();
            _attackVfx = null;
        }

        #endregion
    }
}
