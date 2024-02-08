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
using Threadlock.Helpers;
using Threadlock.StaticData;

namespace Threadlock.Entities.Characters.Enemies.Ghoul
{
    public class GhoulAttack : EnemyAction<Ghoul>
    {
        //const
        Vector2 _hitboxOffset = new Vector2(9, 0);

        //components
        CircleHitbox _hitbox;
        SpriteAnimator _animator;
        VelocityComponent _velocityComponent;

        AnimationWaiter _animationWaiter;

        public GhoulAttack(Ghoul enemy) : base(enemy)
        {
        }

        public override void Initialize()
        {
            base.Initialize();

            _hitbox = Entity.AddComponent(new CircleHitbox(1, 8));
            Flags.SetFlagExclusive(ref _hitbox.PhysicsLayer, (int)PhysicsLayers.EnemyHitbox);
            Flags.SetFlagExclusive(ref _hitbox.CollidesWithLayers, (int)PhysicsLayers.PlayerHurtbox);
            _hitbox.SetEnabled(false);
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            if (_enemy.TryGetComponent<SpriteAnimator>(out var animator))
            {
                _animator = animator;
                _animationWaiter = new AnimationWaiter(_animator);
            }

            if (_enemy.TryGetComponent<VelocityComponent>(out var vc))
                _velocityComponent = vc;
        }

        #region ENEMY ACTION OVERRIDES

        protected override IEnumerator ExecutionCoroutine()
        {
            Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds.Ghoul_claw);
            Game1.StartCoroutine(_animationWaiter.WaitForAnimation("Attack"));
            while (_animator.IsAnimationActive("Attack") && _animator.AnimationState == SpriteAnimator.State.Running)
            {
                //update hitbox offset
                if (_velocityComponent.Direction.X >= 0)
                    _hitbox.SetLocalOffset(_hitboxOffset);
                else _hitbox.SetLocalOffset(-_hitboxOffset);

                if (_animator.CurrentFrame == 2)
                    _hitbox.SetEnabled(true);
                else _hitbox.SetEnabled(false);

                yield return null;
            }
        }

        protected override void Reset()
        {
            _hitbox.SetEnabled(false);
            _animationWaiter?.Cancel();
        }

        #endregion
    }
}
