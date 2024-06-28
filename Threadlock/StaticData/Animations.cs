using Microsoft.Xna.Framework.Graphics;
using Nez.Persistence;
using Nez.Textures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components.EnemyActions;
using Threadlock.Helpers;

namespace Threadlock.StaticData
{
    public class Animations
    {
        static readonly Lazy<Dictionary<string, AnimationConfig2>> _animationDictionary = new Lazy<Dictionary<string, AnimationConfig2>>(() =>
        {
            var dict = new Dictionary<string, AnimationConfig2>();

            if (File.Exists("Content/Data/Animations.json"))
            {
                var json = File.ReadAllText("Content/Data/Animations.json");
                var animations = Json.FromJson<AnimationConfig2[]>(json);

                dict = animations.ToDictionary(a => a.Name, a => a);

                foreach (var anim in dict.Values)
                {
                    //yeah i know this is terrible, my b
                    foreach (var kvp in anim.FrameData)
                        anim.FrameData[kvp.Key].Frame = kvp.Key;

                    if (!string.IsNullOrWhiteSpace(anim.Base))
                        ApplyInheritance(anim, dict);
                }
            }

            return dict;
        });

        public static bool TryGetAnimationSprites(string name, out Sprite[] sprites, bool global = false)
        {
            sprites = null;

            if (string.IsNullOrWhiteSpace(name))
                return false;

            if (_animationDictionary.Value.TryGetValue(name, out var animConfig))
            {
                if (animConfig.UseDirections != null && animConfig.UseDirections.Value)
                    return false;

                //TODO: figure out how sprite sheets should be exported and read

                Texture2D texture = null;
                if (global)
                    texture = Game1.Content.LoadTexture($"Content/Textures/{animConfig.Path}.png");
                else
                    texture = Game1.Scene.Content.LoadTexture($"Content/Textures/{animConfig.Path}.png");
                var allSprites = Sprite.SpritesFromAtlas(texture, animConfig.CellWidth ?? 16, animConfig.CellHeight ?? 16);
                var totalColumns = texture.Width / animConfig?.CellWidth ?? 16;
                var startFrame = animConfig.StartFrame ?? 0;
                var frameCount = animConfig.Frames ?? (totalColumns - startFrame);

                sprites = AnimatedSpriteHelper.GetSpriteArrayByRow(allSprites, animConfig.Row ?? 0, frameCount, totalColumns, startFrame);

                return true;
            }

            return false;
        }

        public static bool TryGetAnimationConfig(string name, out AnimationConfig2 config)
        {
            config = null;

            if (string.IsNullOrWhiteSpace(name))
                return false;

            return _animationDictionary.Value.TryGetValue(name, out config);
        }

        static void ApplyInheritance(AnimationConfig2 animation, Dictionary<string, AnimationConfig2> animDictionary)
        {
            if (string.IsNullOrWhiteSpace(animation.Base))
                return;

            if (animDictionary.TryGetValue(animation.Base, out var parentAnimation))
            {
                if (parentAnimation.Base != null)
                    ApplyInheritance(parentAnimation, animDictionary);

                animation.CellWidth ??= parentAnimation.CellWidth;
                animation.CellHeight ??= parentAnimation.CellHeight;
                animation.Row ??= parentAnimation.Row;
                animation.Frames ??= parentAnimation.Frames;
                animation.Loop ??= parentAnimation.Loop;
                animation.Path ??= parentAnimation.Path;
                animation.ChainTo ??= parentAnimation.ChainTo;
            }
        }
    }
}
