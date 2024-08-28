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
    public class ConveyorBelt : Entity
    {
        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            var animator = AddComponent(new SpriteAnimator());
            animator.SetRenderLayer(RenderLayers.YSort);
            animator.Sprite = new Sprite(Graphics.CreateSingleColorTexture(16, 16, new Color(Color.Cyan.R, Color.Cyan.G, Color.Cyan.B, 255)));

            var collider = AddComponent(new BoxCollider(16, 16));

            var building = AddComponent(new Building("Conveyor Belt"));
        }
    }
}
