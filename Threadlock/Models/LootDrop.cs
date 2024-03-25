using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Models
{
    public class LootDrop
    {
        public Type LootType { get; set; }
        public float DropChance { get; set; }
        public int MinQuantity { get; set; }
        public int MaxQuantity { get; set; }

        public LootDrop(Type lootType, float dropChance, int minQuantity, int maxQuantity)
        {
            LootType = lootType;
            DropChance = dropChance;
            MinQuantity = minQuantity;
            MaxQuantity = maxQuantity;
        }
    }
}
