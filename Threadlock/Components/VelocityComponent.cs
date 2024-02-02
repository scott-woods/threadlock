using Microsoft.Xna.Framework;
using Nez;
using Nez.AI.BehaviorTrees;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Components
{
    public class VelocityComponent : Component
    {
        public Vector2 Direction = new Vector2(1, 0);

        //SubpixelVector2 _subPixelV2 = new SubpixelVector2();

        Mover _mover;

        public VelocityComponent(Mover mover)
        {
            _mover = mover;
        }

        public void Move(Vector2 direction, float speed)
        {
            direction.Normalize();
            Direction = direction;

            var movement = Direction * speed * Time.DeltaTime;
            _mover.CalculateMovement(ref movement, out var result);
            //_subPixelV2.Update(ref movement);

            _mover.ApplyMovement(movement);
        }
    }
}
