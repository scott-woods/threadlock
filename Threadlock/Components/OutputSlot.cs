using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Entities;
using Threadlock.SceneComponents;
using Threadlock.StaticData;

namespace Threadlock.Components
{
    public class OutputSlot : Component, IUpdatable
    {
        public Vector2 Position;
        public Vector2 Direction;

        public Vector2 GridPosition { get => _building.GridPosition + Position; }
        public Vector2 TargetPosition { get => GridPosition + Direction; }

        //components
        FactoryItemProvider _itemProvider;
        Building _building;

        //scene components
        FactoryGrid _factoryGrid;

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            _itemProvider = Entity.GetComponent<FactoryItemProvider>();
            _building = Entity.GetComponent<Building>();

            _factoryGrid = Entity.Scene.GetSceneComponent<FactoryGrid>();
        }

        public void Update()
        {
            DispenseItems();
        }

        void DispenseItems()
        {
            var item = _itemProvider.GetFactoryItem();
            if (item != null && item.Count > 0)
            {
                var targetBuilding = _factoryGrid.GetBuilding(TargetPosition);
                if (targetBuilding != null)
                {
                    var inputSlots = targetBuilding.Entity.GetComponents<InputSlot>();
                    foreach (var inputSlot in inputSlots)
                    {
                        if (inputSlot.GridPosition == TargetPosition)
                        {
                            if (inputSlot.TryReceiveItem(item.Item))
                                item.Count--;

                            break;
                        }
                    }
                }
            }
        }
    }
}
