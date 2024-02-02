using Microsoft.Xna.Framework;
using Nez;
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
    public class Move : PlayerState
    {
        SpriteAnimator _animator;
        VelocityComponent _velocityComponent;

        public override void OnInitialized()
        {
            base.OnInitialized();

            _animator = _context.GetComponent<SpriteAnimator>();
            _velocityComponent = _context.GetComponent<VelocityComponent>();
        }

        public override void Update(float deltaTime)
        {
            var dir = Controls.Instance.DirectionalInput.Value;
            dir.Normalize();

            string animation = "";
            if (dir.Y < 0 && Math.Abs(dir.X) < .1f)
                animation = "RunUp";
            else if (dir.Y > 0 && Math.Abs(dir.X) < .1f)
                animation = "RunDown";
            else
                animation = "Run";

            if (!_animator.IsAnimationActive(animation))
                _animator.Play(animation);

            _velocityComponent.Move(dir, _context.MoveSpeed);
        }

        public override void Reason()
        {
            base.Reason();

            if (TryAction())
                return;
            if (TryBasicAttack())
                return;
            if (TryDash())
                return;
            if (TryIdle())
                return;
        }
    }
}
