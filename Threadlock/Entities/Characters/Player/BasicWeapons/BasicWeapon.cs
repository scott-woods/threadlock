using Nez;
using Nez.Systems;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components.Hitboxes;

namespace Threadlock.Entities.Characters.Player.BasicWeapons
{
    public abstract class BasicWeapon : Component
    {
        public Emitter<BasicWeaponEventTypes, int> Emitter = new Emitter<BasicWeaponEventTypes, int>();
        public Player Player { get => Entity as Player; }
        public Func<IEnumerator> QueuedAction;

        ICoroutine _actionCoroutine;

        /// <summary>
        /// is player allowed to move while attacking with this weapon
        /// </summary>
        public abstract bool CanMove { get; }

        /// <summary>
        /// poll for input, returns true if weapon wants to do something
        /// </summary>
        /// <returns></returns>
        public abstract bool Poll();

        public virtual void Reset()
        {
            _actionCoroutine?.Stop();
            _actionCoroutine = null;

            QueuedAction = null;
        }

        public abstract void OnUnequipped();

        public void WatchHitbox(IHitbox hitbox)
        {
            hitbox.OnHit += OnHit;
        }

        public IEnumerator PerformQueuedAction()
        {
            if (QueuedAction != null)
            {
                yield return QueuedAction();
                QueuedAction = null;
            }
        }

        void OnHit(Entity hitEntity, int damage)
        {
            Emitter.Emit(BasicWeaponEventTypes.Hit, damage);
        }
    }

    public enum BasicWeaponEventTypes
    {
        Hit
    }
}
