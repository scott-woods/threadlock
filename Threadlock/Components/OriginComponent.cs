using Microsoft.Xna.Framework;
using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Components
{
    public class OriginComponent : Component
    {
        public Vector2 Origin
        {
            get
            {
                return _collider != null ? _collider.AbsolutePosition : Entity.Position + _offset;
            }
        }

        Collider _collider;
        Vector2 _offset;

        public OriginComponent(Collider collider)
        {
            _collider = collider;
        }

        public OriginComponent(Vector2 offset)
        {
            _offset = offset;
        }

        public void UpdateOffset(Vector2 offset)
        {
            _offset = offset;
        }
    }
}
