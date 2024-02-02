using Nez.Sprites;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Helpers
{
    public static class CoroutineHelper
    {
        public static IEnumerator WaitForAnimation(SpriteAnimator animator, string animation)
        {
            animator.Play(animation, SpriteAnimator.LoopMode.Once);

            while (true)
            {
                if (animator.CurrentAnimationName != animation)
                    yield break;

                if (animator.AnimationState == SpriteAnimator.State.Completed)
                {
                    yield break;
                }

                yield return null;
            }
        }
    }
}
