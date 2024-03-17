using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Models;

namespace Threadlock.StaticData
{
    public class Maps
    {
        public static List<Map> ForgeMaps = new List<Map>()
        {
            new Map(Nez.Content.Tiled.Tilemaps.Forge.Forge_simple_3)
        };
    }
}
