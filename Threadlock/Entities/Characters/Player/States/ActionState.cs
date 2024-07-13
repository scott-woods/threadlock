using Nez.Tweens;
using Nez;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Entities.Characters.Player.PlayerActions;
using Threadlock.GlobalManagers;
using Threadlock.StaticData;

namespace Threadlock.Entities.Characters.Player.States
{
    public class ActionState : PlayerState
    {
        //consts
        const float _slowDuration = 1f;
        const float _speedUpDuration = .1f;
        const float _slowTimeScale = .25f;
        const float _normalTimeScale = 1f;

        ICoroutine _slowMoCoroutine;
        ICoroutine _normalSpeedCoroutine;
        ICoroutine _prepCoroutine;
        ICoroutine _executionCoroutine;
        ICoroutine _actionCoroutine;

        ActionSlot _currentActionSlot;
        PlayerAction2 _currentAction;
        bool _prepFinished = true;

        #region LIFECYCLE

        public override void OnInitialized()
        {
            base.OnInitialized();

            Game1.GameStateManager.Emitter.AddObserver(GameStateEvents.Paused, OnGamePaused);
            Game1.GameStateManager.Emitter.AddObserver(GameStateEvents.Unpaused, OnGameUnpaused);
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            //if current action isn't null and is in prep phase, check if we should cancel
            if (_currentActionSlot != null && !_prepFinished)
            {
                //check that button is still held
                if (!_currentActionSlot.Button.IsDown)
                {
                    //if another button is held, switch to that action
                    if (_actionManager.TryAction(false, out var nextAction))
                        StartAction(nextAction);
                    else
                    {
                        Reset();
                        _machine.ChangeState<Idle>();
                    }
                }
            }
        }

        public override void End()
        {
            base.End();

            //reset coroutines
            Reset();

            //if not already at normal time scale, return to normal time scale
            if (Time.TimeScale != _normalTimeScale)
                _normalSpeedCoroutine = Game1.StartCoroutine(NormalSpeedCoroutine());
        }

        #endregion

        public void StartAction(ActionSlot actionSlot)
        {
            Reset();
            _currentActionSlot = actionSlot;
            _currentAction = actionSlot.Action.Clone() as PlayerAction2;
            _actionCoroutine = Game1.StartCoroutine(StartActionCoroutine(_currentAction));
        }

        public IEnumerator StartActionCoroutine(PlayerAction2 action)
        {
            //start slow mo
            _slowMoCoroutine = Game1.StartCoroutine(SlowMoCoroutine());

            //start preparing action
            _prepFinished = false;
            _prepCoroutine = Game1.StartCoroutine(action.Prepare(Player.Instance));
            yield return _prepCoroutine;

            _prepFinished = true;

            _prepCoroutine?.Stop();
            _prepCoroutine = null;

            _slowMoCoroutine?.Stop();
            _slowMoCoroutine = null;

            //start returning to normal speed
            _normalSpeedCoroutine = Game1.StartCoroutine(NormalSpeedCoroutine());

            _executionCoroutine = Game1.StartCoroutine(action.Execute());
            yield return _executionCoroutine;
            _executionCoroutine = null;

            //release state
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

        void Reset()
        {
            //stop action
            _actionCoroutine?.Stop();
            _actionCoroutine = null;

            //stop slow mo coroutine
            _slowMoCoroutine?.Stop();
            _slowMoCoroutine = null;

            //stop normal speed coroutine
            _normalSpeedCoroutine?.Stop();
            _normalSpeedCoroutine = null;

            //stop prep
            _prepCoroutine?.Stop();
            _prepCoroutine = null;

            //stop execute
            _executionCoroutine?.Stop();
            _executionCoroutine = null;

            //reset/abort action
            _currentAction?.Reset();
            _currentAction = null;

            _currentActionSlot = null;
        }

        void OnGamePaused()
        {
            _slowMoCoroutine?.Stop();
            _slowMoCoroutine = null;
            _normalSpeedCoroutine?.Stop();
            _normalSpeedCoroutine = null;
        }

        void OnGameUnpaused()
        {

        }
    }
}
