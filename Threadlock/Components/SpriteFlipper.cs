using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Components
{
    public class SpriteFlipper : Component, IUpdatable
    {
        public bool Flipped = false;
        VelocityComponent _velocityComponent;
        List<SpriteRenderer> _renderers;

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            _velocityComponent = Entity.GetComponent<VelocityComponent>();
            _renderers = Entity.GetComponents<SpriteRenderer>();
        }

        public void Update()
        {
            var flip = _velocityComponent.LastNonZeroDirection.X < 0;

            if (Flipped != flip)
            {
                Flipped = flip;

                foreach (var renderer in _renderers)
                {
                    renderer.FlipX = flip;

                    var newOffsetX = renderer.LocalOffset.X * -1;
                    var newOffset = new Vector2(newOffsetX, renderer.LocalOffset.Y);
                    renderer.SetLocalOffset(newOffset);
                }
            }
        }
    }
}
