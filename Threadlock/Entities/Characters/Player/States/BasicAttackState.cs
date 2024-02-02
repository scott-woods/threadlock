using Nez.AI.FSM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Entities.Characters.Player;

namespace Threadlock.Entities.Characters.Player.States
{
    public class BasicAttackState : PlayerState
    {
        public override void Begin()
        {
            base.Begin();

            var attack = _context.GetComponent<SwordAttack>();
            attack.StartAttack(AttackCompletedCallback);
        }

        public override void Update(float deltaTime)
        {
            //throw new NotImplementedException();
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
