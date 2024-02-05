using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Entities.Characters.Player.BasicWeapons;

namespace Threadlock.Entities.Characters.Player
{
    public class ApComponent : Component
    {
        public event Action<int, float> OnApChanged;

        const float _hitMultiplier = 3f;
        const float _hurtMultiplier = 2f;

        int _actionPoints;
        public int ActionPoints
        {
            get => _actionPoints;
            set
            {
                var oldValue = _actionPoints;
                var newValue = Math.Clamp(value, 0, MaxActionPoints);

                _actionPoints = newValue;

                OnApChanged?.Invoke(_actionPoints, (float)_progress / (float)_damageRequired);
            }
        }

        public int MaxActionPoints;

        int _progress = 0;
        int _damageRequired = 10;

        public ApComponent(int maxAp)
        {
            MaxActionPoints = maxAp;
        }

        #region LIFECYCLE

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            if (Entity.TryGetComponent<HealthComponent>(out var hc))
            {
                hc.OnHealthChanged += OnHealthChanged;
            }

            if (Entity.TryGetComponent<BasicWeapon>(out var weapon))
            {
                weapon.Emitter.AddObserver(BasicWeaponEventTypes.Hit, OnBasicAttackHit);
            }
        }

        public override void OnRemovedFromEntity()
        {
            base.OnRemovedFromEntity();

            if (Entity.TryGetComponent<HealthComponent>(out var hc))
            {
                hc.OnHealthChanged -= OnHealthChanged;
            }

            if (Entity.TryGetComponent<BasicWeapon>(out var weapon))
            {
                weapon.Emitter.RemoveObserver(BasicWeaponEventTypes.Hit, OnBasicAttackHit);
            }
        }

        #endregion

        void AddProgress(float amount)
        {
            _progress += (int)amount;

            if (_progress >= _damageRequired && ActionPoints < MaxActionPoints)
            {
                _progress -= _damageRequired;
                ActionPoints += 1;
            }
            else
            {
                OnApChanged?.Invoke(ActionPoints, (float)_progress / (float)_damageRequired);
            }
        }

        void OnBasicAttackHit(int damageAmount)
        {
            if (ActionPoints >= MaxActionPoints)
                return;

            AddProgress(damageAmount * _hitMultiplier);
        }

        void OnHealthChanged(int oldValue, int newValue)
        {
            //if health is the same or increased, we didn't take damage. do nothing
            if (newValue >= oldValue)
                return;

            AddProgress((oldValue - newValue) * _hurtMultiplier);
        }
    }
}
