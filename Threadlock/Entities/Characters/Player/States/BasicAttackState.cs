using Microsoft.Xna.Framework;
using Nez.AI.FSM;
using System;
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

        public override void OnInitialized()
        {
            base.OnInitialized();

            _basicWeapon = _context.GetComponent<BasicWeapon>();
            _basicWeapon.CompletionEmitter.AddObserver(BasicWeaponEventTypes.Completed, AttackCompletedCallback);

            _context.OnWeaponChanged += OnWeaponChanged;
        }

        //public override void Begin()
        //{
        //    base.Begin();

        //    _basicWeapon = _context.GetComponent<BasicWeapon>();
        //    _basicWeapon.BeginAttack(AttackCompletedCallback);
        //}

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
        
        void OnWeaponChanged(BasicWeapon weapon)
        {
            if (_basicWeapon != null && _basicWeapon != weapon)
            {
                _basicWeapon.CompletionEmitter.RemoveObserver(BasicWeaponEventTypes.Completed, AttackCompletedCallback);
                _basicWeapon = null;
            }

            _basicWeapon = weapon;
            _basicWeapon.CompletionEmitter.AddObserver(BasicWeaponEventTypes.Completed, AttackCompletedCallback);
        }

        void AttackCompletedCallback()
        {
            if (TryMove())
                return;
            else
                _machine.ChangeState<Idle>();
        }
    }
}
