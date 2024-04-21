using Microsoft.Xna.Framework;
using Nez;
using Nez.AI.FSM;
using Nez.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Components.TiledComponents;
using Threadlock.DebugTools;
using Threadlock.Entities.Characters.Player.BasicWeapons;
using Threadlock.Entities.Characters.Player.PlayerActions;
using Threadlock.SaveData;
using Threadlock.StaticData;

namespace Threadlock.Entities.Characters.Player.States
{
    public abstract class PlayerState : State<Player>
    {
        const float _checkRadius = 20f;
        const float _checkCooldown = 1f;

        List<Func<bool>> _exitConditions
        {
            get
            {
                var result = new List<Func<bool>>();

                switch (this)
                {
                    case Idle idle:
                        return new List<Func<bool>>() { TryOpenOverview, TryInteract, TryAction, TryBasicAttack, TryMove };
                    case Move move:
                        return new List<Func<bool>>() { TryOpenOverview, TryInteract, TryAction, TryBasicAttack, TryDash, TryIdle };
                    default:
                        return new List<Func<bool>>();
                }
            }
        }

        protected StatusComponent _statusComponent;
        protected ApComponent _apComponent;
        protected ActionManager _actionManager;

        bool _isCheckOnCooldown = false;

        public override void OnInitialized()
        {
            base.OnInitialized();

            if (_context.TryGetComponent<StatusComponent>(out var statusComponent))
                _statusComponent = statusComponent;

            if (_context.TryGetComponent<ApComponent>(out var apComponent))
                _apComponent = apComponent;

            if (_context.TryGetComponent<ActionManager>(out var actionManager))
                _actionManager = actionManager;

            Game1.UIManager.Emitter.AddObserver(GlobalManagers.UIEvents.DialogueStarted, OnDialogueStarted);
            Game1.UIManager.Emitter.AddObserver(GlobalManagers.UIEvents.DialogueEnded, OnDialogueEnded);
        }

        void OnDialogueStarted()
        {
            _machine.ChangeState<CutsceneState>();
        }

        void OnDialogueEnded()
        {
            _isCheckOnCooldown = true;
            Game1.Schedule(_checkCooldown, timer =>
            {
                _isCheckOnCooldown = false;
            });
            _machine.ChangeState<Idle>();
        }

        public override void Update(float deltaTime)
        {

        }

        public override void Reason()
        {
            base.Reason();

            if (Controls.Instance.Pause.IsPressed)
                Game1.GameStateManager.Pause();

            if (_statusComponent != null)
            {
                if ((int)_statusComponent.CurrentStatusPriority > (int)StatusPriority.Normal && _machine.CurrentState.GetType() != typeof(StunnedState))
                {
                    _machine.ChangeState<StunnedState>();
                }
            }

            foreach (var condition in _exitConditions)
            {
                if (condition())
                    break;
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
            if (_context.TryGetComponent<BasicWeapon>(out var weapon))
            {
                if (weapon.Poll())
                {
                    _machine.ChangeState<BasicAttackState>();
                    return true;
                }
            }

            return false;
        }

        public bool TryAction()
        {
            if (_actionManager.TryAction(out var actionSlot))
            {
                _machine.ChangeState<ActionState>();
                var actionState = _machine.GetState<ActionState>();
                actionState.StartAction(actionSlot);
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

        public bool TryInteract()
        {
            if (!_isCheckOnCooldown && Controls.Instance.Check.IsPressed)
            {
                var basePos = _context.Position;
                if (_context.TryGetComponent<OriginComponent>(out var oc))
                    basePos = oc.Origin;

                var dir = Vector2.Zero;
                if (_context.TryGetComponent<VelocityComponent>(out var vc))
                    dir = vc.LastNonZeroDirection;

                var checkEnd = basePos + (dir * _checkRadius);

                var raycast = Physics.Linecast(basePos, checkEnd, 1 << PhysicsLayers.Trigger);
                if (raycast.Collider != null)
                {
                    if (raycast.Collider.Entity.TryGetComponent<Trigger>(out var trigger))
                    {
                        Game1.StartCoroutine(trigger.HandleTriggered());
                        return true;
                    }
                }
            }

            return false;
        }

        public bool TryOpenOverview()
        {
            if (Controls.Instance.ShowStats.IsPressed)
            {
                _machine.ChangeState<ViewingStatsState>();
                return true;
            }

            return false;
        }
    }
}
