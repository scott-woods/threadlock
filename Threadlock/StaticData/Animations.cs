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
                foreach (var anim in animations)
                {
                    //yeah i know this is terrible, my b
                    foreach (var kvp in anim.FrameData)
                        anim.FrameData[kvp.Key].Frame = kvp.Key;

                    dict.Add(anim.Name, anim);
                }
                    
            }

            return dict;
        });

        public static bool TryGetAnimationSprites(string name, out Sprite[] sprites)
        {
            sprites = null;

            if (string.IsNullOrWhiteSpace(name))
                return false;

            if (_animationDictionary.Value.TryGetValue(name, out var animConfig))
            {
                //TODO: figure out how sprite sheets should be exported and read

                var texture = Game1.Scene.Content.LoadTexture($"Content/Textures/{animConfig.Path}.png");
                var allSprites = Sprite.SpritesFromAtlas(texture, animConfig.CellWidth, animConfig.CellHeight);
                var totalColumns = texture.Width / animConfig.CellWidth;

                sprites = AnimatedSpriteHelper.GetSpriteArrayByRow(allSprites, animConfig.Row, animConfig.Frames, totalColumns, animConfig.StartFrame);

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
    }
}
