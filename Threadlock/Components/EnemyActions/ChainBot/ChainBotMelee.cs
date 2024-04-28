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

namespace Threadlock.Components.EnemyActions.ChainBot
{
    public class ChainBotMelee : EnemyAction, IUpdatable
    {
        //consts
        const int _damage = 2;
        Vector2 _offset = new Vector2(20, 4);
        List<int> _hitboxActiveFrames = new List<int> { 0, 4 };

        //components
        BoxHitbox _hitbox;
        SpriteAnimator _animator;

        //misc
        int _soundCounter = 0;
        AnimationWaiter _animationWaiter;

        //coroutines
        ICoroutine _waitForCharge, _attack;

        #region LIFECYCLE

        public override void Initialize()
        {
            base.Initialize();

            _animator = Entity.GetComponent<SpriteAnimator>();

            _hitbox = Entity.AddComponent(new BoxHitbox(_damage, 48, 10));
            Flags.SetFlagExclusive(ref _hitbox.PhysicsLayer, PhysicsLayers.EnemyHitbox);
            Flags.SetFlagExclusive(ref _hitbox.CollidesWithLayers, PhysicsLayers.PlayerHurtbox);
            _hitbox.SetLocalOffset(_offset);
            _hitbox.SetEnabled(false);

            _animationWaiter = new AnimationWaiter(_animator);
        }

        public void Update()
        {
            if (_animator.CurrentAnimationName == "Attack" && _animator.AnimationState == SpriteAnimator.State.Running)
            {
                if (_hitboxActiveFrames.Contains(_animator.CurrentFrame))
                {
                    if (_animator.CurrentFrame == _hitboxActiveFrames[0] && _soundCounter == 0)
                    {
                        _soundCounter += 1;
                        Game1.AudioManager.PlaySound(Content.Audio.Sounds._81_Whip_woosh_1);
                    }
                    else if (_animator.CurrentFrame == _hitboxActiveFrames[1] && _soundCounter == 1)
                    {
                        _soundCounter += 1;
                        Game1.AudioManager.PlaySound(Content.Audio.Sounds._81_Whip_woosh_1);
                    }

                    _hitbox?.SetEnabled(true);
                }
                else _hitbox?.SetEnabled(false);
            }
        }

        #endregion

        #region EnemyAction implementation

        public override float CooldownTime => 0;
        public override int Priority => 0;

        public override bool CanExecute()
        {
            var targetPos = Enemy.TargetEntity.Position;
            var xDist = Math.Abs(Enemy.Position.X - targetPos.X);
            var yDist = Math.Abs(Enemy.Position.Y - targetPos.Y);
            if (xDist <= 32 && yDist <= 8)
                return true;
            return false;
        }

        protected override IEnumerator ExecutionCoroutine()
        {
            var animationWaiter = new AnimationWaiter(_animator);

            //transition to charge
            _waitForCharge = Core.StartCoroutine(animationWaiter.WaitForAnimation("TransitionToCharge"));
            yield return _waitForCharge;
            _waitForCharge = null;

            //charge
            _animator.Play("Charge");
            yield return Coroutine.WaitForSeconds(.2f);

            //attack
            var hitboxOffset = _offset;
            if (Entity.TryGetComponent<SpriteFlipper>(out var spriteFlipper))
            {
                if (spriteFlipper.Flipped)
                    hitboxOffset.X *= -1;
            }
            _hitbox.SetLocalOffset(hitboxOffset);
            _attack = Core.StartCoroutine(animationWaiter.WaitForAnimation("Attack"));
            yield return _attack;
            _attack = null;
        }

        public override void Abort()
        {
            base.Abort();

            _waitForCharge?.Stop();
            _waitForCharge = null;

            _attack?.Stop();
            _attack = null;

            _hitbox.SetEnabled(false);

            _animationWaiter.Cancel();

            _soundCounter = 0;
        }

        protected override void Reset()
        {
            //values
            _soundCounter = 0;

            //make sure coroutines are null
            _waitForCharge = null;
            _attack = null;

            //disable hitbox
            _hitbox.SetEnabled(false);
        }

        #endregion
    }
}
