using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Entities.Characters.Player;

namespace Threadlock.Entities.Characters
{
    public class TestChaser : Entity
    {
        //consts
        const float _speed = 75f;

        //components
        PrototypeSpriteRenderer _spriteRenderer;
        Mover _mover;
        VelocityComponent _velocityComponent;

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            _spriteRenderer = AddComponent(new PrototypeSpriteRenderer(8, 8));
            _mover = AddComponent(new Mover());
            _velocityComponent = AddComponent(new VelocityComponent(_mover));
        }

        public override void Update()
        {
            base.Update();

            var dir = Player.Player.Instance.Position - Position;
            dir.Normalize();
            _velocityComponent.Move(dir, _speed);
        }
    }
}
