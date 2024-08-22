using Nez;
using Nez.AI.GOAP;
using Nez.Tweens;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Actions;
using Threadlock.Helpers;

namespace Threadlock.Entities.Characters.Player.States
{
    public class PreparingActionState : PlayerState
    {
        //consts
        const float _slowDuration = 1f;
        const float _speedUpDuration = .1f;
        const float _slowTimeScale = .25f;
        const float _normalTimeScale = 1f;

        ActionManager _actionManager;

        ICoroutine _slowMoCoroutine;
        ICoroutine _normalSpeedCoroutine;
        ICoroutine _prepCoroutine;

        #region LIFECYCLE

        public override void OnInitialized()
        {
            base.OnInitialized();

            _actionManager = _context.GetComponent<ActionManager>();
        }

        public override void Begin()
        {
            base.Begin();

            //stop normal speed coroutine if in progress
            _normalSpeedCoroutine?.Stop();
            _normalSpeedCoroutine = null;

            //start slow mo
            _slowMoCoroutine = Game1.StartCoroutine(SlowMoCoroutine());

            //add observer for prep finished
            _actionManager.ActiveAction.Action.Emitter.AddObserver(PlayerActionEvents.PrepFinished, OnPrepFinished);

            //start preparing
            _prepCoroutine = Game1.StartCoroutine(_actionManager.ActiveAction.Action.Prepare(_context));
        }

        public override void Reason()
        {
            base.Reason();

            if (_actionManager.ActiveAction == null || !_actionManager.ActiveAction.Button.IsDown)
            {
                _actionManager.ActiveAction?.Action.Abort();

                if (TryMove())
                    return;
                else
                    _machine.ChangeState<Idle>();
            }
        }

        public override void End()
        {
            base.End();

            _slowMoCoroutine?.Stop();
            _slowMoCoroutine = null;

            _prepCoroutine?.Stop();
            _prepCoroutine = null;

            _actionManager.ActiveAction?.Action.Emitter.RemoveObserver(PlayerActionEvents.PrepFinished, OnPrepFinished);

            _normalSpeedCoroutine = Game1.StartCoroutine(NormalSpeedCoroutine());
        }

        #endregion

        #region OBSERVERS

        void OnPrepFinished()
        {
            _machine.ChangeState<ExecutingActionState>();
        }

        #endregion

        #region SLOW MO/NORMAL COROUTINES

        IEnumerator SlowMoCoroutine()
        {
            //get starting time scale
            var initialTimeScale = Time.TimeScale;

            var time = 0f;
            while (time < _slowDuration)
            {
                time += Time.DeltaTime;

                var currentTimeScale = Lerps.Ease(EaseType.QuartOut, initialTimeScale, _slowTimeScale, time, _slowDuration);
                Time.TimeScale = currentTimeScale;

                yield return null;
            }

            _slowMoCoroutine = null;
        }

        IEnumerator NormalSpeedCoroutine()
        {
            //get starting time scale
            var initialTimeScale = Time.TimeScale;

            var time = 0f;
            while (time < _speedUpDuration)
            {
                time += Time.DeltaTime;

                var currentTimeScale = Lerps.Ease(EaseType.QuartOut, initialTimeScale, _normalTimeScale, time, _speedUpDuration);
                Time.TimeScale = currentTimeScale;

                yield return null;
            }

            _normalSpeedCoroutine = null;
        }

        #endregion
    }
}
