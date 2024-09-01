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
using Threadlock.Helpers;
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

        OutputSlot _outputSlot;
        SpriteAnimator _animator;

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            SetTag(EntityTags.Building);

            _animator = AddComponent(new SpriteAnimator());
            _animator.SetRenderLayer(RenderLayers.YSort);
            AnimatedSpriteHelper.ParseAnimationFile("Content/Textures", "prototype_conveyor_config", ref _animator);
            _animator.Play("Conveyor_Moving_Down");

            var collider = AddComponent(new BoxCollider(16, 16));
            Flags.SetFlag(ref collider.PhysicsLayer, PhysicsLayers.Environment);

            var building = AddComponent(new Building());
            building.GridSize = new Vector2(1, 1);
            building.OnOrientationChanged += OnOrientationChanged;

            var itemProvider = AddComponent(new FactoryItemProvider(() =>
            {
                return _currentItem;
            }));

            var inputSlot = AddComponent(new InputSlot(TryReceiveItem));
            inputSlot.Position = Vector2.Zero;

            _outputSlot = AddComponent(new OutputSlot());
            _outputSlot.Position = Vector2.Zero;
            _outputSlot.Direction = new Vector2(0, -1);

            var interactable = AddComponent(new Interactable(collider));
            interactable.Emitter.AddObserver(InteractableEvents.Interacted, OnInteracted);
        }

        public override void OnRemovedFromScene()
        {
            if (TryGetComponent<Interactable>(out var interactable))
                interactable.Emitter.RemoveObserver(InteractableEvents.Interacted, OnInteracted);

            if (TryGetComponent<Building>(out var building))
                building.OnOrientationChanged -= OnOrientationChanged;

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

        void OnOrientationChanged(BuildingOrientation orientation)
        {
            switch (orientation)
            {
                case BuildingOrientation.Up:
                    _animator.Play("Conveyor_Moving_Up");
                    _outputSlot.Direction = new Vector2(0, 1);
                    break;
                case BuildingOrientation.Right:
                    _animator.Play("Conveyor_Moving_Right");
                    _outputSlot.Direction = new Vector2(1, 0);
                    break;
                case BuildingOrientation.Down:
                    _animator.Play("Conveyor_Moving_Down");
                    _outputSlot.Direction = new Vector2(0, -1);
                    break;
                case BuildingOrientation.Left:
                    _animator.Play("Conveyor_Moving_Left");
                    _outputSlot.Direction = new Vector2(-1, 0);
                    break;
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
