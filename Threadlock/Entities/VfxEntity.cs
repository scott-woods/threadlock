using Nez;
using Nez.Persistence;
using Nez.Sprites;
using Nez.Textures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components.EnemyActions;
using Threadlock.Helpers;
using Threadlock.Models;

namespace Threadlock.Entities
{
    public class VfxEntity : Entity
    {
        VfxConfig _config;

        SpriteAnimator _animator;
        public SpriteAnimator Animator { get => _animator; }

        public VfxEntity(VfxConfig config)
        {
            _config = config;

            _animator = new SpriteAnimator();
        }

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            AddComponent(_animator);

            AnimatedSpriteHelper.ParseAnimationFile("Content/Textures/VFX", _config.Name, ref _animator);

            Play();
        }

        void Play()
        {
            if (_animator.Animations.ContainsKey("Play"))
            {
                _animator.Play("Play", SpriteAnimator.LoopMode.Once);
                _animator.OnAnimationCompletedEvent += OnAnimationCompleted;
            }
            else
                Destroy();
        }

        void OnAnimationCompleted(string animationName)
        {
            _animator.OnAnimationCompletedEvent -= OnAnimationCompleted;
            _animator.SetEnabled(false);

            Destroy();
        }
    }
}
