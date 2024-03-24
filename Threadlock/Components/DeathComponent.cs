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
        public Emitter<DeathEventTypes, Entity> Emitter = new Emitter<DeathEventTypes, Entity>();

        string _deathAnimName;
        string _sound;
        bool _destroy;

        public DeathComponent(string deathAnimationName, string sound, bool destroy = true)
        {
            _deathAnimName = deathAnimationName;
            _sound = sound;
            _destroy = destroy;
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            if (Entity.TryGetComponent<HealthComponent>(out var hc))
                hc.OnHealthDepleted += OnHealthDepleted;
        }

        void Die()
        {
            Emitter.Emit(DeathEventTypes.Started, Entity);

            Game1.AudioManager.PlaySound(_sound);

            if (Entity.TryGetComponent<SpriteAnimator>(out var animator))
            {
                animator.Play(_deathAnimName, SpriteAnimator.LoopMode.Once);
                animator.OnAnimationCompletedEvent += OnAnimationCompleted;
            }
            else
                Finished();
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

            Finished();
        }

        void Finished()
        {
            Emitter.Emit(DeathEventTypes.Finished, Entity);

            if (_destroy)
                Entity.Destroy();
        }
    }

    public enum DeathEventTypes
    {
        Started,
        Finished
    }
}
