using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Models;

namespace Threadlock.Components
{
    public class HealthComponent : Component
    {
        /// <summary>
        /// fire when health changes, returns previous value and new value
        /// </summary>
        public event Action<int, int> OnHealthChanged;

        public event Action OnHealthDepleted;

        int _health;
        public int Health
        {
            get => _health;
            set
            {
                var prevHealth = _health;

                var newHealth = Math.Clamp(value, 0, MaxHealth);

                _health = newHealth;

                if (prevHealth != newHealth)
                    OnHealthChanged?.Invoke(prevHealth, newHealth);

                if (newHealth == 0)
                    OnHealthDepleted?.Invoke();
            }
        }

        public int MaxHealth;

        public HealthComponent(int maxHealth, int health)
        {
            MaxHealth = maxHealth;
            Health = health;
        }

        #region LIFECYCLE

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            if (Entity.TryGetComponent<Hurtbox>(out var hurtbox))
                hurtbox.Emitter.AddObserver(HurtboxEventTypes.Hit, OnHurtboxHit);
        }

        public override void OnRemovedFromEntity()
        {
            base.OnRemovedFromEntity();

            if (Entity.TryGetComponent<Hurtbox>(out var hurtbox))
                hurtbox.Emitter.RemoveObserver(HurtboxEventTypes.Hit, OnHurtboxHit);
        }

        #endregion

        #region OBSERVERS

        void OnHurtboxHit(HurtboxHit hit)
        {
            Health -= hit.Hitbox.Damage;
        }

        #endregion
    }
}
