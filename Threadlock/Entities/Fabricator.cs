using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using Nez.Textures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;
using Threadlock.Components;
using Threadlock.Models;
using Threadlock.StaticData;
using Threadlock.UI;

namespace Threadlock.Entities
{
    public class Fabricator : Entity
    {
        //components
        Interactable _interactable;
        SpriteAnimator _animator;

        //inventory
        FactoryItemStack _outputSlot;

        Vector2 _outputPosition = new Vector2(0, -16);

        float _productionRate = 2f;
        float _productionTimer = 0f;

        #region LIFECYCLE

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            _animator = AddComponent(new SpriteAnimator());
            _animator.SetRenderLayer(RenderLayers.YSort);
            _animator.Sprite = new Sprite(Graphics.CreateSingleColorTexture(32, 32, new Color(Color.Fuchsia.R, Color.Fuchsia.G, Color.Fuchsia.B, 255)));

            var collider = AddComponent(new BoxCollider(24, 24));
            Flags.SetFlagExclusive(ref collider.PhysicsLayer, PhysicsLayers.Environment);
            Flags.SetFlagExclusive(ref collider.CollidesWithLayers, PhysicsLayers.Cursor);

            var building = AddComponent(new Building("Fabricator"));

            _interactable = AddComponent(new Interactable(collider));
            _interactable.Emitter.AddObserver(InteractableEvents.Interacted, OnInteracted);

            var itemProvider = AddComponent(new FactoryItemProvider(() =>
            {
                if (_outputSlot?.Count > 0)
                {
                    return _outputSlot;
                }
                return null;
            }));

            var outputSlot = AddComponent(new OutputSlot());
            outputSlot.Position = Vector2.Zero;
            outputSlot.Direction = new Vector2(0, -1);

            SetOutputItem(FactoryItemDatabase.Items.GetValueOrDefault("BigNut"));
        }

        public override void OnRemovedFromScene()
        {
            base.OnRemovedFromScene();

            _interactable.Emitter.RemoveObserver(InteractableEvents.Interacted, OnInteracted);
        }

        public override void Update()
        {
            base.Update();

            //handle production
            _productionTimer += Time.DeltaTime;
            if (_productionTimer >= _productionRate)
            {
                _productionTimer = 0f;

                if (_outputSlot.Count < _outputSlot.Item.MaxStackSize)
                    _outputSlot.Count++;
            }
        }

        #endregion

        #region OBSERVERS

        void OnInteracted()
        {
            var canvas = Scene.FindComponentOfType<UICanvas>();
            canvas?.AddComponent(new BuildingMenu("Fabricator", _outputSlot));
        }

        #endregion

        public void SetOutputItem(FactoryItem item)
        {
            _outputSlot = new FactoryItemStack(item);
        }
    }
}
