//using Nez;
//using Nez.Tweens;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Threadlock.Entities.Characters.Player.PlayerActions;
//using Threadlock.StaticData;

//namespace Threadlock.Entities.Characters.Player.States
//{
//    public class PreparingActionState : PlayerState
//    {
//        //consts
//        const float _slowDuration = 1f;
//        const float _speedUpDuration = .5f;
//        const float _slowTimeScale = .25f;
//        const float _normalTimeScale = 1f;

//        VirtualButton _currentButton;
//        PlayerAction _currentAction;

//        //coroutines
//        ICoroutine _slowMoCoroutine;
//        ICoroutine _normalSpeedCoroutine;

//        #region LIFECYCLE

//        public override void Begin()
//        {
//            base.Begin();

//            //stop previous coroutines if necessary
//            _normalSpeedCoroutine?.Stop();
//            _normalSpeedCoroutine = null;

//            //transition to slowmo
//            _slowMoCoroutine = Game1.StartCoroutine(SlowMoCoroutine());

//            //start preparing action
//            _currentAction.OnPreparationFinished += OnPreparationFinished;
//            _currentAction.Prepare();
//        }

//        public override void Reason()
//        {
//            base.Reason();

//            //if button is null or released
//            if (_currentButton == null)
//            {
//                HandleButtonReleased();
//            }
//            else
//            {
//                if (_currentButton.IsReleased)
//                {
//                    HandleButtonReleased();
//                }
//            }
//        }

//        public override void End()
//        {
//            base.End();

//            _currentAction.OnPreparationFinished -= OnPreparationFinished;

//            _slowMoCoroutine?.Stop();
//            _slowMoCoroutine = null;
//            _normalSpeedCoroutine = Game1.StartCoroutine(NormalSpeedCoroutine());

//            _currentAction?.Abort();
//            _currentAction = null;
//            _currentButton = null;
//        }

//        #endregion

//        void HandleButtonReleased()
//        {
//            _machine.ChangeState<Idle>();
//        }

//        public void SetCurrentButton(VirtualButton button)
//        {
//            _currentButton = button;
//        }

//        public void SetCurrentAction(PlayerAction action)
//        {
//            _currentAction = action;
//        }

//        void OnPreparationFinished()
//        {
//            var executingState = _machine.GetState<ExecutingActionState>();
//            executingState.SetCurrentAction(_currentAction);
//            _machine.ChangeState<ExecutingActionState>();
//        }

//        IEnumerator SlowMoCoroutine()
//        {
//            var initialTimeScale = Time.TimeScale;

//            var time = 0f;
//            while (time < _slowDuration)
//            {
//                time += Time.DeltaTime;

//                var currentTimeScale = Lerps.Ease(EaseType.QuartOut, initialTimeScale, _slowTimeScale, time, _slowDuration);
//                Time.TimeScale = currentTimeScale;

//                yield return null;
//            }

//            _slowMoCoroutine = null;
//        }

//        IEnumerator NormalSpeedCoroutine()
//        {
//            var initialTimeScale = Time.TimeScale;

//            var time = 0f;
//            while (time < _speedUpDuration)
//            {
//                time += Time.DeltaTime;

//                var currentTimeScale = Lerps.Ease(EaseType.QuartOut, initialTimeScale, _normalTimeScale, time, _speedUpDuration);
//                Time.TimeScale = currentTimeScale;

//                yield return null;
//            }

//            _normalSpeedCoroutine = null;
//        }
//    }
//}
