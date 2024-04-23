using Microsoft.Xna.Framework;
using Nez.Sprites;
using Nez.Textures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Helpers;
using Threadlock.StaticData;

namespace Threadlock.Components.TiledComponents
{
    public class AnimatedObject : TiledComponent
    {
        SpriteAnimator _animator;

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            if (TmxObject.Properties != null)
            {
                if (TmxObject.Properties.TryGetValue("TexturePath", out var path))
                {
                    var texture = Entity.Scene.Content.LoadTexture($@"Content\Textures\Tilesets\{path}.png");
                    var sprites = Sprite.SpritesFromAtlas(texture, (int)TmxObject.Width, (int)TmxObject.Height);
                    if (TmxObject.Properties.TryGetValue("Frames", out var frames))
                        sprites = sprites.GetRange(0, Convert.ToInt32(frames));
                    _animator = Entity.AddComponent(new SpriteAnimator());
                    _animator.SetLocalOffset(new Vector2(TmxObject.Width / 2, TmxObject.Height / 2));
                    _animator.SetRenderLayer(RenderLayers.YSort);
                    _animator.AddAnimation("Main", sprites.ToArray());

                    var offset = new Vector2(TmxObject.Width / 2, (TmxObject.Height) - 8);
                    Entity.AddComponent(new OriginComponent(offset));

                    _animator.Play("Main");
                }
            }
        }
    }
}
