using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Entities.Characters.Player.States
{
    public class StunnedState : PlayerState
    {
        public override void Reason()
        {
            base.Reason();

            if (_statusComponent.CurrentStatusPriority == Components.StatusPriority.Normal)
            {
                if (TryDash())
                    return;
                if (TryMove())
                    return;
                if (TryIdle())
                    return;
            }
        }
    }
}
