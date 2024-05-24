using Microsoft.Xna.Framework;
using Nez;
using Nez.AI.FSM;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Entities.Characters.Player.BasicWeapons;
using Threadlock.SaveData;

namespace Threadlock.Entities.Characters.Player.States
{
    public class BasicAttackState : PlayerState
    {
        BasicWeapon _basicWeapon;
        ICoroutine _performAttackCoroutine;

        public override void OnInitialized()
        {
            base.OnInitialized();

            _basicWeapon = _context.GetComponent<BasicWeapon>();

            _context.OnWeaponChanged += OnWeaponChanged;
        }

        public override void Begin()
        {
            base.Begin();

            _performAttackCoroutine = Game1.StartCoroutine(PerformAttack());
        }

        public override void Update(float deltaTime)
        {
            if (_basicWeapon.CanMove)
            {
                if (Controls.Instance.XAxisIntegerInput.Value != 0 || Controls.Instance.YAxisIntegerInput.Value != 0)
                    _context.Run();
                if (Controls.Instance.DirectionalInput.Value == Vector2.Zero)
                    _context.Idle();
            }
        }

        public override void End()
        {
            base.End();

            _performAttackCoroutine?.Stop();
            _performAttackCoroutine = null;

            _basicWeapon.Reset();
        }

        IEnumerator PerformAttack()
        {
            yield return _basicWeapon.PerformQueuedAction();

            //exit attack state
            if (TryMove())
                yield break;
            else
                _machine.ChangeState<Idle>();
        }

        void OnWeaponChanged(BasicWeapon weapon)
        {
            if (_basicWeapon != null && _basicWeapon != weapon)
            {
                _basicWeapon.Reset();
                _basicWeapon = null;
            }

            _basicWeapon = weapon;
        }
    }
}
