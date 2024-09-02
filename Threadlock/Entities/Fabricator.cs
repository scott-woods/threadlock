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
using Threadlock.Helpers;
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
        OutputSlot _outputSlot;

        //inventory
        FactoryItemStack _itemStack;

        float _productionRate = 2f;
        float _productionTimer = 0f;

        public Fabricator()
        {
            
        }

        #region LIFECYCLE

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            _animator = AddComponent(new SpriteAnimator());
            _animator.SetRenderLayer(RenderLayers.YSort);
            AnimatedSpriteHelper.ParseAnimationFile("Content/Textures", "prototype_fabricator_sheet_config", ref _animator);
            _animator.Play("Fab_Idle_Down");
            //_animator.Sprite = new Sprite(Graphics.CreateSingleColorTexture(32, 32, new Color(Color.Fuchsia.R, Color.Fuchsia.G, Color.Fuchsia.B, 255)));

            var collider = AddComponent(new BoxCollider(24, 24));
            Flags.SetFlagExclusive(ref collider.PhysicsLayer, PhysicsLayers.Environment);
            Flags.SetFlagExclusive(ref collider.CollidesWithLayers, PhysicsLayers.Cursor);

            var building = AddComponent(new Building());
            building.GridSize = new Vector2(2, 2);
            building.OnOrientationChanged += OnOrientationChanged;

            _interactable = AddComponent(new Interactable(collider));
            _interactable.Emitter.AddObserver(InteractableEvents.Interacted, OnInteracted);

            var itemProvider = AddComponent(new FactoryItemProvider(() =>
            {
                if (_itemStack?.Count > 0)
                {
                    return _itemStack;
                }
                return null;
            }));

            _outputSlot = AddComponent(new OutputSlot());
            _outputSlot.Position = Vector2.Zero;
            _outputSlot.Direction = new Vector2(0, -1);

            SetOutputItem(FactoryItemDatabase.Items.GetValueOrDefault("BigNut"));
        }

        public override void OnRemovedFromScene()
        {
            _interactable.Emitter.RemoveObserver(InteractableEvents.Interacted, OnInteracted);

            if (TryGetComponent<Building>(out var building))
                building.OnOrientationChanged -= OnOrientationChanged;

            base.OnRemovedFromScene();
        }

        public override void Update()
        {
            base.Update();

            //handle production
            _productionTimer += Time.DeltaTime;
            if (_productionTimer >= _productionRate)
            {
                _productionTimer = 0f;

                if (_itemStack.Count < _itemStack.Item.MaxStackSize)
                    _itemStack.Count++;
            }
        }

        #endregion

        #region OBSERVERS

        void OnInteracted()
        {
            var canvas = Scene.FindComponentOfType<UICanvas>();
            canvas?.AddComponent(new BuildingMenu(GetComponent<Building>(), _itemStack));
        }

        void OnOrientationChanged(BuildingOrientation orientation)
        {
            switch (orientation)
            {
                case BuildingOrientation.Up:
                    _animator.Play("Fab_Idle_Up");
                    _outputSlot.Position = new Vector2(1, 1);
                    _outputSlot.Direction = new Vector2(0, 1);
                    break;
                case BuildingOrientation.Right:
                    _animator.Play("Fab_Idle_Right");
                    _outputSlot.Position = new Vector2(0, 1);
                    _outputSlot.Direction = new Vector2(-1, 0);
                    break;
                case BuildingOrientation.Down:
                    _animator.Play("Fab_Idle_Down");
                    _outputSlot.Position = new Vector2(0, 0);
                    _outputSlot.Direction = new Vector2(0, -1);
                    break;
                case BuildingOrientation.Left:
                    _animator.Play("Fab_Idle_Left");
                    _outputSlot.Position = new Vector2(1, 0);
                    _outputSlot.Direction = new Vector2(1, 0);
                    break;
            }
        }

        #endregion

        public void SetOutputItem(FactoryItem item)
        {
            _itemStack = new FactoryItemStack(item);
        }
    }
}
