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
                return _collider.AbsolutePosition;
            }
        }

        Collider _collider;

        public OriginComponent(Collider collider)
        {
            _collider = collider;
        }
    }
}
