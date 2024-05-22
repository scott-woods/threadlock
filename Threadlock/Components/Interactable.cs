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
    /// <summary>
    /// requires a collider as a parameter. handles objects that must be checked to be interacted with
    /// </summary>
    public class Interactable : Component
    {
        public event Action OnInteracted;

        //passed components
        Collider _collider;

        //local components
        SpriteRenderer _promptRenderer;

        Vector2 _promptOffset;
        bool _focused = false;

        public Interactable(Collider collider)
        {
            _collider = collider;
        }

        public Interactable(Collider collider, Vector2 promptOffset)
        {
            _collider = collider;
            _promptOffset = promptOffset;
        }

        public override void Initialize()
        {
            base.Initialize();

            Flags.SetFlag(ref _collider.PhysicsLayer, PhysicsLayers.PromptTrigger);
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            //create prompt renderer
            _promptRenderer = Entity.AddComponent(new SpriteRenderer());
            _promptRenderer.SetRenderLayer(RenderLayers.AboveFront);
            var texture = Entity.Scene.Content.LoadTexture(Nez.Content.Textures.UI.KeyboardSymbols.KeyboardLettersandSymbols);
            var sprite = new Sprite(texture, 64, 32, 16, 16);
            _promptRenderer.SetSprite(sprite);
            _promptRenderer.SetLocalOffset(_promptOffset);
            _promptRenderer.SetEnabled(false);
        }

        public override void OnEnabled()
        {
            base.OnEnabled();

            _promptRenderer.SetEnabled(_focused);
        }

        public override void OnDisabled()
        {
            base.OnDisabled();

            _promptRenderer.SetEnabled(false);
        }

        public void SetFocus(bool focus)
        {
            _focused = focus;
            _promptRenderer.SetEnabled(focus);
        }

        public void Interact()
        {
            OnInteracted?.Invoke();
        }
    }
}
