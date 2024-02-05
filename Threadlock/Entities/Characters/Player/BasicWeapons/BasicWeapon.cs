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
    }

    public enum BasicWeaponEventTypes
    {
        Hit
    }
}
