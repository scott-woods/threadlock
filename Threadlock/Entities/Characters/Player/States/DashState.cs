using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Entities.Characters.Player;

namespace Threadlock.Entities.Characters.Player.States
{
    public class DashState : PlayerState
    {
        Dash _dash;

        public override void OnInitialized()
        {
            base.OnInitialized();

            _dash = _context.GetComponent<Dash>();
        }

        public override void Begin()
        {
            base.Begin();

            //disable hurtbox
            //_context.Hurtbox.SetEnabled(false);

            _dash.ExecuteDash(OnDashCompleted);
        }

        public override void Update(float deltaTime)
        {
            //throw new NotImplementedException();
        }

        public override void End()
        {
            base.End();

            //reenable hurtbox
            //_context.Hurtbox.SetEnabled(true);

            _dash.Abort();
        }

        void OnDashCompleted()
        {
            _machine.ChangeState<Idle>();
        }
    }
}
