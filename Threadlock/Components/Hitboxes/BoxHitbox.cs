using Microsoft.Xna.Framework;
using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Components.Hitboxes
{
    public class BoxHitbox : BoxCollider, IHitbox
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

        /// <summary>
		/// creates a BoxCollider and uses the x/y components as the localOffset
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		/// <param name="width">Width.</param>
		/// <param name="height">Height.</param>
		public BoxHitbox(int damage, float x, float y, float width, float height) : base(x, y, width, height)
        {
            Damage = damage;
        }

        public BoxHitbox(int damage, float width, float height) : base(width, height)
        {
            Damage = damage;
        }

        /// <summary>
        /// creates a BoxCollider and uses the x/y components of the Rect as the localOffset
        /// </summary>
        /// <param name="rect">Rect.</param>
        public BoxHitbox(int damage, Rectangle rect) : base(rect)
        {
            Damage = damage;
        }

        public override void Initialize()
        {
            base.Initialize();

            IsTrigger = true;
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
