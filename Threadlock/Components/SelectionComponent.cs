﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez;
using Nez.PhysicsShapes;
using Nez.Sprites;
using Threadlock.Effects;
using Threadlock.StaticData;

namespace Threadlock.Components
{
    /// <summary>
    /// Added to entities that can be selected in some way by the player
    /// </summary>
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

                _collider.IsTrigger = true;

                Flags.SetFlagExclusive(ref _collider.PhysicsLayer, PhysicsLayers.Selectable);
                Flags.SetFlagExclusive(ref _collider.CollidesWithLayers, PhysicsLayers.Selector);
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
