using Nez;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Actions;
using Threadlock.SaveData;
using Threadlock.StaticData;
using static Nez.Content;

namespace Threadlock.Entities.Characters.Player
{
    public class PlayerWeapon : Component
    {
        public string Name;

        public List<PlayerWeaponAttack> PrimaryAttack = new List<PlayerWeaponAttack>();
        public List<PlayerWeaponAttack> SecondaryAttack = new List<PlayerWeaponAttack>();

        public float PostBufferTime;

        PlayerWeaponAttack _queuedAttack;
        ICoroutine _executionCoroutine;
        int _nextIndex;
        bool _isInputBuffered = false;
        List<PlayerWeaponAttack> _activeList;
        ITimer _bufferTimer;
        PlayerWeaponData _data;

        public PlayerWeapon(PlayerWeaponData data)
        {
            Name = data.Name;
            PostBufferTime = data.PostBufferTime;

            _data = data;
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            foreach (var attackName in _data.PrimaryAttack)
            {
                if (PlayerWeaponAttacks.TryCreatePlayerWeaponAttack(attackName, Entity, out var attack))
                    PrimaryAttack.Add(attack);
            }

            foreach (var attackName in _data.SecondaryAttack)
            {
                if (PlayerWeaponAttacks.TryCreatePlayerWeaponAttack(attackName, Entity, out var attack))
                    SecondaryAttack.Add(attack);
            }
        }

        public bool Poll()
        {
            if (PrimaryAttack != null && Controls.Instance.Melee.IsPressed)
            {
                _activeList = PrimaryAttack;
                //_queuedAttack = PrimaryAttack[_nextIndex];
                return true;
            }
            else if (SecondaryAttack != null && Controls.Instance.AltAttack.IsPressed)
            {
                _activeList = SecondaryAttack;
                //_queuedAttack = SecondaryAttack.First();
                return true;
            }

            return false;
        }

        public IEnumerator Execute()
        {
            //stop the buffer timer if it was running
            _bufferTimer?.Stop();

            //loop through combo as long as input is buffered
            _isInputBuffered = true;
            while (_isInputBuffered)
            {
                //set buffer to false
                _isInputBuffered = false;

                //get the next attack in the active list
                _queuedAttack = _activeList[_nextIndex];

                //start the attack execution
                _executionCoroutine = Core.StartCoroutine(_queuedAttack.Execute());

                //watch for input that would extend the combo
                if (_queuedAttack.ComboInputDelay.HasValue)
                    Core.StartCoroutine(InputWatcher(_queuedAttack.ComboInputDelay.Value));

                //wait for attack execution (may be stopped early by input watcher)
                yield return _executionCoroutine;

                //null out execution coroutine
                _executionCoroutine = null;

                //increment index, or set to 0 if at end of combo
                _nextIndex++;
                if (_nextIndex >= _activeList.Count)
                {
                    _nextIndex = 0;
                    break;
                }
            }

            //start buffer timer. this holds on to our current place in the combo for a short time so it can be continued
            if (PostBufferTime > 0)
                _bufferTimer = Core.Schedule(PostBufferTime, timer => _nextIndex = 0);
        }

        IEnumerator InputWatcher(float comboInputDelay)
        {
            var timer = 0f;

            while (_executionCoroutine != null)
            {
                //increment timer
                timer += Time.DeltaTime;

                //try to get input buffer if haven't already and combo input delay has been reached
                if (!_isInputBuffered && timer >= comboInputDelay)
                {
                    if (_activeList == PrimaryAttack && Controls.Instance.Melee.IsPressed
                        || _activeList == SecondaryAttack && Controls.Instance.AltAttack.IsPressed)
                    {
                        _isInputBuffered = true;
                    }
                }

                //if input is buffered and we've reached the time that the current execution can be overriden, do that
                if (_isInputBuffered && _queuedAttack.ComboStartTime.HasValue && timer >= _queuedAttack.ComboStartTime)
                {
                    _executionCoroutine?.Stop();
                    _executionCoroutine = null;
                    yield break;
                }

                yield return null;
            }
        }
    }

    public class PlayerWeaponData
    {
        public string Name;

        public List<string> PrimaryAttack;
        public List<string> SecondaryAttack;

        public float PostBufferTime;
    }
}
