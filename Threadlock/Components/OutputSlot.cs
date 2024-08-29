using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Entities;
using Threadlock.StaticData;

namespace Threadlock.Components
{
    public class OutputSlot : Component, IUpdatable
    {
        public Vector2 Position;
        public Vector2 Direction;

        FactoryItemProvider _itemProvider;

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            _itemProvider = Entity.GetComponent<FactoryItemProvider>();
        }

        public void Update()
        {
            DispenseItems();
        }

        public Vector2 GetWorldPosition()
        {
            var localTopLeft = Entity.Position - new Vector2(Entity.GetComponent<SpriteAnimator>().Width / 2, Entity.GetComponent<SpriteAnimator>().Height / 2);
            return localTopLeft + (Position * 16);
        }

        void DispenseItems()
        {
            var item = _itemProvider.GetFactoryItem();
            if (item != null && item.Count > 0)
            {
                var worldPos = GetWorldPosition();
                var testPos = worldPos + (Direction * 16);

                var inputSlots = Entity.Scene.FindComponentsOfType<InputSlot>();
                foreach (var inputSlot in inputSlots)
                {
                    if (inputSlot.GetWorldPosition() == testPos)
                    {
                        if (inputSlot.TryReceiveItem(item.Item))
                        {
                            item.Count--;
                        }
                    }
                }
            }
        }
    }
}
