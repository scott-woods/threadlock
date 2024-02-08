using Microsoft.Xna.Framework;
using Nez.AI.FSM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.DebugTools;
using Threadlock.Entities.Characters.Player.PlayerActions;
using Threadlock.SaveData;
using Threadlock.StaticData;

namespace Threadlock.Entities.Characters.Player.States
{
    public class PlayerState : State<Player>
    {
        protected StatusComponent _statusComponent;
        protected ApComponent _apComponent;
        protected ActionManager _actionManager;

        public override void OnInitialized()
        {
            base.OnInitialized();

            if (_context.TryGetComponent<StatusComponent>(out var statusComponent))
                _statusComponent = statusComponent;

            if (_context.TryGetComponent<ApComponent>(out var apComponent))
                _apComponent = apComponent;

            if (_context.TryGetComponent<ActionManager>(out var actionManager))
                _actionManager = actionManager;
        }

        public override void Update(float deltaTime)
        {
        }

        public override void Reason()
        {
            base.Reason();

            if (_statusComponent != null)
            {
                if ((int)_statusComponent.CurrentStatusPriority > (int)StatusPriority.Normal && _machine.CurrentState.GetType() != typeof(StunnedState))
                {
                    _machine.ChangeState<StunnedState>();
                }
            }
        }

        public bool TryMove()
        {
            if (Controls.Instance.XAxisIntegerInput.Value != 0 || Controls.Instance.YAxisIntegerInput.Value != 0)
            {
                _machine.ChangeState<Move>();
                return true;
            }

            return false;
        }

        public bool TryIdle()
        {
            if (Controls.Instance.DirectionalInput.Value == Vector2.Zero)
            {
                _machine.ChangeState<Idle>();
                return true;
            }

            return false;
        }

        public bool TryBasicAttack()
        {
            if (Controls.Instance.Melee.IsPressed)
            {
                _machine.ChangeState<BasicAttackState>();
                return true;
            }

            return false;
        }

        public bool TryAction()
        {
            if (_actionManager.CanPerformAction())
            {
                _machine.ChangeState<ActionState>();
                return true;
            }

            return false;
        }

        public bool TryDash()
        {
            if (Controls.Instance.Dodge.IsPressed)
            {
                _machine.ChangeState<DashState>();
                return true;
            }

            return false;
        }
    }
}
