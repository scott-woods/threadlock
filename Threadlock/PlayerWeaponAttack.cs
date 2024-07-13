using Nez;
using Nez.Persistence;
using Nez.Sprites;
using System;
using System.Collections;
using System.Collections.Generic;
using Threadlock.Entities.Characters.Player;
using Threadlock.Helpers;
using Threadlock.StaticData;

namespace Threadlock
{
    public class PlayerWeaponAttack : BasicAction, ICloneable
    {
        public float ComboInputDelay;
        public float ComboWaitTime;
        public string WaitAnimation;
        public bool ShouldComboWait = true;

        [JsonExclude]
        public VirtualButton Button;

        ICoroutine _currentExecutionCoroutine;
        ICoroutine _watchForInputCoroutine;
        bool _canContinue;

        IEnumerator WatchForInput()
        {
            var timer = 0f;
            while (true)
            {
                timer += Time.DeltaTime;
                if (timer >= ComboInputDelay && Button.IsPressed)
                {
                    if (!ShouldComboWait)
                    {
                        _currentExecutionCoroutine?.Stop();
                        _currentExecutionCoroutine = null;
                    }
                    _canContinue = true;
                    yield break;
                }

                yield return null;
            }
        }

        IEnumerator DoNextAction(int index)
        {
            _canContinue = false;

            var actionName = ComboActions[index];
            if (PlayerWeaponAttacks.TryCreatePlayerWeaponAttack(actionName, Context, out var action))
            {
                if (ComboActions.Count >= index + 2)
                    _watchForInputCoroutine = Game1.StartCoroutine(WatchForInput());

                _currentExecutionCoroutine = Game1.StartCoroutine(action.Execute());
                yield return _currentExecutionCoroutine;

                if (ComboWaitTime > 0)
                {
                    if (!_canContinue)
                    {
                        var animator = Context.GetComponent<SpriteAnimator>();
                        AnimatedSpriteHelper.PlayAnimation(ref animator, WaitAnimation);
                    }
                    
                    var timer = 0f;
                    while (!_canContinue && timer < ComboWaitTime)
                    {
                        timer += Time.DeltaTime;
                        yield return null;
                    }
                }
                

                _watchForInputCoroutine?.Stop();
                _watchForInputCoroutine = null;

                if (_canContinue)
                {
                    yield return DoNextAction(index + 1);
                }
            }
        }

        #region BASIC ACTION

        public override IEnumerator HandleCombo()
        {
            //if no combo actions, break
            if (ComboActions == null || ComboActions.Count == 0)
                yield break;

            yield return DoNextAction(0);
        }

        protected override TargetingInfo GetTargetingInfo()
        {
            var player = Context as Player;

            return new TargetingInfo()
            {
                Direction = player.GetFacingDirection(),
            };
        }

        #endregion

        #region ICLONEABLE

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        #endregion
    }
}
