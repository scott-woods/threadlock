using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;

namespace Threadlock.Entities.Characters.Player.States
{
    public class StunnedState : PlayerState
    {
        bool _outOfStun = false;

        public override void Begin()
        {
            base.Begin();

            _outOfStun = false;
            _statusComponent.Emitter.AddObserver(StatusEvents.Changed, OnStatusChanged);
        }

        public override void End()
        {
            base.End();

            _statusComponent.Emitter.RemoveObserver(StatusEvents.Changed, OnStatusChanged);
        }

        public override void Reason()
        {
            base.Reason();

            if (_outOfStun)
            {
                if (TryDash())
                    return;
                if (TryMove())
                    return;
                if (TryIdle())
                    return;
            }
        }

        void OnStatusChanged(StatusPriority status)
        {
            if (status == StatusPriority.Normal)
                _outOfStun = true;
        }
    }
}
