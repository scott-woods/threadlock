using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Components.TiledComponents
{
    public class PlayerSpawnPoint : TiledComponent
    {
        public string Id;

        public override void Initialize()
        {
            base.Initialize();

            if (TmxObject.Properties.TryGetValue("Id", out var id))
            {
                Id = id;
            }
        }
    }
}
