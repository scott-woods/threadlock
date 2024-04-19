using Nez.Sprites;
using Nez.Textures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Helpers
{
    public static class AnimatedSpriteHelper
    {
        /// <summary>
        /// Get a sprite array given sprite list in a range of indicies (both inclusive)
        /// </summary>
        /// <param name="spriteList"></param>
        /// <param name="firstIndex"></param>
        /// <param name="lastIndex"></param>
        /// <returns></returns>
        public static Sprite[] GetSpriteArrayFromRange(List<Sprite> spriteList, int firstIndex, int lastIndex)
        {
            var sprites = spriteList.Where((sprite, index) => index >= firstIndex && index <= lastIndex);
            return sprites.ToArray();
        }

        /// <summary>
        /// For spritesheets with several animations in one file. Row is the zero-based row in the sheet, columns is how many this animation is, and total is total number of columns
        /// </summary>
        /// <param name="spriteList"></param>
        /// <param name="row"></param>
        /// <param name="columns"></param>
        /// <param name="totalColumns"></param>
        /// <returns></returns>
        public static Sprite[] GetSpriteArrayByRow(List<Sprite> spriteList, int row, int columns, int totalColumns)
        {
            var sprites = new List<Sprite>();

            for (int i = 0; i < columns; i++)
            {
                sprites.Add(spriteList[i + (row * totalColumns)]);
            }

            return sprites.ToArray();
        }

        /// <summary>
        /// Get a sprite array with all sprites matching any of the given indicies
        /// </summary>
        /// <param name="spriteList"></param>
        /// <param name="indicies"></param>
        /// <returns></returns>
        public static Sprite[] GetSpriteArray(List<Sprite> spriteList, List<int> indicies, bool allowDuplicates = false)
        {
            var newSpriteList = new List<Sprite>();
            if (!allowDuplicates)
            {
                newSpriteList = spriteList.Where((sprite, index) => indicies.Contains(index)).ToList();
            }
            else
            {
                foreach(var index in indicies)
                {
                    var sprite = spriteList[index];
                    newSpriteList.Add(sprite);
                }
            }
            
            return newSpriteList.ToArray();
        }

        public static float GetAnimationDuration(SpriteAnimator animator)
        {
            if (!string.IsNullOrWhiteSpace(animator.CurrentAnimationName))
                return GetAnimationDuration(animator, animator.CurrentAnimationName);
            return 0f;
        }

        public static float GetAnimationDuration(SpriteAnimator animator, string animationName)
        {
            if (!animator.Animations.ContainsKey(animationName))
                return 0;

            var secondsPerFrame = 1 / (animator.Animations[animationName].FrameRates[0] * animator.Speed);
            var iterationDuration = secondsPerFrame * animator.Animations[animationName].Sprites.Length;

            return iterationDuration;
        }
    }
}
