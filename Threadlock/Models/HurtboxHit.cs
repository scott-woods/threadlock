using Microsoft.Xna.Framework;
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
        public int Damage;
        public Vector2 Direction = Vector2.Zero;
        public float PushForce = 0f;

        public HurtboxHit(CollisionResult collisionResult, IHitbox hitbox)
        {
            var dir = hitbox.Direction != Vector2.Zero ? hitbox.Direction : -collisionResult.Normal;
            if (dir != Vector2.Zero)
                dir.Normalize();

            Direction = dir;

            Damage = hitbox.Damage;
            PushForce = hitbox.PushForce;
        }

        public HurtboxHit(int damage)
        {
            Damage = damage;
        }
    }
}
