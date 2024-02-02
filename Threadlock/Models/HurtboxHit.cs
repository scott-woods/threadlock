using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components.Hitboxes;

namespace Threadlock.Models
{
    public class HurtboxHit
    {
        public CollisionResult CollisionResult { get; set; }
        public IHitbox Hitbox { get; set; }

        public HurtboxHit(CollisionResult collisionResult, IHitbox hitbox)
        {
            CollisionResult = collisionResult;
            Hitbox = hitbox;
        }
    }
}
