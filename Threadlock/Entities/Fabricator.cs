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
        public event Action<ItemRecipe> OnRecipeChanged;

        public List<ItemRecipe> ItemRecipes = new List<ItemRecipe>()
        {
            new ItemRecipe()
            {
                Item = FactoryItemDatabase.Items.GetValueOrDefault("BigNut"),
                Count = 3,
                TimeToCraft = 2f
            },
            new ItemRecipe()
            {
                Item = FactoryItemDatabase.Items.GetValueOrDefault("SmallNut"),
                Count = 5,
                TimeToCraft = 1f
            }
        };

        public ItemRecipe ActiveRecipe;

        //inventory
        public FactoryItemStack ItemStack;

        //components
        Interactable _interactable;
        SpriteAnimator _animator;
        OutputSlot _outputSlot;

        float _productionRate = 2f;
        float _productionTimer = 0f;

        public Fabricator()
        {
            
        }

        #region LIFECYCLE

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            ItemStack = new FactoryItemStack(ItemRecipes.First().Item, 0);

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
                if (ItemStack?.Count > 0)
                {
                    return ItemStack;
                }
                return null;
            }));

            _outputSlot = AddComponent(new OutputSlot());
            _outputSlot.Position = Vector2.Zero;
            _outputSlot.Direction = new Vector2(0, -1);

            SetOutputItem(ItemRecipes.First());
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
            if (_productionTimer >= ActiveRecipe.TimeToCraft)
            {
                _productionTimer = 0f;

                ItemStack.Count = Math.Clamp(ItemStack.Count + ActiveRecipe.Count, 0, ItemStack.Item.MaxStackSize);
            }
        }

        #endregion

        #region OBSERVERS

        void OnInteracted()
        {
            var canvas = Scene.FindComponentOfType<UICanvas>();
            canvas?.AddComponent(new FabricatorMenu(this));
            //canvas?.AddComponent(new BuildingMenu(GetComponent<Building>(), ItemStack));
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

        public void SetOutputItem(ItemRecipe recipe)
        {
            var prevRecipe = ActiveRecipe;
            ActiveRecipe = recipe;

            if (prevRecipe != ActiveRecipe)
                OnRecipeChanged?.Invoke(ActiveRecipe);

            ItemStack.Item = recipe.Item;
            ItemStack.Count = 0;

            _productionTimer = 0f;
        }
    }
}
