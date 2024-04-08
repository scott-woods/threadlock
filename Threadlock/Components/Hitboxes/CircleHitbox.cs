using Microsoft.Xna.Framework;
using Nez;
using Nez.PhysicsShapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Components.Hitboxes
{
    public class CircleHitbox : CircleCollider, IHitbox
    {
        #region IHitbox

        int _damage;
        public int Damage { get => _damage; set => _damage = value; }

        float _pushForce = 1f;
        public float PushForce { get => _pushForce; set => _pushForce = value; }

        Vector2 _direction;
        public Vector2 Direction { get => _direction; set => _direction = value; }

        string _attackId;
        public string AttackId { get => _attackId; set => _attackId = value; }

        public event Action<Entity, int> OnHit;

        public void Hit(Entity hitEntity, int damage)
        {
            OnHit?.Invoke(hitEntity, damage);
        }

        #endregion

        public CircleHitbox(int damage) : base()
        {
            Damage = damage;
        }

        public CircleHitbox(int damage, float radius) : base(radius)
        {
            Damage = damage;
        }

        public override void Initialize()
        {
            base.Initialize();

            IsTrigger = false;
        }

        public override void OnEnabled()
        {
            base.OnEnabled();

            //every time the hitbox is enabled, consider it a separate attack and generate a unique id
            AttackId = Guid.NewGuid().ToString();
        }

        public override void OnDisabled()
        {
            base.OnDisabled();

            AttackId = null;
        }
    }
}
