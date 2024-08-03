using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez.Persistence;
using Nez.Textures;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Threadlock.Helpers;
using Threadlock.Models;

namespace Threadlock.StaticData
{
    public class Animations
    {
        static Dictionary<string, AnimationConfig> _animationDictionary;

        public static async Task InitializeAnimationDictionaryAsync()
        {
            _animationDictionary = await LoadAnimationsAsync();
        }

        static async Task<Dictionary<string, AnimationConfig>> LoadAnimationsAsync()
        {
            var dict = new Dictionary<string, AnimationConfig>();

            if (File.Exists("Content/Data/Animations.json"))
            {
                var json = await File.ReadAllTextAsync("Content/Data/Animations.json");
                var animations = Json.FromJson<AnimationConfig[]>(json);

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
        }

        //static readonly Lazy<Dictionary<string, AnimationConfig2>> _animationDictionary = new Lazy<Dictionary<string, AnimationConfig2>>(() =>
        //{
        //    var dict = new Dictionary<string, AnimationConfig2>();

        //    if (File.Exists("Content/Data/Animations.json"))
        //    {
        //        var json = File.ReadAllText("Content/Data/Animations.json");
        //        var animations = Json.FromJson<AnimationConfig2[]>(json);

        //        dict = animations.ToDictionary(a => a.Name, a => a);

        //        foreach (var anim in dict.Values)
        //        {
        //            //yeah i know this is terrible, my b
        //            foreach (var kvp in anim.FrameData)
        //                anim.FrameData[kvp.Key].Frame = kvp.Key;

        //            if (!string.IsNullOrWhiteSpace(anim.Base))
        //                ApplyInheritance(anim, dict);
        //        }
        //    }

        //    return dict;
        //});

        public static bool TryGetAnimationSprites(string name, out Sprite[] sprites, bool global = false)
        {
            sprites = null;

            if (string.IsNullOrWhiteSpace(name))
                return false;

            if (_animationDictionary.TryGetValue(name, out var animConfig))
            {
                //directional animation won't have any sprites
                if (animConfig.UseDirections)
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

                if (animConfig.Origin != null)
                {
                    for (int i = 0; i < sprites.Length; i++)
                    {
                        var sprite = sprites[i];
                        var origin = animConfig.Origin.Value;

                        if (animConfig.FlipOriginX)
                            origin.X = animConfig.CellWidth.Value - origin.X;

                        sprite.Origin = origin;
                    }
                }

                return true;
            }

            return false;
        }

        public static bool TryGetAnimationConfig(string name, out AnimationConfig config)
        {
            config = null;

            if (string.IsNullOrWhiteSpace(name))
                return false;

            return _animationDictionary.TryGetValue(name, out config);
        }

        static void ApplyInheritance(AnimationConfig animation, Dictionary<string, AnimationConfig> animDictionary)
        {
            if (string.IsNullOrWhiteSpace(animation.Base))
                return;

            if (animDictionary.TryGetValue(animation.Base, out var parentAnimation))
            {
                if (parentAnimation.Base != null)
                    ApplyInheritance(parentAnimation, animDictionary);

                animation.CellWidth ??= parentAnimation.CellWidth;
                animation.CellHeight ??= parentAnimation.CellHeight;
                animation.Origin ??= parentAnimation.Origin;
                animation.Row ??= parentAnimation.Row;
                animation.Frames ??= parentAnimation.Frames;
                animation.StartFrame ??= parentAnimation.StartFrame;
                animation.Loop ??= parentAnimation.Loop;
                animation.Path ??= parentAnimation.Path;
                animation.ChainTo ??= parentAnimation.ChainTo;
                animation.FPS ??= parentAnimation.FPS;

                foreach (var kvp in parentAnimation.FrameData)
                {
                    if (!animation.FrameData.ContainsKey(kvp.Key))
                        animation.FrameData[kvp.Key] = kvp.Value;
                }
            }
        }
    }

    public class AnimationConfig
    {
        public string Name;
        public string Path;

        public int? CellWidth;
        public int? CellHeight;
        public Vector2? Origin;
        public bool FlipX;
        public bool FlipOriginX;

        public int? Row;
        public int? Frames;
        public int? StartFrame;

        public bool? Loop;
        public int? FPS;

        public string Base;
        public string ChainTo;

        public Dictionary<int, FrameData> FrameData = new Dictionary<int, FrameData>();

        //directional config only
        public bool UseDirections;
        public Dictionary<string, string> DirectionalAnimations = new Dictionary<string, string>();
        public DirectionSource DirectionSource;
        public bool CanDirectionChange = true;
    }

    public enum DirectionSource
    {
        Velocity,
        Aiming
    }
}
