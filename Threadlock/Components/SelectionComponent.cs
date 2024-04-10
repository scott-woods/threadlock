using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez;
using Nez.BitmapFonts;
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
                    var boxClone = collider.Clone() as BoxCollider;
                    _collider = Entity.AddComponent(boxClone);
                    boxClone.Width += _padding;
                    boxClone.Height += _padding;
                    //boxClone.LocalOffset -= new Vector2(_padding / 2, _padding / 2);
                }
                if (hurtbox.Collider is CircleCollider circleCollider)
                {
                    var circleClone = circleCollider.Clone() as CircleCollider;
                    _collider = Entity.AddComponent(circleClone);
                    circleClone.Radius += (_padding * 2);
                }
                if (hurtbox.Collider is PolygonCollider polygonCollider)
                {
                    var polygonClone = polygonCollider.Clone() as PolygonCollider;
                    _collider = Entity.AddComponent(polygonClone);
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
            _renderer.Material = new Material(BlendState.NonPremultiplied, outline);
        }

        public void Unhighlight()
        {
            _renderer.Material = null;
        }
    }
}
