using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Models;

namespace Threadlock.StaticData
{
    public class Pickups
    {
        public static PickupModel HealthOrb = new PickupModel()
        {
            Sound = Nez.Content.Audio.Sounds.Burp,
            Texture = Nez.Content.Textures.Drops.Health_orb,
            Magnetized = false
        };
    }
}
