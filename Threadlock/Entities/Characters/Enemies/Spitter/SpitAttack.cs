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
using Threadlock.Helpers;

namespace Threadlock.Entities.Characters.Enemies.Spitter
{
    public class SpitAttack : EnemyAction<Spitter>
    {
        //consts
        const int _fireFrame = 3;

        //components
        SpriteAnimator _animator;

        AnimationWaiter _animationWaiter;

        public SpitAttack(Spitter enemy) : base(enemy)
        {
        }

        #region LIFECYCLE

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            if (_enemy.TryGetComponent<SpriteAnimator>(out var animator))
            {
                _animator = animator;
                _animationWaiter = new AnimationWaiter(_animator);
            }
        }

        #endregion

        #region ENEMY ACTION OVERRIDES

        protected override IEnumerator ExecutionCoroutine()
        {
            Game1.StartCoroutine(_animationWaiter.WaitForAnimation("Attack"));

            while (_animator.IsAnimationActive("Attack") && _animator.AnimationState == SpriteAnimator.State.Running)
            {
                if (_animator.CurrentFrame == _fireFrame)
                {
                    Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds.Spitter_fire);

                    var dir = EntityHelper.DirectionToEntity(_enemy, _enemy.TargetEntity);

                    CreateProjectile(dir);

                    var leftRotationMatrix = Matrix.CreateRotationZ(MathHelper.ToRadians(30));
                    var leftRotatedDir = Vector2.Transform(dir, leftRotationMatrix);
                    CreateProjectile(leftRotatedDir);

                    var rightRotationMatrix = Matrix.CreateRotationZ(MathHelper.ToRadians(-30));
                    var rightRotatedDir = Vector2.Transform(dir, rightRotationMatrix);
                    CreateProjectile(rightRotatedDir);

                    break;
                }

                yield return null;
            }

            while (_animator.IsAnimationActive("Attack") && _animator.AnimationState == SpriteAnimator.State.Running)
                yield return null;
        }

        protected override void Reset()
        {
            _animationWaiter?.Cancel();
        }

        #endregion

        void CreateProjectile(Vector2 dir)
        {
            var projectile = Entity.Scene.AddEntity(new SpitAttackProjectile(dir));
            projectile.SetPosition(_enemy.Position);
        }
    }
}
