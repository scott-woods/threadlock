using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Entities.Characters.Player.PlayerActions;

namespace Threadlock.Entities.Characters.Player.States
{
    public class ExecutingActionState : PlayerState
    {
        PlayerAction _currentAction;

        public override void Begin()
        {
            base.Begin();

            _currentAction.Execute(ExecutionCompletedCallback);
        }

        public void SetCurrentAction(PlayerAction action)
        {
            _currentAction = action;
        }

        void ExecutionCompletedCallback()
        {
            _machine.ChangeState<Idle>();
        }
    }
}
