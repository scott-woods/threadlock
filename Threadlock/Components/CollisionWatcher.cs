using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Components
{
    public class CollisionWatcher : Component, IUpdatable, ITriggerListener
    {
        public event Action<Collider, Collider> OnTriggerEntered;
        public event Action<Collider, Collider> OnTriggerExited;

        HashSet<KeyValuePair<Collider, Collider>> _colliderSets = new HashSet<KeyValuePair<Collider, Collider>>();

        public CollisionWatcher()
        {
            UpdateOrder = int.MaxValue;
        }

        public void OnTriggerEnter(Collider other, Collider local)
        {
            if (_colliderSets.Any(s => s.Key == other && s.Value == local))
                return;
            else
            {
                _colliderSets.Add(new KeyValuePair<Collider, Collider>(other, local));
                OnTriggerEntered?.Invoke(other, local);
            }
        }

        public void OnTriggerExit(Collider other, Collider local)
        {
            if (_colliderSets.Any(s => s.Key == other && s.Value == local))
                return;
            else
            {
                _colliderSets.Add(new KeyValuePair<Collider, Collider>(other, local));
                OnTriggerExited?.Invoke(other, local);
            }
        }

        public void Update()
        {
            _colliderSets.Clear();
        }
    }
}
