using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Models
{
    /// <summary>
    /// represents a possible item drop, provided a config, drop chance, and min/max quantity possible
    /// </summary>
    public class LootDrop
    {
        public LootConfig LootConfig { get; set; }
        public float DropChance { get; set; }
        public int MinQuantity { get; set; }
        public int MaxQuantity { get; set; }

        public LootDrop(LootConfig lootConfig, float dropChance, int minQuantity, int maxQuantity)
        {
            LootConfig = lootConfig;
            DropChance = dropChance;
            MinQuantity = minQuantity;
            MaxQuantity = maxQuantity;
        }
    }
}
