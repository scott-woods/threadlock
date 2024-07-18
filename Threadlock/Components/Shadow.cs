using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using System;
using Threadlock.StaticData;

namespace Threadlock.Components
{
    public class Shadow : SpriteRenderer
    {
        SpriteRenderer _baseRenderer;

        public Shadow(SpriteRenderer baseRenderer)
        {
            _baseRenderer = baseRenderer;
        }

        public override void Initialize()
        {
            base.Initialize();

            var texture = Game1.Content.LoadTexture(Nez.Content.Textures.Effects.Shadow);
            //var sprite = new Sprite(texture);
            //SetSprite(sprite);
            SetTexture(texture);
            SetRenderLayer(RenderLayers.Shadow);
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            SetMaterial(Material.DefaultMaterial);

            if (Entity.TryGetComponent<OriginComponent>(out var oc))
                SetLocalOffset(new Vector2(Math.Abs(oc.Origin.X - Entity.Position.X), Math.Abs(oc.Origin.Y - Entity.Position.Y)));
        }
    }
}
