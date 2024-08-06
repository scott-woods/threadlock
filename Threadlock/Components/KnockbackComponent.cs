using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez;
using Nez.Sprites;
using Nez.Tweens;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Entities.Characters.Player;
using Threadlock.Helpers;
using Threadlock.Models;

namespace Threadlock.Components
{
    public class KnockbackComponent : Component
    {
        //constants
        const StatusPriority _statusPriority = StatusPriority.Stunned;

        //params
        float _baseSpeed = 0f;
        float _knockbackDuration = 0f;
        string _hitAnimation;

        //components
        VelocityComponent _velocityComponent;

        //coroutines
        ICoroutine _knockbackCoroutine;

        public KnockbackComponent(VelocityComponent velocityComponent, string hitAnimation, float speed = 110f, float knockbackDuration = .5f)
        {
            _velocityComponent = velocityComponent;
            _baseSpeed = speed;
            _knockbackDuration = knockbackDuration;
            _hitAnimation = hitAnimation;
        }

        #region LIFECYCLE

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            if (Entity.TryGetComponent<Hurtbox>(out var hurtbox))
                hurtbox.Emitter.AddObserver(HurtboxEventTypes.Hit, OnHurtboxHit);

            if (Entity.TryGetComponent<DeathComponent>(out var dc))
                dc.Emitter.AddObserver(DeathEventTypes.Started, OnDeathStarted);
        }

        public override void OnRemovedFromEntity()
        {
            base.OnRemovedFromEntity();

            if (Entity.TryGetComponent<Hurtbox>(out var hurtbox))
                hurtbox.Emitter.RemoveObserver(HurtboxEventTypes.Hit, OnHurtboxHit);

            if (Entity.TryGetComponent<DeathComponent>(out var dc))
                dc.Emitter.RemoveObserver(DeathEventTypes.Started, OnDeathStarted);
        }

        #endregion

        #region OBSERVERS

        void OnHurtboxHit(HurtboxHit hit)
        {
            Debug.Log("Hurtbox hit, knockback starting");

            //handle status component
            if (Entity.TryGetComponent<StatusComponent>(out var statusComponent))
            {
                if (!statusComponent.PushStatus(_statusPriority))
                {
                    return;
                }
            }

            //get direction
            var dir = hit.Direction;

            //determine knockback speed
            var initialSpeed = _baseSpeed * hit.PushForce;

            //if already in knockback, cancel the original
            _knockbackCoroutine?.Stop();
            _knockbackCoroutine = null;

            //start knockback
            _knockbackCoroutine = Game1.StartCoroutine(Knockback(dir, initialSpeed));
        }

        void OnDeathStarted(Entity entity)
        {
            if (_knockbackCoroutine != null)
                EndKnockback();
        }

        #endregion

        void EndKnockback()
        {
            _knockbackCoroutine?.Stop();
            _knockbackCoroutine = null;

            //if (Entity.TryGetComponent<SpriteAnimator>(out var animator))
            //{
            //    animator.Stop();
            //}

            var renderables = Entity.GetComponents<RenderableComponent>();
            foreach (var renderable in renderables)
            {
                if (renderable.Material != null)
                {
                    renderable.Material.Dispose();
                }
            }

            if (Entity.TryGetComponent<StatusComponent>(out var statusComponent))
                statusComponent.PopStatus(_statusPriority);
        }

        #region COROUTINES

        IEnumerator Knockback(Vector2 direction, float speed)
        {
            //wait one frame so we don't mess up physics stuff
            yield return null;

            //play animation if possible
            if (Entity.TryGetComponent<SpriteAnimator>(out var animator))
            {
                AnimatedSpriteHelper.PlayAnimation(animator, _hitAnimation);
            }

            //apply effect
            var blinkEffect = new SpriteBlinkEffect();
            blinkEffect.BlinkColor = Entity.GetType() == typeof(Player) ? Color.White : Color.Red;
            var renderables = Entity.GetComponents<RenderableComponent>();
            foreach (var renderable in renderables.Where(r => r.GetType() != typeof(Shadow)))
            {
                renderable.Material = new Material(BlendState.NonPremultiplied, blinkEffect);
            }

            var time = 0f;
            while (time < _knockbackDuration)
            {
                time += Time.DeltaTime;

                var currentSpeed = Lerps.Ease(EaseType.CubicOut, speed, 0f, time, _knockbackDuration);
                _velocityComponent.Move(direction, currentSpeed, false, false);

                yield return null;
            }

            EndKnockback();
        }

        #endregion
    }
}
