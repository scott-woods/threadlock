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
        public Vector2 LastNonZeroDirection = new Vector2(1, 0);

        //SubpixelVector2 _subPixelV2 = new SubpixelVector2();

        Mover _mover;

        public VelocityComponent(Mover mover)
        {
            _mover = mover;
        }

        public void Move(Vector2 direction, float speed)
        {
            //can't move in no direction
            if (direction == Vector2.Zero)
                return;

            //normalize direction
            direction.Normalize();

            //if normalizing resulted in NaN somehow, break here
            if (float.IsNaN(direction.X) || float.IsNaN(direction.Y))
                return;

            Direction = direction;
            if (direction != Vector2.Zero)
                LastNonZeroDirection = direction;

            var movement = Direction * speed * Time.DeltaTime;
            _mover.CalculateMovement(ref movement, out var result);
            //_subPixelV2.Update(ref movement);

            _mover.ApplyMovement(movement);
        }
    }
}
