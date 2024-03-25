using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Entities;
using Threadlock.Models;

namespace Threadlock.StaticData
{
    public class LootTables
    {
        public static List<LootDrop> BasicEnemy
        {
            get
            {
                return new List<LootDrop>()
                {
                    new LootDrop(typeof(HealthOrb), .1f, 1, 1)
                };
            }
        }
    }
}
