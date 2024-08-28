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
    public class Fabricator : Entity
    {
        Interactable _interactable;

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            var animator = AddComponent(new SpriteAnimator());
            animator.SetRenderLayer(RenderLayers.YSort);
            animator.Sprite = new Sprite(Graphics.CreateSingleColorTexture(32, 32, new Color(Color.Fuchsia.R, Color.Fuchsia.G, Color.Fuchsia.B, 255)));

            var collider = AddComponent(new BoxCollider(24, 24));
            Flags.SetFlagExclusive(ref collider.PhysicsLayer, PhysicsLayers.Environment);
            Flags.SetFlagExclusive(ref collider.CollidesWithLayers, PhysicsLayers.Cursor);

            var building = AddComponent(new Building("Fabricator"));

            _interactable = AddComponent(new Interactable(collider));
            _interactable.Emitter.AddObserver(InteractableEvents.Interacted, OnInteracted);
        }

        public override void OnRemovedFromScene()
        {
            base.OnRemovedFromScene();

            _interactable.Emitter.RemoveObserver(InteractableEvents.Interacted, OnInteracted);
        }

        void OnInteracted()
        {
            //pickup
        }
    }
}
