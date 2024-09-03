using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.StaticData;

namespace Threadlock.Components
{
    public class Destructable : Component, ITriggerListener
    {
        public event Action OnDestruction;

        string _sound;

        public Destructable(Collider collider, string sound)
        {
            _sound = sound;

            Flags.SetFlag(ref collider.CollidesWithLayers, PhysicsLayers.PlayerHitbox);
        }

        public void OnTriggerEnter(Collider other, Collider local)
        {
            Game1.AudioManager.PlaySound(_sound);
            OnDestruction?.Invoke();
        }

        public void OnTriggerExit(Collider other, Collider local)
        {
            //throw new NotImplementedException();
        }
    }
}
