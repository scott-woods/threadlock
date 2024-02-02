using Nez;
using Nez.Sprites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Entities.Characters.Player;

namespace Threadlock.Entities.Characters
{
    public class SimPlayer : Entity
    {
        //components
        SpriteAnimator _animator;

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            _animator = AddComponent(new SpriteAnimator());
            _animator.SetColor(new Microsoft.Xna.Framework.Color(255, 255, 255, 128));

            //get animations from player animator
            if (Player.Player.Instance.TryGetComponent<SpriteAnimator>(out var animator))
            {
                foreach (var animation in animator.Animations)
                {
                    _animator.AddAnimation(animation.Key, animation.Value);
                }

                _animator.SetLocalOffset(animator.LocalOffset);
            }
        }

        public override void Update()
        {
            base.Update();

            if (!_animator.IsAnimationActive("IdleDown"))
                _animator.Play("IdleDown");
        }
    }
}
