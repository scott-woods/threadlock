using Nez;
using Nez.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Entities.Characters.Player.BasicWeapons
{
    public abstract class BasicWeapon : Component
    {
        public Emitter<BasicWeaponEventTypes, int> Emitter = new Emitter<BasicWeaponEventTypes, int>();
        public abstract bool CanMove { get; }
        protected Action CompletionCallback;

        public void BeginAttack(Action completionCallback)
        {
            CompletionCallback = completionCallback;
            StartAttack();
        }

        public abstract void OnUnequipped();

        protected abstract void StartAttack();
    }

    public enum BasicWeaponEventTypes
    {
        Hit
    }
}
