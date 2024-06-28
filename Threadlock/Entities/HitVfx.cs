using Microsoft.Xna.Framework;
using Nez.Sprites;
using Nez;
using Nez.Textures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.StaticData;
using Threadlock.Helpers;
using System.Collections;

namespace Threadlock.Entities
{
    public class HitVfx : Entity
    {
        Color _color;
        string _animationName;

        SpriteAnimator _animator;

        public HitVfx(string animationName, Color? color = null)
        {
            _animationName = animationName;
            _color = color == null ? Color.White : color.Value;
        }

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            _animator = AddComponent(new SpriteAnimator());
            _animator.SetColor(_color);
            AnimatedSpriteHelper.LoadAnimation(_animationName, ref _animator);

            Game1.StartCoroutine(PlayEffect());
        }

        IEnumerator PlayEffect()
        {
            yield return AnimatedSpriteHelper.WaitForAnimation(_animator, _animationName);
            _animator.SetEnabled(false);
            Destroy();
        }
    }
}
