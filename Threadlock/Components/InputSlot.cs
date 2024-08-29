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

        public InputSlot(Func<FactoryItem, bool> receiveItemHandler)
        {
            ReceiveItemHandler = receiveItemHandler;
        }

        public Vector2 GetWorldPosition()
        {
            var localTopLeft = Entity.Position - new Vector2(Entity.GetComponent<SpriteAnimator>().Width / 2, Entity.GetComponent<SpriteAnimator>().Height / 2);
            return localTopLeft + (Position * 16);
        }

        public bool TryReceiveItem(FactoryItem item)
        {
            return ReceiveItemHandler.Invoke(item);
        }
    }
}
