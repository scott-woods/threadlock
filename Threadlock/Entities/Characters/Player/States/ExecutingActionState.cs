using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Helpers;

namespace Threadlock.Entities.Characters.Player.States
{
    public class ExecutingActionState : PlayerState
    {
        ActionManager _actionManager;

        ICoroutine _executionCoroutine;

        public override void OnInitialized()
        {
            base.OnInitialized();

            _actionManager = _context.GetComponent<ActionManager>();
        }

        public override void Begin()
        {
            base.Begin();

            _executionCoroutine = Game1.StartCoroutine(CoroutineHelper.CoroutineWrapper(_actionManager.ActiveAction.Action.Execute(), OnExecutionFinished));
        }

        public override void End()
        {
            base.End();

            _executionCoroutine?.Stop();
            _executionCoroutine = null;
        }

        void OnExecutionFinished()
        {
            if (TryMove())
                return;
            else
                _machine.ChangeState<Idle>();
        }
    }
}
