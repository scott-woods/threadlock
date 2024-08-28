using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using Nez.Textures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.StaticData;

namespace Threadlock.Components
{
    public class ButtonPrompt : Component
    {
        //local components
        SpriteRenderer _promptRenderer;

        Interactable _interactable;

        Vector2 _promptOffset;

        public ButtonPrompt(Vector2 promptOffset)
        {
            _promptOffset = promptOffset;
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            _interactable = Entity.GetComponent<Interactable>();
            _interactable?.Emitter.AddObserver(InteractableEvents.FocusEntered, OnFocusEntered);
            _interactable?.Emitter.AddObserver(InteractableEvents.FocusExited, OnFocusExited);

            //create prompt renderer
            _promptRenderer = Entity.AddComponent(new SpriteRenderer());
            _promptRenderer.SetRenderLayer(RenderLayers.AboveFront);
            var texture = Entity.Scene.Content.LoadTexture(Nez.Content.Textures.UI.KeyboardSymbols.KeyboardLettersandSymbols);
            var sprite = new Sprite(texture, 64, 32, 16, 16);
            _promptRenderer.SetSprite(sprite);
            _promptRenderer.SetLocalOffset(_promptOffset);
            _promptRenderer.SetEnabled(false);
        }

        public override void OnRemovedFromEntity()
        {
            base.OnRemovedFromEntity();

            _interactable?.Emitter.RemoveObserver(InteractableEvents.FocusEntered, OnFocusEntered);
            _interactable?.Emitter.RemoveObserver(InteractableEvents.FocusExited, OnFocusExited);
        }

        void OnFocusEntered()
        {
            _promptRenderer.SetEnabled(true);
        }

        void OnFocusExited()
        {
            _promptRenderer.SetEnabled(false);
        }
    }
}
