using Nez.Sprites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Helpers;

namespace Threadlock.Entities.Characters.Player.States
{
    public class CutsceneState : PlayerState
    {
        SpriteAnimator _animator;

        public override void OnInitialized()
        {
            base.OnInitialized();

            _animator = _context.GetComponent<SpriteAnimator>();
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            AnimatedSpriteHelper.PlayAnimation(_animator, "Player_Idle");
        }
    }
}
