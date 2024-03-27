using Nez.Tweens;
using Nez;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Entities.Characters.Player.PlayerActions;

namespace Threadlock.Entities.Characters.Player.States
{
    public class ActionState : PlayerState
    {
        //consts
        const float _slowDuration = 1f;
        const float _speedUpDuration = .1f;
        const float _slowTimeScale = .25f;
        const float _normalTimeScale = 1f;

        //coroutines
        ICoroutine _slowMoCoroutine;
        ICoroutine _normalSpeedCoroutine;
        ICoroutine _actionCoroutine;
        ICoroutine _prepCoroutine;
        ICoroutine _executionCoroutine;

        //misc
        KeyValuePair<VirtualButton, PlayerAction> _currentAction;

        public override void Begin()
        {
            base.Begin();

            //if current action is somehow null, get out of action state
            if (_actionManager.CurrentAction == null)
            {
                _machine.ChangeState<Idle>();
                return;
            }

            //get current action from action manager
            _currentAction = _actionManager.CurrentAction.Value;

            //cancel normal speed coroutine if necessary
            _normalSpeedCoroutine?.Stop();
            _normalSpeedCoroutine = null;

            //start slow mo coroutine
            _slowMoCoroutine = Game1.StartCoroutine(SlowMoCoroutine());

            //start action coroutine
            _actionCoroutine = Game1.StartCoroutine(ActionCoroutine());
        }

        public override void Reason()
        {
            base.Reason();

            //if key is released
            if (_currentAction.Key.IsReleased && _currentAction.Value.State == PlayerActionState.Preparing)
            {
                //stop prep
                _prepCoroutine?.Stop();
                _prepCoroutine = null;

                //stop execute
                _executionCoroutine?.Stop();
                _executionCoroutine = null;

                //stop action
                _actionCoroutine?.Stop();
                _actionCoroutine = null;

                //reset action
                _currentAction.Value.Reset();

                if (_actionManager.CanPerformAction())
                {
                    _currentAction = _actionManager.CurrentAction.Value;
                    _actionCoroutine = Game1.StartCoroutine(ActionCoroutine());
                }
                else
                    _machine.ChangeState<Idle>();
            }
        }

        public override void End()
        {
            base.End();

            //stop slow mo coroutine
            _slowMoCoroutine?.Stop();
            _slowMoCoroutine = null;

            //if not already at normal time scale, return to normal time scale
            if (Time.TimeScale != _normalTimeScale)
                _normalSpeedCoroutine = Game1.StartCoroutine(NormalSpeedCoroutine());

            //stop prep
            _prepCoroutine?.Stop();
            _prepCoroutine = null;

            //stop execute
            _executionCoroutine?.Stop();
            _executionCoroutine = null;

            //stop action
            _actionCoroutine?.Stop();
            _actionCoroutine = null;

            //reset action
            _currentAction.Value.Reset();
        }

        IEnumerator ActionCoroutine()
        {
            _prepCoroutine = Game1.StartCoroutine(_currentAction.Value.Prepare());
            yield return _prepCoroutine;
            _prepCoroutine = null;

            //after prep finished, stop slow mo if still going
            _slowMoCoroutine?.Stop();
            _slowMoCoroutine = null;

            //start returning to normal speed
            _normalSpeedCoroutine = Game1.StartCoroutine(NormalSpeedCoroutine());

            //execute action
            _executionCoroutine = Game1.StartCoroutine(_currentAction.Value.Execute());
            yield return _executionCoroutine;
            _executionCoroutine = null;

            //return to idle state
            _machine.ChangeState<Idle>();
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
