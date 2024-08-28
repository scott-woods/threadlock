using Nez.Sprites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Helpers;
using Threadlock.SaveData;

namespace Threadlock.Entities.Characters.Player.States.Factory
{
    public class FactoryMove : PlayerState
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
            base.Update(deltaTime);

            var dir = Controls.Instance.DirectionalInput.Value;
            dir.Normalize();

            _velocityComponent.Move(dir, 135f, false, true);

            AnimatedSpriteHelper.PlayAnimation(_animator, "Player_Run");
        }

        public override void Reason()
        {
            base.Reason();

            if (Controls.Instance.XAxisIntegerInput.Value == 0 && Controls.Instance.YAxisIntegerInput.Value == 0)
                _machine.ChangeState<FactoryIdle>();
        }
    }
}
