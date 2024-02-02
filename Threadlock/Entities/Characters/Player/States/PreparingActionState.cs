using Nez;
using Nez.Tweens;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Entities.Characters.Player.PlayerActions;
using Threadlock.StaticData;

namespace Threadlock.Entities.Characters.Player.States
{
    public class PreparingActionState : PlayerState
    {
        //consts
        const float _slowDuration = 1f;
        const float _speedUpDuration = 1f;
        const float _slowTimeScale = .25f;
        const float _normalTimeScale = 1f;

        VirtualButton _currentButton;
        PlayerAction _currentAction;

        //coroutines
        ICoroutine _slowMoCoroutine;
        ICoroutine _normalSpeedCoroutine;

        public override void Begin()
        {
            base.Begin();

            //transition to slowmo
            _slowMoCoroutine = Core.StartCoroutine(SlowMoCoroutine());

            _currentAction.Prepare(ExecutionStartedCallback);
        }

        public override void Reason()
        {
            base.Reason();

            //if button is null or released
            if (_currentButton == null)
            {
                HandleButtonReleased();
            }
            else
            {
                if (_currentButton.IsReleased)
                {
                    HandleButtonReleased();
                }
            }
        }

        void HandleButtonReleased()
        {
            _machine.ChangeState<Idle>();
        }

        public override void End()
        {
            base.End();

            _slowMoCoroutine?.Stop();
            _slowMoCoroutine = null;
            _normalSpeedCoroutine = Core.StartCoroutine(NormalSpeedCoroutine());

            _currentAction?.Abort();
            _currentAction = null;
            _currentButton = null;
        }

        public void SetCurrentButton(VirtualButton button)
        {
            _currentButton = button;
        }

        public void SetCurrentAction(PlayerAction action)
        {
            _currentAction = action;
        }

        void ExecutionStartedCallback()
        {
            var executingState = _machine.GetState<ExecutingActionState>();
            executingState.SetCurrentAction(_currentAction);
            _machine.ChangeState<ExecutingActionState>();
        }

        IEnumerator SlowMoCoroutine()
        {
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
    }
}
