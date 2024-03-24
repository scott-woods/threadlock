using Microsoft.Xna.Framework;
using Nez.AI.FSM;
using Nez.Sprites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.StaticData;

namespace Threadlock.Entities.Characters.Player.States
{
    public class Idle : PlayerState
    {
        SpriteAnimator _animator;
        VelocityComponent _velocityComponent;

        public override void OnInitialized()
        {
            base.OnInitialized();

            _animator = _context.GetComponent<SpriteAnimator>();
            _velocityComponent = _context.GetComponent<VelocityComponent>();
        }

        public override void Begin()
        {
            base.Begin();

            var dir = _velocityComponent.Direction;

            string animation = "";
            if (dir.Y < 0 && Math.Abs(dir.X) < .75f)
                animation = "IdleUp";
            else if (dir.Y > 0 && Math.Abs(dir.X) < .75f)
                animation = "IdleDown";
            else
                animation = "Idle";

            if (!_animator.IsAnimationActive(animation))
                _animator.Play(animation);

            _velocityComponent.Direction = Vector2.Zero;
        }

        public override void Reason()
        {
            base.Reason();

            TryInteract();

            if (TryAction())
                return;
            if (TryBasicAttack())
                return;
            if (TryMove())
                return;
        }
    }
}
