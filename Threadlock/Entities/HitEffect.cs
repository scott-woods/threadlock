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

namespace Threadlock.Entities
{
    public class HitEffect : Entity
    {
        HitEffectModel _hitEffectModel;
        Color _color;
        public SpriteAnimator Animator;

        public HitEffect(HitEffectModel hitEffectModel, Color? color = null)
        {
            _hitEffectModel = hitEffectModel;

            _color = color == null ? Color.White : color.Value;
        }

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            Animator = AddComponent(new SpriteAnimator());
            Animator.SetColor(_color);

            var texture = Scene.Content.LoadTexture(_hitEffectModel.EffectPath);
            var sprites = Sprite.SpritesFromAtlas(texture, _hitEffectModel.CellWidth, _hitEffectModel.CellHeight);
            Animator.AddAnimation("Hit", sprites.ToArray(), 13);

            PlayEffect();
        }

        public virtual void PlayEffect()
        {
            Animator.OnAnimationCompletedEvent += OnAnimationCompleted;
            Animator.Play("Hit", SpriteAnimator.LoopMode.Once);
        }

        void OnAnimationCompleted(string animationName)
        {
            Animator.OnAnimationCompletedEvent -= OnAnimationCompleted;
            Animator.SetEnabled(false);

            Destroy();
        }
    }
}
