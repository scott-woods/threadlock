using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Models;

namespace Threadlock.Entities.Characters.Player
{
    public class PlayerResources : Component
    {
        public Dictionary<FactoryResource, int> Resources;
    }
}
