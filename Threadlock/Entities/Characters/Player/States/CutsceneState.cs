using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Entities.Characters.Player.States
{
    public class CutsceneState : PlayerState
    {
        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            _context.Idle();
        }
    }
}
