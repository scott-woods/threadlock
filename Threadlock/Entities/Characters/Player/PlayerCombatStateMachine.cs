using Nez.AI.FSM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Entities.Characters.Player.States;
using Threadlock.Entities.Characters.Player.States.Combat;
using Threadlock.Entities.Characters.Player.States.Shared;

namespace Threadlock.Entities.Characters.Player
{
    public class PlayerCombatStateMachine : StateMachine<Player>
    {
        public PlayerCombatStateMachine(Player context) : base(context, new Idle())
        {
            AddState(new CutsceneState());
            AddState(new DyingState());
            AddState(new Move());

            AddState(new BasicAttackState());
            AddState(new DashState());
            AddState(new ExecutingActionState());
            AddState(new PreparingActionState());
            AddState(new SequencedAttackState());
            AddState(new StunnedState());
        }
    }
}
