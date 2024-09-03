using Nez;
using Nez.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Models;

namespace Threadlock.UI
{
    public class RecipeMenu : Component
    {
        public event Action<ItemRecipe> OnRecipeSelected;

        Table _root;

        List<ItemRecipe> _recipes;
        Dictionary<Button, ItemRecipe> _buttonDict = new Dictionary<Button, ItemRecipe>();

        public RecipeMenu(List<ItemRecipe> recipes)
        {
            _recipes = recipes;
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            var skin = Skins.Skins.GetDefaultSkin();

            _root = new Table();
            _root.SetBackground(skin.GetNinePatchDrawable("window_blue"));
            _root.SetSize(200, 100);

            foreach (var recipe in _recipes)
            {
                var button = new TextButton(recipe.Item.Name, skin);
                button.OnClicked += OnButtonClicked;
                
                _buttonDict.Add(button, recipe);

                _root.Add(button).Left().Top().Pad(10);
                _root.Row();
            }

            if (Entity.TryGetComponent<UICanvas>(out var canvas))
                canvas.Stage.AddElement(_root);
        }

        public override void OnRemovedFromEntity()
        {
            base.OnRemovedFromEntity();

            _root.Remove();
        }

        void OnButtonClicked(Button button)
        {
            if (_buttonDict.TryGetValue(button, out var recipe))
            {
                OnRecipeSelected?.Invoke(recipe);
                CloseMenu();
            }
        }

        void CloseMenu()
        {
            this.RemoveComponent();
        }
    }
}
