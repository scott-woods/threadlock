using Microsoft.Xna.Framework;
using Nez;
using Nez.AI.FSM;
using Nez.Sprites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Entities.Characters.Player.BasicWeapons;
using Threadlock.SaveData;

namespace Threadlock.Entities.Characters.Player.States
{
    public class Move : PlayerState
    {
        SpriteAnimator _animator;
        VelocityComponent _velocityComponent;

        public override void OnInitialized()
        {
            base.OnInitialized();

            _animator = _context.GetComponent<SpriteAnimator>();
            _velocityComponent = _context.GetComponent<VelocityComponent>();
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            //var degreesToAdd = Controls.Instance.DirectionalInput.Value.X * 5;

            //var nextDegrees = (_context.RotationDegrees + degreesToAdd) % 360;
            //_context.SetRotationDegrees(nextDegrees);

            //var offset = _animator.LocalOffset;
            //offset = Mathf.RotateAround(offset, Vector2.Zero, degreesToAdd);
            //_animator.SetLocalOffset(offset);

            //_context.SetRotationDegrees()
            //if (Controls.Instance.DirectionalInput.Value.X > 0)
            //    _context.SetRotationDegrees(Math.Clamp(_context.RotationDegrees + 5, 0, 360));
            //else if (Controls.Instance.DirectionalInput.Value.X < 0)
            //    _context.SetRotationDegrees(Math.Clamp(_context.RotationDegrees - 5, 0, 360));

            _context.Run();
        }
    }
}
