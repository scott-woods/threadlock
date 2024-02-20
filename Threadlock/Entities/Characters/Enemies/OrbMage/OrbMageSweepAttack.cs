using Nez;
using Nez.Sprites;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Helpers;

namespace Threadlock.Entities.Characters.Enemies.OrbMage
{
    public class OrbMageSweepAttack : EnemyAction<OrbMage>
    {
        //consts
        const int _pickDirectionFrame = 4;
        const int _startFrame = 8;

        //components
        SpriteAnimator _animator;

        OrbMageSweepAttackVfx _attackVfx;

        AnimationWaiter _animationWaiter;

        public OrbMageSweepAttack(OrbMage enemy) : base(enemy) { }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            if (_enemy.TryGetComponent<SpriteAnimator>(out var animator))
                _animator = animator;

            _animationWaiter = new AnimationWaiter(_animator);
        }

        protected override IEnumerator ExecutionCoroutine()
        {
            _animationWaiter.WaitForAnimation("SweepAttack");

            while (_animator.CurrentFrame < _pickDirectionFrame)
                yield return null;

            var dir = EntityHelper.DirectionToEntity(_enemy, _enemy.TargetEntity);

            while (_animator.CurrentFrame < _startFrame)
                yield return null;

            //create vfx entity
            _attackVfx = Entity.Scene.AddEntity(new OrbMageSweepAttackVfx(dir));
            _attackVfx.SetPosition(Entity.Position);
            yield return _attackVfx.Play();
        }

        protected override void Reset()
        {
            _animationWaiter.Cancel();
        }
    }
}
