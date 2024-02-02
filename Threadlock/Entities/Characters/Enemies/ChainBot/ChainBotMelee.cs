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

namespace Threadlock.Entities.Characters.Enemies.ChainBot
{
    public class ChainBotMelee : EnemyAction<ChainBot>, IUpdatable
    {
        //consts
        const int _damage = 2;
        Vector2 _offset = new Vector2(28, 4);
        List<int> _hitboxActiveFrames = new List<int> { 0, 4 };

        //components
        BoxHitbox _hitbox;
        SpriteAnimator _animator;

        //misc
        int _soundCounter = 0;

        //coroutines
        ICoroutine _waitForCharge, _attack;

        public ChainBotMelee(ChainBot enemy) : base(enemy)
        {
        }

        public override void Initialize()
        {
            base.Initialize();

            _animator = Entity.GetComponent<SpriteAnimator>();

            _hitbox = Entity.AddComponent(new BoxHitbox(_damage, 40, 10));
            Flags.SetFlagExclusive(ref _hitbox.PhysicsLayer, PhysicsLayers.EnemyHitbox);
            Flags.SetFlagExclusive(ref _hitbox.CollidesWithLayers, PhysicsLayers.PlayerHurtbox);
            _hitbox.SetLocalOffset(_offset);
            _hitbox.SetEnabled(false);
        }

        public void Update()
        {
            if (_animator.CurrentAnimationName == "AttackRight" && _animator.AnimationState == SpriteAnimator.State.Running)
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

        protected override IEnumerator ExecutionCoroutine()
        {
            //transition to charge
            _waitForCharge = Game1.StartCoroutine(CoroutineHelper.WaitForAnimation(_animator, "TransitionRight"));
            yield return _waitForCharge;
            _waitForCharge = null;

            //charge
            _animator.Play("ChargeRight");
            yield return Coroutine.WaitForSeconds(.2f);

            //attack
            var hitboxOffset = _offset;
            if (Entity.TryGetComponent<SpriteFlipper>(out var spriteFlipper))
            {
                if (spriteFlipper.Flipped)
                    hitboxOffset.X *= -1;
            }
            _hitbox.SetLocalOffset(hitboxOffset);
            _animator.OnAnimationCompletedEvent += OnAnimationCompleted;
            _attack = Game1.StartCoroutine(CoroutineHelper.WaitForAnimation(_animator, "AttackRight"));
            yield return _attack;
            _attack = null;

            _soundCounter = 0;
        }

        public override void Abort()
        {
            base.Abort();

            _waitForCharge?.Stop();
            _waitForCharge = null;

            _attack?.Stop();
            _attack = null;

            _hitbox.SetEnabled(false);

            _animator.OnAnimationCompletedEvent -= OnAnimationCompleted;

            _soundCounter = 0;
        }

        void OnAnimationCompleted(string animationName)
        {
            _animator.SetSprite(_animator.CurrentAnimation.Sprites.Last());
            _animator.OnAnimationCompletedEvent -= OnAnimationCompleted;
        }
    }
}
