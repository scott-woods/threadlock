using Nez;
using Nez.Sprites;
using Nez.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Components
{
    public class DeathComponent : Component
    {
        public Emitter<DeathEventTypes> Emitter = new Emitter<DeathEventTypes>();

        string _deathAnimName;
        string _sound;

        public DeathComponent(string deathAnimationName, string sound)
        {
            _deathAnimName = deathAnimationName;
            _sound = sound;
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            if (Entity.TryGetComponent<HealthComponent>(out var hc))
                hc.OnHealthDepleted += OnHealthDepleted;
        }

        void Die()
        {
            Emitter.Emit(DeathEventTypes.Started);

            Game1.AudioManager.PlaySound(_sound);

            if (Entity.TryGetComponent<SpriteAnimator>(out var animator))
            {
                animator.Play(_deathAnimName, SpriteAnimator.LoopMode.Once);
                animator.OnAnimationCompletedEvent += OnAnimationCompleted;
            }
            else
            {
                Emitter.Emit(DeathEventTypes.Finished);
                Entity.Destroy();
            }
        }

        void OnHealthDepleted()
        {
            if (Entity.TryGetComponent<StatusComponent>(out var statusComponent))
                statusComponent.PushStatus(StatusPriority.Death);

            Die();
        }

        void OnAnimationCompleted(string animationName)
        {
            if (Entity.TryGetComponent<SpriteAnimator>(out var animator))
            {
                animator.OnAnimationCompletedEvent -= OnAnimationCompleted;
                animator.SetSprite(animator.CurrentAnimation.Sprites.Last());
            }

            Emitter.Emit(DeathEventTypes.Finished);
            Entity.Destroy();
        }
    }

    public enum DeathEventTypes
    {
        Started,
        Finished
    }
}
