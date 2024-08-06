using Microsoft.Xna.Framework;
using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Components
{
    public class DirectionComponent : Component
    {
        Vector2 _direction;

        public Vector2 GetCurrentDirection()
        {
            return _direction;
        }

        public void UpdateCurrentDirection(Vector2 direction)
        {
            direction.Normalize();

            if (float.IsNaN(direction.X) || float.IsNaN(direction.Y))
                return;

            _direction = direction;
        }
    }
}
