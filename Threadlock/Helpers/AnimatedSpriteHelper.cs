using Nez.Persistence;
using Nez.Sprites;
using Nez.Textures;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Models;
using Threadlock.StaticData;

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
        public static Sprite[] GetSpriteArrayByRow(List<Sprite> spriteList, int row, int columns, int totalColumns, int startFrame = 0)
        {
            var sprites = new List<Sprite>();

            for (int i = 0; i < columns; i++)
            {
                sprites.Add(spriteList[i + (row * totalColumns) + startFrame]);
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

        /// <summary>
        /// get the length of the currently playing animation on this animator, in seconds
        /// </summary>
        /// <param name="animator"></param>
        /// <returns></returns>
        public static float GetAnimationDuration(SpriteAnimator animator)
        {
            if (!string.IsNullOrWhiteSpace(animator.CurrentAnimationName))
                return GetAnimationDuration(animator, animator.CurrentAnimationName);
            return 0f;
        }

        /// <summary>
        /// get the length of the animation with a specified name on the animator, in seconds
        /// </summary>
        /// <param name="animator"></param>
        /// <param name="animationName"></param>
        /// <returns></returns>
        public static float GetAnimationDuration(SpriteAnimator animator, string animationName)
        {
            if (!animator.Animations.ContainsKey(animationName))
                return 0;

            var secondsPerFrame = 1 / (animator.Animations[animationName].FrameRates[0] * animator.Speed);
            var iterationDuration = secondsPerFrame * animator.Animations[animationName].Sprites.Length;

            return iterationDuration;
        }

        public static void PlayAnimation(ref SpriteAnimator animator, string animationName)
        {
            Game1.StartCoroutine(PlayAnimationCoroutine(animator, animationName));
        }

        public static IEnumerator WaitForAnimation(SpriteAnimator animator, string animationName)
        {
            yield return PlayAnimationCoroutine(animator, animationName);
        }

        static IEnumerator PlayAnimationCoroutine(SpriteAnimator animator, string animationName)
        {
            if (animator == null || string.IsNullOrWhiteSpace(animationName))
                yield break;

            if (Animations.TryGetAnimationSprites(animationName, out var sprites) && Animations.TryGetAnimationConfig(animationName, out var config))
            {
                if (!animator.Animations.ContainsKey(animationName))
                    animator.AddAnimation(animationName, sprites);

                animator.Play(animationName, config.Loop ? SpriteAnimator.LoopMode.Loop : SpriteAnimator.LoopMode.Once);

                var currentFrame = -1;
                while (animator.CurrentAnimationName == animationName && animator.AnimationState != SpriteAnimator.State.Completed)
                {
                    //if current frame is mismatched, this is the first time we're hitting this frame
                    if (currentFrame != animator.CurrentFrame)
                    {
                        if (config.FrameData.TryGetValue(animator.CurrentFrame, out var frameData))
                        {
                            foreach (var sound in frameData.Sounds)
                                Game1.AudioManager.PlaySound($"Content/Audio/Sounds/{sound}.wav");
                        }

                        //update frame
                        currentFrame = animator.CurrentFrame;
                    }

                    yield return null;
                }
            }
        }

        public static void LoadAnimations(ref SpriteAnimator animator, params string[] animationNames)
        {
            foreach (var animName in animationNames)
            {
                LoadAnimation(animName, ref animator);
            }
        }

        public static void LoadAnimation(string animationName, ref SpriteAnimator animator)
        {
            if (string.IsNullOrWhiteSpace(animationName) || animator.Animations.ContainsKey(animationName))
                return;

            if (Animations.TryGetAnimationConfig(animationName, out var config) && Animations.TryGetAnimationSprites(animationName, out var sprites))
            {
                animator.AddAnimation(animationName, sprites);
                if (!string.IsNullOrWhiteSpace(config.ChainTo))
                    LoadAnimation(config.ChainTo, ref animator);
            }
        }

        public static void ParseAnimationFile(string folderPath, string filename, ref SpriteAnimator animator, int fps = 10)
        {
            //read file
            var json = File.ReadAllText($"{folderPath}/{filename}.json");
            if (string.IsNullOrWhiteSpace(json))
                return;

            //parse json
            var export = Json.FromJson<AsepriteExport>(json);
            if (export == null)
                return;

            //load texture
            var texture = Game1.Scene.Content.LoadTexture($"{folderPath}/{export.Meta.Image}");

            //each frame tag represents an animation
            foreach (var tag in export.Meta.FrameTags)
            {
                //init sprite list
                var sprites = new List<Sprite>();

                //go through frames for this tag
                var currentFrame = tag.From;
                while (currentFrame < tag.To)
                {
                    //get frame data
                    var frameData = export.Frames[currentFrame];

                    //create sprite from frame data and add it to the list
                    var sprite = new Sprite(texture, frameData.Frame.ToRectangle());
                    sprites.Add(sprite);

                    //increment frame
                    currentFrame++;
                }

                //create animation and add it to the animator
                animator.AddAnimation(tag.Name, sprites.ToArray(), fps);
            }
        }
    }
}
