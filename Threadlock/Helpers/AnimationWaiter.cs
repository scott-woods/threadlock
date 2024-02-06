using Nez.Sprites;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Helpers
{
    public class AnimationWaiter
    {
        SpriteAnimator _animator;
        bool _animationFinished = false;

        public AnimationWaiter(SpriteAnimator animator)
        {
            _animator = animator;
        }

        public IEnumerator WaitForAnimation(string animation)
        {
            _animationFinished = false;
            _animator.OnAnimationCompletedEvent += OnAnimationCompleted;

            _animator.Play(animation, SpriteAnimator.LoopMode.Once);

            while (!_animationFinished)
                yield return null;
        }

        public void Cancel()
        {
            _animator.OnAnimationCompletedEvent -= OnAnimationCompleted;
        }

        void OnAnimationCompleted(string animationName)
        {
            _animator.OnAnimationCompletedEvent -= OnAnimationCompleted;
            _animator.SetSprite(_animator.CurrentAnimation.Sprites.Last());

            _animationFinished = true;
        }
    }
}
