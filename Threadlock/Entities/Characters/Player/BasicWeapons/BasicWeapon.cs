using Nez;
using Nez.Systems;
using System;
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
        public Emitter<BasicWeaponEventTypes> CompletionEmitter = new Emitter<BasicWeaponEventTypes>();
        public abstract bool CanMove { get; }

        public abstract bool Poll();

        public abstract void OnUnequipped();

        public void WatchHitbox(IHitbox hitbox)
        {
            hitbox.OnHit += OnHit;
        }

        void OnHit(Entity hitEntity, int damage)
        {
            Emitter.Emit(BasicWeaponEventTypes.Hit, damage);
        }
    }

    public enum BasicWeaponEventTypes
    {
        Hit,
        Completed
    }
}
