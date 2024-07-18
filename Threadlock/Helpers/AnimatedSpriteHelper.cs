using Microsoft.Xna.Framework;
using Nez;
using Nez.Persistence;
using Nez.Sprites;
using Nez.Textures;
using Nez.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Components.EnemyActions;
using Threadlock.Entities.Characters;
using Threadlock.Entities.Characters.Enemies;
using Threadlock.Entities.Characters.Player;
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

        public static ICoroutine PlayAnimation(ref SpriteAnimator animator, string animationName)
        {
            return Game1.StartCoroutine(PlayAnimationCoroutine(animator, animationName));
        }

        public static IEnumerator WaitForAnimation(SpriteAnimator animator, string animationName)
        {
            yield return PlayAnimationCoroutine(animator, animationName);
        }

        static IEnumerator PlayAnimationCoroutine(SpriteAnimator animator, string animationName, AnimationConfig2 parentConfig = null)
        {
            //if animator is null, name is null, or already active, break
            if (animator == null || animator.Entity == null || string.IsNullOrWhiteSpace(animationName) || (animator.IsAnimationActive(animationName) && animator.AnimationState != SpriteAnimator.State.Completed))
                yield break;

            //get the config for this animation
            if (Animations.TryGetAnimationConfig(animationName, out var config))
            {
                //retrieve sprite flipper
                var flipper = animator.Entity.GetComponent<SpriteFlipper>();

                //if this is a directional animation, determine the direction
                if (config.UseDirections)
                {
                    //get the directional animation name
                    var childAnimationName = GetDirectionalAnimationName(config, animator.Entity);

                    //call play animation with the new directional name
                    yield return PlayAnimationCoroutine(animator, childAnimationName, config);
                }
                else //this is not a directional animation container, play it normally
                {
                    //update sprite flipper if necessary
                    flipper?.SetFlipX(config.FlipX);

                    //play the animation
                    animator.Play(animationName, config.Loop ?? false ? SpriteAnimator.LoopMode.Loop : SpriteAnimator.LoopMode.Once);

                    //handle what happens while animation is running
                    var currentFrame = -1;
                    while (animator.CurrentAnimationName == animationName && animator.AnimationState != SpriteAnimator.State.Completed)
                    {
                        if (animator.Entity == null || animator.Entity.IsDestroyed)
                            yield break;

                        ////if directional, check if we should change direction
                        //if (parentConfig != null && parentConfig.CanDirectionChange && parentConfig.UseDirections)
                        //{
                        //    //get the directional animation name
                        //    var childAnimationName = GetDirectionalAnimationName(parentConfig, animator.Entity);

                        //    //if we've changed direction, change animation
                        //    if (childAnimationName != animationName)
                        //    {
                        //        yield return PlayAnimationCoroutine(animator, childAnimationName, parentConfig, animator.CurrentFrame);
                        //        break;
                        //    }
                        //}

                        //if current frame is mismatched, this is the first time we're hitting this frame
                        if (currentFrame != animator.CurrentFrame)
                        {
                            //handle frame data
                            if (config.FrameData.TryGetValue(animator.CurrentFrame, out var frameData))
                            {
                                //handle sounds
                                if (frameData.Sounds != null && frameData.Sounds.Count > 0)
                                {
                                    if (frameData.PickRandomSound)
                                        Game1.AudioManager.PlaySound($"Content/Audio/Sounds/{frameData.Sounds.RandomItem()}.wav");
                                    else
                                    {
                                        foreach (var sound in frameData.Sounds)
                                            Game1.AudioManager.PlaySound($"Content/Audio/Sounds/{sound}.wav");
                                    }
                                }
                            }

                            //update frame
                            currentFrame = animator.CurrentFrame;
                        }

                        yield return null;
                    }
                }
            }
            else if (animator.Animations.ContainsKey(animationName)) //just in case there's an animation somehow not from the config file
            {
                animator.Play(animationName, SpriteAnimator.LoopMode.Once);
                while (animator.CurrentAnimationName == animationName && animator.AnimationState != SpriteAnimator.State.Completed)
                    yield return null;
            }
        }

        static string GetDirectionalAnimationName(AnimationConfig2 config, Entity owner)
        {
            //determine direction source
            Vector2 dir = Vector2.Zero;
            switch (config.DirectionSource)
            {
                case DirectionSource.Velocity:
                    if (owner.TryGetComponent<VelocityComponent>(out var vc))
                        dir = vc.Direction;
                    break;
                case DirectionSource.Aiming:
                    if (owner is Player player)
                        dir = player.GetFacingDirection();
                    else if (owner is SimPlayer simPlayer)
                        dir = simPlayer.GetFacingDirection();
                    else if (owner is Enemy enemy)
                        dir = EntityHelper.DirectionToEntity(enemy, enemy.TargetEntity);
                    break;
            }

            //alter animation name for direction
            var animationName = config.Name;
            var childAnimationName = animationName;

            //init dir string
            string dirString;

            //set dirString based on angle
            var angle = DirectionHelper.GetDegreesFromDirection(dir);
            if (angle >= 45 && angle < 135)
                dirString = "Down";
            else if (angle >= 135 && angle < 225)
                dirString = "Left";
            else if (angle >= 225 && angle < 315)
                dirString = "Up";
            else
                dirString = "Right";

            //handle animation name
            if (config.DirectionalAnimations.ContainsKey(dirString))
            {
                if (config.DirectionalAnimations.TryGetValue(dirString, out var anim) && anim != "Flip")
                    childAnimationName = anim;
            }
            else
            {
                if (dirString == "Up" || dirString == "Down")
                {
                    if (dir.X >= 0 && config.DirectionalAnimations.TryGetValue("Right", out var rightAnim))
                        childAnimationName = rightAnim;
                    else if (dir.X < 0 && config.DirectionalAnimations.TryGetValue("Left", out var leftAnim))
                        childAnimationName = leftAnim;
                }
                //if (dirString == "Left" && config.DirectionalAnimations.TryGetValue("Right", out var rightAnim))
                //    childAnimationName = rightAnim;
                //else if (dirString == "Right" && config.DirectionalAnimations.TryGetValue("Left", out var leftAnim))
                //    childAnimationName = leftAnim;
            }

            return childAnimationName;
        }

        public static void LoadAnimations(ref SpriteAnimator animator, params string[] animationNames)
        {
            foreach (var animName in animationNames)
            {
                LoadAnimation(animName, ref animator);
            }
        }

        public static void LoadAnimationsGlobal(ref SpriteAnimator animator, params string[] animationNames)
        {
            foreach (var animName in animationNames)
            {
                LoadAnimation(animName, ref animator, true);
            }
        }

        public static void LoadAnimation(string animationName, ref SpriteAnimator animator, bool global = false, int fps = 10)
        {
            if (string.IsNullOrWhiteSpace(animationName) || animator.Animations.ContainsKey(animationName))
                return;

            if (Animations.TryGetAnimationConfig(animationName, out var config))
            {
                //if this is a directional only animation, load the directions
                if (config.UseDirections)
                {
                    foreach (var directionalAnim in config.DirectionalAnimations)
                    {
                        LoadAnimation(directionalAnim.Value, ref animator, global, config.FPS ?? fps);
                    }
                }
                else //load normally
                {
                    if (Animations.TryGetAnimationSprites(animationName, out var sprites, global))
                    {
                        animator.AddAnimation(animationName, sprites, config.FPS ?? fps);
                    }
                }

                //load chain to if exists
                if (!string.IsNullOrWhiteSpace(config.ChainTo))
                    LoadAnimation(config.ChainTo, ref animator, global);
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

        public static bool IsAnimationPlaying(SpriteAnimator animator, string animationName)
        {
            if (animator.CurrentAnimationName == animationName && animator.AnimationState != SpriteAnimator.State.Completed)
                return true;
            else
            {
                if (Animations.TryGetAnimationConfig(animationName, out var config))
                {
                    if (config.UseDirections)
                    {
                        foreach (var dir in config.DirectionalAnimations)
                        {
                            if (animator.CurrentAnimationName == dir.Value && animator.AnimationState != SpriteAnimator.State.Completed)
                                return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
