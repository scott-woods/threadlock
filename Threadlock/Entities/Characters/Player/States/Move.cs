﻿using Microsoft.Xna.Framework;
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
            _context.Run();
        }
    }
}
