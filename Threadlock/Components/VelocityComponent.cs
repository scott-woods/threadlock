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
        Vector2 _lastNonZeroDirection = new Vector2(1, 0);
        public Vector2 LastNonZeroDirection
        {
            get => _lastNonZeroDirection;
            set
            {
                if (!float.IsNaN(value.X) && !float.IsNaN(value.Y))
                    _lastNonZeroDirection = value;
            }
        }

        //SubpixelVector2 _subPixelV2 = new SubpixelVector2();

        Mover _mover;
        ProjectileMover _projectileMover;

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            if (Entity.TryGetComponent<Mover>(out var mover))
                _mover = mover;

            if (Entity.TryGetComponent<ProjectileMover>(out var projectileMover))
                _projectileMover = projectileMover;
        }

        public void Move(Vector2 direction, float speed, bool isProjectile = false)
        {
            //check if movers are null
            if (!isProjectile && _mover == null)
                return;
            if (isProjectile && _projectileMover == null)
                return;

            //can't move in no direction
            if (direction == Vector2.Zero)
                return;

            //normalize direction
            direction.Normalize();

            //if normalizing resulted in NaN somehow, break here
            if (float.IsNaN(direction.X) || float.IsNaN(direction.Y))
                return;

            //update direction
            Direction = direction;
            if (direction != Vector2.Zero)
                LastNonZeroDirection = direction;

            //calculate movement and move (use the mover to calculate to make sure we don't go through walls)
            var movement = Direction * speed * Time.DeltaTime;
            _mover.CalculateMovement(ref movement, out var result);

            //apply movement
            if (isProjectile)
                _projectileMover.Move(movement);
            else
                _mover.ApplyMovement(movement);
        }
    }
}
