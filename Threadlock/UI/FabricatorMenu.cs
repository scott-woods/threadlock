using Nez;
using Nez.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Entities;
using Threadlock.Models;

namespace Threadlock.UI
{
    public class FabricatorMenu : Component
    {
        Table _root;
        Label _recipeNameLabel;
        Label _itemCountLabel;

        Fabricator _fabricator;

        RecipeMenu _recipeMenu;

        public FabricatorMenu(Fabricator fabricator)
        {
            _fabricator = fabricator;
        }

        #region LIFECYCLE

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            var skin = Skins.Skins.GetDefaultSkin();

            _root = new Table();
            _root.SetBackground(skin.GetNinePatchDrawable("window_blue"));
            _root.SetSize(300, 200);

            //recipe label
            _root.Add(new Label("Recipe: ", skin)).Left().Top().Pad(10);
            _recipeNameLabel = new Label($"{_fabricator.ActiveRecipe.Count / _fabricator.ActiveRecipe.TimeToCraft} {_fabricator.ActiveRecipe.Item.Name} per Second", skin);
            _root.Add(_recipeNameLabel).Left().Top().Pad(10);

            _root.Row();

            //inventory label
            _root.Add(new Label("Inventory: ", skin)).Left().Top().Pad(10);
            _itemCountLabel = new Label($"{_fabricator.ItemStack.Count} {_fabricator.ItemStack.Item.Name}", skin);
            _root.Add(_itemCountLabel).Left().Top().Pad(10);

            _root.Row();

            var recipeButton = new TextButton("Select Recipe", skin);
            recipeButton.OnClicked += (button) =>
            {
                _recipeMenu = new RecipeMenu(_fabricator.ItemRecipes);
                _recipeMenu.OnRecipeSelected += OnRecipeSelected;
                Entity.AddComponent(_recipeMenu);
            };
            _root.Add(recipeButton).Left().Top().Pad(10);

            _root.Row();

            var moveButton = new TextButton("Move", skin);
            moveButton.OnClicked += (button) =>
            {
                _fabricator.GetComponent<Building>().Pickup();
                CloseMenu();
            };
            _root.Add(moveButton).Left().Top().Pad(10);

            //connect to signals
            _fabricator.ItemStack.CountChanged += OnItemCountChanged;
            _fabricator.OnRecipeChanged += OnRecipeChanged;

            if (Entity.TryGetComponent<UICanvas>(out var canvas))
                canvas.Stage.AddElement(_root);
        }

        public override void OnRemovedFromEntity()
        {
            base.OnRemovedFromEntity();

            _root.Remove();

            _fabricator.ItemStack.CountChanged -= OnItemCountChanged;
            _fabricator.OnRecipeChanged -= OnRecipeChanged;
        }

        #endregion

        #region OBSERVERS

        void OnRecipeSelected(ItemRecipe recipe)
        {
            _recipeMenu.OnRecipeSelected -= OnRecipeSelected;
            _recipeMenu = null;

            _fabricator.SetOutputItem(recipe);
        }

        void OnRecipeChanged(ItemRecipe recipe)
        {
            _recipeNameLabel.SetText($"{recipe.Count / recipe.TimeToCraft} {recipe.Item.Name} per Second");
        }

        void OnItemCountChanged(int itemCount)
        {
            _itemCountLabel.SetText($"{itemCount.ToString()} {_fabricator.ItemStack.Item.Name}");
        }

        #endregion

        void CloseMenu()
        {
            this.RemoveComponent();
        }
    }
}
