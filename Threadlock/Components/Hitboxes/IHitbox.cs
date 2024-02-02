using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Components.Hitboxes
{
    public interface IHitbox
    {
        int Damage { get; set; }
        float PushForce { get; set; }
        Vector2 Direction { get; set; }
        string AttackId { get; set; }
    }
}
