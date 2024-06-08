using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components.EnemyActions;
using Threadlock.Components.Hitboxes;
using Threadlock.StaticData;

namespace Threadlock.Entities
{
    public class Projectile2 : Entity
    {
        Vector2 _direction;

        SpriteAnimator _animator;
        ProjectileMover _mover;
        SpriteTrail _trail;
        Collider _hitbox;

        public Projectile2(string name, float speed, Vector2 direction, HitboxConfig hitboxConfig, bool destroyOnWall, int physicsLayer, int collidesWithLayer)
        {
            if (hitboxConfig.Size != Vector2.Zero)
                _hitbox = new BoxHitbox(hitboxConfig.Damage, hitboxConfig.Size.X, hitboxConfig.Size.Y);
            else if (hitboxConfig.Radius != 0)
                _hitbox = new CircleHitbox(hitboxConfig.Damage, hitboxConfig.Radius);
            else if (hitboxConfig.Points != null)
                _hitbox = new PolygonHitbox(hitboxConfig.Damage, hitboxConfig.Points.ToArray());

            _hitbox.PhysicsLayer = 0;
            Flags.SetFlag(ref _hitbox.PhysicsLayer, physicsLayer);
            _hitbox.CollidesWithLayers = 0;
            Flags.SetFlag(ref _hitbox.CollidesWithLayers, collidesWithLayer);
            if (destroyOnWall)
                Flags.SetFlag(ref _hitbox.CollidesWithLayers, PhysicsLayers.Environment);
        }
    }
}
