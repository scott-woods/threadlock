﻿using Microsoft.Xna.Framework;
using Nez.AI.FSM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.SaveData;
using Threadlock.StaticData;

namespace Threadlock.Entities.Characters.Player.States
{
    public class PlayerState : State<Player>
    {
        protected StatusComponent _statusComponent;

        public override void OnInitialized()
        {
            base.OnInitialized();

            if (_context.TryGetComponent<StatusComponent>(out var statusComponent))
                _statusComponent = statusComponent;
        }

        public override void Update(float deltaTime)
        {
        }

        public override void Reason()
        {
            base.Reason();

            if (_statusComponent != null)
            {
                if (_statusComponent.CurrentStatusPriority == StatusPriority.Stunned && _machine.CurrentState.GetType() != typeof(StunnedState))
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
            if (Player.Instance.OffensiveAction1 != null && Controls.Instance.Action1.IsPressed)
            {
                var prepState = _machine.GetState<PreparingActionState>();
                prepState.SetCurrentButton(Controls.Instance.Action1);
                prepState.SetCurrentAction(Player.Instance.OffensiveAction1);
                _machine.ChangeState<PreparingActionState>();
                return true;
            }
            else if (Player.Instance.SupportAction != null && Controls.Instance.SupportAction.IsPressed)
            {
                var prepState = _machine.GetState<PreparingActionState>();
                prepState.SetCurrentButton(Controls.Instance.SupportAction);
                prepState.SetCurrentAction(Player.Instance.SupportAction);
                _machine.ChangeState<PreparingActionState>();
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
