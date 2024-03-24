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

namespace Threadlock.Entities.Characters.Enemies.OrbMage
{
    public class OrbMageAttack : EnemyAction<OrbMage>
    {
        //constants
        const int _showVfxFrame = 6;
        const float _predictionOffset = 64;

        //entities
        OrbMageAttackVfx _attackVfx;

        //components
        SpriteAnimator _animator;

        //misc
        AnimationWaiter _animationWaiter;

        public OrbMageAttack(OrbMage enemy) : base(enemy)
        {
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            if (Entity.TryGetComponent<SpriteAnimator>(out var animator))
            {
                _animator = animator;
                _animationWaiter = new AnimationWaiter(animator);
            }
        }

        protected override IEnumerator ExecutionCoroutine()
        {
            //create vfx entity
            _attackVfx = Entity.Scene.AddEntity(new OrbMageAttackVfx());

            //play animation
            if (_animationWaiter != null)
                Game1.StartCoroutine(_animationWaiter.WaitForAnimation("Attack"));

            //wait for vfx frame
            while (_animator.IsAnimationActive("Attack") && _animator.CurrentFrame < _showVfxFrame)
                yield return null;

            //target player
            var targetPos = _enemy.TargetEntity.Position;
            if (_enemy.TargetEntity.TryGetComponent<OriginComponent>(out var origin))
                targetPos = origin.Origin;
            targetPos += new Vector2(0, -17);

            if (_enemy.TargetEntity.TryGetComponent<VelocityComponent>(out var vc))
            {
                targetPos += (vc.Direction * Nez.Random.Range(0, _predictionOffset));
            }

            _attackVfx.SetPosition(targetPos);

            //wait for vfx to finish
            yield return _attackVfx.Play();
        }

        protected override void Reset()
        {
            _animationWaiter?.Cancel();
        }
    }
}
