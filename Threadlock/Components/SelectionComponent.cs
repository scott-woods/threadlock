using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez;
using Nez.BitmapFonts;
using Nez.PhysicsShapes;
using Nez.Sprites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Effects;
using Threadlock.StaticData;

namespace Threadlock.Components
{
    public class SelectionComponent : Component
    {
        SpriteRenderer _renderer;
        Collider _collider;
        float _padding;

        public SelectionComponent(SpriteRenderer renderer, float padding = 0)
        {
            _renderer = renderer;
            _padding = padding;
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            if (Entity.TryGetComponent<Hurtbox>(out var hurtbox))
            {
                if (hurtbox.Collider is BoxCollider collider)
                {
                    _collider = Entity.AddComponent(new BoxCollider(collider.Width + _padding, collider.Height + _padding));
                }
                if (hurtbox.Collider is CircleCollider circleCollider)
                {
                    _collider = Entity.AddComponent(new CircleCollider(circleCollider.Radius + (_padding * 2)));
                }
                if (hurtbox.Collider is PolygonCollider polygonCollider)
                {
                    var shape = polygonCollider.Shape as Polygon;
                    _collider = Entity.AddComponent(new PolygonCollider(shape.Points));
                }                

                Flags.SetFlagExclusive(ref _collider.PhysicsLayer, PhysicsLayers.Selectable);
                _collider.CollidesWithLayers = 0;
            }
        }

        public override void OnRemovedFromEntity()
        {
            base.OnRemovedFromEntity();

            if (_collider != null)
            {
                Entity.RemoveComponent(_collider);
                _collider = null;
            }
        }

        public void Highlight(Color color)
        {
            var outline = new SpriteOutline();
            outline.OutlineColor = color;
            outline.TextureSize = _renderer.Bounds.Size;
            _renderer.Material = new Material(BlendState.NonPremultiplied, outline);
        }

        public void Unhighlight()
        {
            _renderer.Material = null;
        }
    }
}
