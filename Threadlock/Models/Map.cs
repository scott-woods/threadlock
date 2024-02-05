using Nez.Tiled;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Models
{
    public class Map
    {
        public string Name { get; set; }
        public TmxMap TmxMap
        {
            get => Game1.Scene.Content.LoadTiledMap(Name);
        }

        public Map(string name)
        {
            Name = name;
        }
    }
}
