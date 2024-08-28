using Nez.Sprites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Entities.Characters.Player.States.Shared;
using Threadlock.Helpers;
using Threadlock.SaveData;

namespace Threadlock.Entities.Characters.Player.States.Factory
{
    public class FactoryIdle : PlayerState
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

            AnimatedSpriteHelper.PlayAnimation(_animator, "Player_Idle");
        }

        public override void Reason()
        {
            base.Reason();

            if (Controls.Instance.XAxisIntegerInput.Value != 0 || Controls.Instance.YAxisIntegerInput.Value != 0)
                _machine.ChangeState<FactoryMove>();
        }
    }
}
