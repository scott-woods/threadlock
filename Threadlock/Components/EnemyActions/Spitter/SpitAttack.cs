using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using System.Collections;
using Threadlock.Entities.Characters.Enemies;
using Threadlock.Helpers;

namespace Threadlock.Components.EnemyActions.Spitter
{
    public class SpitAttack : EnemyAction
    {
        //consts
        const int _fireFrame = 3;
        const float _attackRange = 128f;

        //components
        SpriteAnimator _animator;

        AnimationWaiter _animationWaiter;

        #region LIFECYCLE

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            if (Enemy.TryGetComponent<SpriteAnimator>(out var animator))
            {
                _animator = animator;
                _animationWaiter = new AnimationWaiter(_animator);
            }
        }

        #endregion

        #region Enemy action implementation

        public override float CooldownTime => 0f;
        public override int Priority => 0;

        public override bool CanExecute()
        {
            if (EntityHelper.DistanceToEntity(Enemy, Enemy.TargetEntity) > _attackRange)
                return false;

            if (!EntityHelper.HasLineOfSight(Enemy, Enemy.TargetEntity))
                return false;

            return true;
        }

        protected override IEnumerator ExecutionCoroutine()
        {
            Core.StartCoroutine(_animationWaiter.WaitForAnimation("Attack"));

            while (_animator.IsAnimationActive("Attack") && _animator.AnimationState == SpriteAnimator.State.Running)
            {
                var dir = EntityHelper.DirectionToEntity(Enemy, Enemy.TargetEntity);
                if (Enemy.TryGetComponent<VelocityComponent>(out var vc))
                    vc.LastNonZeroDirection = dir;

                if (_animator.CurrentFrame == _fireFrame)
                {
                    Game1.AudioManager.PlaySound(Content.Audio.Sounds.Spitter_fire);

                    CreateProjectile(dir);

                    //var leftRotationMatrix = Matrix.CreateRotationZ(MathHelper.ToRadians(30));
                    //var leftRotatedDir = Vector2.Transform(dir, leftRotationMatrix);
                    //CreateProjectile(leftRotatedDir);

                    //var rightRotationMatrix = Matrix.CreateRotationZ(MathHelper.ToRadians(-30));
                    //var rightRotatedDir = Vector2.Transform(dir, rightRotationMatrix);
                    //CreateProjectile(rightRotatedDir);

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
            projectile.SetPosition(Enemy.Position);
        }
    }
}
