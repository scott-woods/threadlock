using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Models;

namespace Threadlock.Components
{
    public class InputSlot : Component
    {
        public Func<FactoryItem, bool> ReceiveItemHandler;
        public Vector2 Position;

        public Vector2 GridPosition { get => _building.GridPosition + Position; }

        //components
        Building _building;

        public InputSlot(Func<FactoryItem, bool> receiveItemHandler)
        {
            ReceiveItemHandler = receiveItemHandler;
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            _building = Entity.GetComponent<Building>();
        }

        public bool TryReceiveItem(FactoryItem item)
        {
            return ReceiveItemHandler.Invoke(item);
        }
    }
}
