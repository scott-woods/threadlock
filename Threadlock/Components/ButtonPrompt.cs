using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using Nez.Textures;
using System;
using System.Linq;
using Threadlock.Entities.Characters.Player;
using Threadlock.StaticData;

namespace Threadlock.Components
{
    /// <summary>
    /// displays an icon in world space when near the entity, indicating it is interactable
    /// </summary>
    public class ButtonPrompt : Component, ITriggerListener, IUpdatable
    {
        public event Action OnClicked;
        public event Action OnEntered;
        public event Action OnExited;

        float _radius;
        Vector2 _offset;

        SpriteRenderer _renderer;
        Collider _collider;

        public ButtonPrompt(float radius, Vector2 offset)
        {
            _radius = radius;
            _offset = offset;
        }

        public ButtonPrompt(float radius, RenderableComponent renderable)
        {
            _radius = radius;
            _offset = new Vector2(0, ((renderable.Height / 2) + (renderable.Height * .2f)) * -1);
        }

        public ButtonPrompt(Collider collider)
        {
            _collider = collider;
            _offset = Vector2.Zero;
        }

        public ButtonPrompt (Collider collider, Vector2 offset)
        {
            _collider = collider;
            _offset = offset;
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            _renderer = Entity.AddComponent(new SpriteRenderer());
            _renderer.SetRenderLayer(RenderLayers.AboveFront);
            var texture = Entity.Scene.Content.LoadTexture(Nez.Content.Textures.UI.KeyboardSymbols.KeyboardLettersandSymbols);
            var sprite = new Sprite(texture, 64, 32, 16, 16);
            _renderer.SetSprite(sprite);
            _renderer.SetLocalOffset(_offset);
            _renderer.SetEnabled(false);

            if (_collider == null)
            {
                _collider = Entity.AddComponent(new CircleCollider(_radius));
                _collider.IsTrigger = true;
                _collider.CollidesWithLayers = 0;
                Flags.SetFlagExclusive(ref _collider.PhysicsLayer, PhysicsLayers.PromptTrigger);
                Flags.SetFlag(ref _collider.CollidesWithLayers, PhysicsLayers.PlayerHurtbox);
                Flags.SetFlag(ref _collider.CollidesWithLayers, PhysicsLayers.PlayerCollider);
            }
        }

        public void Update()
        {
            if (Entity.Scene.FindEntity("Player") is Player player && player.TryRaycast(_collider.PhysicsLayer, out var raycast))
            {
                if (raycast.Collider == _collider)
                {
                    _renderer.SetEnabled(true);
                    OnEntered?.Invoke();
                }
            }
            else if (!Physics.BoxcastBroadphaseExcludingSelf(_collider, _collider.CollidesWithLayers).Any())
            {
                if (_renderer.Enabled)
                {
                    _renderer.SetEnabled(false);
                    OnExited?.Invoke();
                }
            }
        }

        public void OnTriggerEnter(Collider other, Collider local)
        {
            _renderer.SetEnabled(true);
            OnEntered?.Invoke();
        }

        public void OnTriggerExit(Collider other, Collider local)
        {
            _renderer.SetEnabled(false);
            OnExited?.Invoke();
        }

        /// <summary>
        /// called from player state
        /// </summary>
        public void Trigger()
        {
            OnClicked?.Invoke();
        }
    }
}
