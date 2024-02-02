using Nez;
using Nez.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components.Hitboxes;
using Threadlock.Models;

namespace Threadlock.Components
{
    public class Hurtbox : Component, IUpdatable
    {
        public Emitter<HurtboxEventTypes, HurtboxHit> Emitter = new Emitter<HurtboxEventTypes, HurtboxHit>();

        Collider _collider;

        public Hurtbox(Collider collider)
        {
            _collider = collider;
        }

        public void Update()
        {
            if (_collider.CollidesWithAny(out CollisionResult result))
            {
                var hitbox = result.Collider as IHitbox;
                Emitter.Emit(HurtboxEventTypes.Hit, new HurtboxHit(result, hitbox));
            }
        }
    }

    public enum HurtboxEventTypes
    {
        Hit
    }
}
