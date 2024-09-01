using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using Nez.Textures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.StaticData;

namespace Threadlock.Entities
{
    public class Refinery : Entity
    {
        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            var animator = AddComponent(new SpriteAnimator());
            animator.SetRenderLayer(RenderLayers.YSort);
            animator.Sprite = new Sprite(Graphics.CreateSingleColorTexture(48, 48, new Color(Color.Red.R, Color.Red.G, Color.Red.B, 255)));

            var building = AddComponent(new Building());
            building.GridSize = new Vector2(3, 3);

            var collider = AddComponent(new BoxCollider(48, 48));
        }
    }
}
