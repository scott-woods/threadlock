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
using Threadlock.Models;
using Threadlock.StaticData;
using Threadlock.UI;

namespace Threadlock.Entities
{
    public class ConveyorBelt : Entity
    {
        bool _canReceiveItems = true;
        FactoryItemStack _currentItem;

        float _itemTimer = 0f;
        float _itemRate = 5f;

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            SetTag(EntityTags.Building);

            var animator = AddComponent(new SpriteAnimator());
            animator.SetRenderLayer(RenderLayers.YSort);
            animator.Sprite = new Sprite(Graphics.CreateSingleColorTexture(16, 16, new Color(Color.Cyan.R, Color.Cyan.G, Color.Cyan.B, 255)));

            var collider = AddComponent(new BoxCollider(16, 16));

            var building = AddComponent(new Building("Conveyor Belt"));

            var itemProvider = AddComponent(new FactoryItemProvider(() =>
            {
                return _currentItem;
            }));

            var inputSlot = AddComponent(new InputSlot(TryReceiveItem));
            inputSlot.Position = Vector2.Zero;

            var outputSlot = AddComponent(new OutputSlot());
            outputSlot.Position = Vector2.Zero;
            outputSlot.Direction = new Vector2(0, -1);

            var interactable = AddComponent(new Interactable(collider));
            interactable.Emitter.AddObserver(InteractableEvents.Interacted, OnInteracted);
        }

        public override void OnRemovedFromScene()
        {
            if (TryGetComponent<Interactable>(out var interactable))
                interactable.Emitter.RemoveObserver(InteractableEvents.Interacted, OnInteracted);

            base.OnRemovedFromScene();
        }

        public override void Update()
        {
            base.Update();

            if (!_canReceiveItems)
            {
                _itemTimer += Time.DeltaTime;

                if (_itemTimer >= _itemRate)
                {
                    _itemTimer = 0f;
                    _canReceiveItems = true;
                }
            }
        }

        #region OBSERVERS

        void OnInteracted()
        {
            if (_currentItem != null)
            {
                var canvas = Scene.FindComponentOfType<UICanvas>();
                canvas?.AddComponent(new BuildingMenu("Conveyor Belt", _currentItem));
            }
        }

        #endregion

        bool TryReceiveItem(FactoryItem item)
        {
            if (_canReceiveItems)
            {
                if (_currentItem != null)
                {
                    if (_currentItem.Item != item)
                        return false;
                    _currentItem.Count++;
                    _canReceiveItems = false;
                    return true;
                }
                else
                {
                    _currentItem = new FactoryItemStack(item, 1);
                    _canReceiveItems = false;
                    return true;
                }
            }

            return false;
        }
    }
}
