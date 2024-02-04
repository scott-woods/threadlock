using Nez;
using Nez.Tiled;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Components.TiledComponents
{
    public abstract class TiledComponent : Component
    {
        public TmxObject TmxObject { get; set; }
    }
}
