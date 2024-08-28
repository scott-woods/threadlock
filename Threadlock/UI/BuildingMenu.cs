using Nez;
using Nez.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.SaveData;

namespace Threadlock.UI
{
    public class BuildingMenu : Component, IUpdatable
    {
        Window _root;

        string _name;

        public BuildingMenu(string buildingName)
        {
            _name = buildingName;
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            var skin = Skin.CreateDefaultSkin();

            _root = new Window(_name, skin);
            _root.SetSize(250, 250);
            _root.PadTop(50);

            if (Entity.TryGetComponent<UICanvas>(out var canvas))
                canvas.Stage.AddElement(_root);
        }

        public override void OnRemovedFromEntity()
        {
            base.OnRemovedFromEntity();

            _root.Remove();
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
