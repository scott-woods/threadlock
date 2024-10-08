﻿using Microsoft.Xna.Framework;
using Nez.AI.FSM;
using System;
using System.Collections.Generic;
using Threadlock.Components;
using Threadlock.Entities.Characters.Player.BasicWeapons;
using Threadlock.SaveData;
using Threadlock.StaticData;

namespace Threadlock.Entities.Characters.Player.States
{
    public abstract class PlayerState : State<Player>
    {
        const float _checkCooldown = 1f;

        List<Func<bool>> _exitConditions
        {
            get
            {
                var result = new List<Func<bool>>();

                switch (this)
                {
                    case Idle idle:
                        return new List<Func<bool>>() { TryOpenOverview, TryInteract, TryAction, TryBasicAttack, TryMove, TrySequencedAttack };
                    case Move move:
                        return new List<Func<bool>>() { TryOpenOverview, TryInteract, TryAction, TryBasicAttack, TryDash, TryIdle, TrySequencedAttack };
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
        }

        public override void Begin()
        {
            base.Begin();

            Game1.UIManager.Emitter.AddObserver(GlobalManagers.UIEvents.DialogueStarted, OnDialogueStarted);
            Game1.UIManager.Emitter.AddObserver(GlobalManagers.UIEvents.DialogueEnded, OnDialogueEnded);
            Game1.UIManager.Emitter.AddObserver(GlobalManagers.UIEvents.MenuOpened, OnMenuOpened);
            Game1.UIManager.Emitter.AddObserver(GlobalManagers.UIEvents.MenuClosed, OnMenuClosed);

            _statusComponent.Emitter.AddObserver(StatusEvents.Changed, OnStatusChanged);
        }

        public override void Update(float deltaTime)
        {
            
        }

        public override void End()
        {
            base.End();

            Game1.UIManager.Emitter.RemoveObserver(GlobalManagers.UIEvents.DialogueStarted, OnDialogueStarted);
            Game1.UIManager.Emitter.RemoveObserver(GlobalManagers.UIEvents.DialogueEnded, OnDialogueEnded);
            Game1.UIManager.Emitter.RemoveObserver(GlobalManagers.UIEvents.MenuOpened, OnMenuOpened);
            Game1.UIManager.Emitter.RemoveObserver(GlobalManagers.UIEvents.MenuClosed, OnMenuClosed);

            _statusComponent.Emitter.RemoveObserver(StatusEvents.Changed, OnStatusChanged);
        }

        void OnStatusChanged(StatusPriority status)
        {
            if (status == StatusPriority.Normal)
            {
                if (TryMove())
                    return;
                if (TryIdle())
                    return;
            }
            if (status == StatusPriority.Stunned)
                _machine.ChangeState<StunnedState>();
            else if (status == StatusPriority.Death)
                _machine.ChangeState<DyingState>();
        }

        void OnMenuOpened()
        {
            _machine.ChangeState<CutsceneState>();
        }

        void OnMenuClosed()
        {
            _machine.ChangeState<Idle>();
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

        public override void Reason()
        {
            base.Reason();

            if (Controls.Instance.Pause.IsPressed)
                Game1.GameStateManager.Pause();

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
            if (_context.TryGetComponent<PlayerWeapon>(out var playerWeapon))
            {
                if (playerWeapon.Poll())
                {
                    _machine.ChangeState<BasicAttackState>();
                    return true;
                }
            }

            return false;

            //if (_context.TryGetComponent<BasicWeapon>(out var weapon))
            //{
            //    if (weapon.Poll())
            //    {
            //        _machine.ChangeState<BasicAttackState>();
            //        return true;
            //    }
            //}

            //return false;
        }

        public bool TryAction()
        {
            if (_actionManager.TryAction(true, out var actionSlot))
            {
                _machine.ChangeState<PreparingActionState>();
                return true;

                //_machine.ChangeState<ActionState>();
                //var actionState = _machine.GetState<ActionState>();
                //actionState.StartAction(actionSlot);
                //return true;
            }

            return false;
        }

        public bool TrySequencedAttack()
        {
            if (Controls.Instance.Special.IsPressed)
            {
                _machine.ChangeState<SequencedAttackState>();
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
            if (Controls.Instance.Check.IsPressed && _context.TryGetComponent<InteractableChecker>(out var checker))
            {
                return checker.TryCheck();
            }

            return false;
        }

        public bool TryOpenOverview()
        {
            //if (Controls.Instance.ShowStats.IsPressed)
            //{
            //    Game1.StartCoroutine(Game1.UIManager.ShowMenu(new CharacterOverview()));
            //    return true;
            //}

            return false;
        }
    }
}
