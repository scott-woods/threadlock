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

namespace Threadlock.Entities.Characters.Enemies.OrbMage
{
    public class OrbMageAttack : EnemyAction<OrbMage>
    {
        //constants
        const int _showVfxFrame = 6;

        //coroutines
        CoroutineManager _coroutineManager = new CoroutineManager();
        ICoroutine _orbMageAttackExecutionCoroutine;

        //entities
        OrbMageAttackVfx _attackVfx;

        public OrbMageAttack(OrbMage enemy) : base(enemy)
        {
        }

        protected override IEnumerator ExecutionCoroutine()
        {
            //create vfx entity
            _attackVfx = Entity.Scene.AddEntity(new OrbMageAttackVfx());
            var targetPos = _enemy.GetTarget().Position;
            if (_enemy.GetTarget().TryGetComponent<OriginComponent>(out var origin))
                targetPos = origin.Origin;
            targetPos += new Vector2(0, -17);
            _attackVfx.SetPosition(targetPos);

            if (_enemy.TryGetComponent<SpriteAnimator>(out var animator))
            {
                //play attack animation
                animator.Play("Attack", Nez.Sprites.SpriteAnimator.LoopMode.Once);
                animator.OnAnimationCompletedEvent += OnAnimationCompleted;

                //wait until show vfx frame
                while (animator.CurrentFrame < _showVfxFrame)
                    yield return null;
            }

            //wait for vfx to finish
            yield return _attackVfx.Play();
        }

        protected override void Reset()
        {
            _orbMageAttackExecutionCoroutine?.Stop();
            _orbMageAttackExecutionCoroutine = null;

            if (_enemy.TryGetComponent<SpriteAnimator>(out var animator))
                animator.OnAnimationCompletedEvent -= OnAnimationCompleted;
        }

        void OnAnimationCompleted(string animationName)
        {
            if (_enemy.TryGetComponent<SpriteAnimator>(out var animator))
            {
                animator.OnAnimationCompletedEvent -= OnAnimationCompleted;
                animator.SetSprite(animator.CurrentAnimation.Sprites.Last());
            }
        }
    }
}
