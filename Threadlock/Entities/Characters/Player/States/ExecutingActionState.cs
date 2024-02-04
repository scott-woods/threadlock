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

            _currentAction.OnExecutionFinished += OnExecutionFinished;
            _currentAction.Execute();
        }

        public void SetCurrentAction(PlayerAction action)
        {
            _currentAction = action;
        }

        void OnExecutionFinished()
        {
            _machine.ChangeState<Idle>();
        }
    }
}
