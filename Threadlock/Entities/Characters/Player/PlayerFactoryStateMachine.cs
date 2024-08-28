using Nez.AI.FSM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Entities.Characters.Player.States.Factory;
using Threadlock.Entities.Characters.Player.States.Shared;
using Threadlock.SaveData;

namespace Threadlock.Entities.Characters.Player
{
    public class PlayerFactoryStateMachine : StateMachine<Player>
    {
        public PlayerFactoryStateMachine(Player context) : base(context, new FactoryIdle())
        {
            AddState(new CutsceneState());
            AddState(new DyingState());
            AddState(new FactoryMove());
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            //toggle building placer
            if (Controls.Instance.Action1.IsPressed)
            {
                if (_context.TryGetComponent<BuildingPlacer>(out var buildingPlacer))
                    buildingPlacer.SetEnabled(!buildingPlacer.Enabled);
            }
        }
    }
}
