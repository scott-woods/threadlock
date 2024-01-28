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
    public class Idle : State<Player>
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
            string animation = "";
            if (_velocityComponent.Direction.Y < 0)
                animation = "IdleUp";
            else if (_velocityComponent.Direction.Y > 0)
                animation = "IdleDown";
            else
                animation = "Idle";

            if (!_animator.IsAnimationActive(animation))
                _animator.Play(animation);
        }

        public override void Reason()
        {
            base.Reason();

            if (Controls.Instance.XAxisIntegerInput.Value != 0 || Controls.Instance.YAxisIntegerInput.Value != 0)
            {
                _machine.ChangeState<Move>();
                return;
            }
        }
    }
}
