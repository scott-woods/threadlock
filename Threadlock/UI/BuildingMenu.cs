using Nez;
using Nez.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Models;
using Threadlock.SaveData;

namespace Threadlock.UI
{
    public class BuildingMenu : Component, IUpdatable
    {
        //elements
        Window _root;
        Label _itemNameLabel;
        Label _itemCountLabel;

        Building _building;
        FactoryItemStack _itemStack;

        public BuildingMenu(Building building, FactoryItemStack itemStack)
        {
            _building = building;
            _itemStack = itemStack;
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            var skin = Skin.CreateDefaultSkin();

            _root = new Window("Building", skin);
            _root.SetSize(250, 250);
            _root.PadTop(50);

            var table = new Table();
            _root.Add(table).Grow();

            _itemNameLabel = new Label($"{_itemStack.Item.Name}: ", skin);
            table.Add(_itemNameLabel).Left().Top().Pad(10);

            _itemCountLabel = new Label($"{_itemStack.Count}", skin);
            table.Add(_itemCountLabel).Left().Top().Pad(10);

            table.Row();

            var moveButton = new TextButton("Move", skin);
            moveButton.OnClicked += (button) =>
            {
                _building.Pickup();
                CloseMenu();
            };
            table.Add(moveButton).Left().Top().Pad(10);

            _itemStack.CountChanged += OnItemCountChanged;

            if (Entity.TryGetComponent<UICanvas>(out var canvas))
                canvas.Stage.AddElement(_root);
        }

        void OnItemCountChanged(int itemCount)
        {
            _itemCountLabel.SetText(itemCount.ToString());
        }

        public override void OnRemovedFromEntity()
        {
            base.OnRemovedFromEntity();

            _root.Remove();

            _itemStack.CountChanged -= OnItemCountChanged;
        }

        public void Update()
        {
            if (Controls.Instance.AltAttack.IsPressed)
                CloseMenu();
        }

        void CloseMenu()
        {
            this.RemoveComponent();
        }
    }
}
