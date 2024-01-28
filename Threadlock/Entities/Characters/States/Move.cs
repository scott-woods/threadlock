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

namespace Threadlock.Entities.Characters.States
{
    public class Move : State<Player>
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
            if (dir.X != 0 && dir.Y != 0)
            {
                Debug.Log("Diagonal");
            }
            dir.Normalize();

            string animation = "";
            if (dir.Y < 0)
                animation = "RunUp";
            else if (dir.Y > 0)
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

            if (Controls.Instance.DirectionalInput.Value == Vector2.Zero)
            {
                _machine.ChangeState<Idle>();
                return;
            }
        }
    }
}
