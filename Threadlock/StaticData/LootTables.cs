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
                    new LootDrop(LootConfig.HealthOrb, .1f, 1, 1),
                    new LootDrop(LootConfig.Coin, 1f, 3, 5),
                    new LootDrop(LootConfig.BigCoin, .15f, 1, 2),
                    new LootDrop(LootConfig.Dust, .1f, 1, 3)
                };
            }
        }
    }
}
