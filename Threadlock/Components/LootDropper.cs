﻿using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Entities;
using Threadlock.Models;

namespace Threadlock.Components
{
    public class LootDropper : Component
    {
        List<LootDrop> _lootTable = new List<LootDrop>();

        public LootDropper(List<LootDrop> lootTable)
        {
            _lootTable = lootTable;
        }

        public void DropLoot()
        {
            foreach (var lootDrop in _lootTable)
            {
                if (Nez.Random.Chance(lootDrop.DropChance))
                    SpawnLootItem(lootDrop);
            }
        }

        void SpawnLootItem(LootDrop lootDrop)
        {
            var quantity = Nez.Random.Range(lootDrop.MinQuantity, lootDrop.MaxQuantity);
            for (int i = 0; i < quantity; i++)
            {
                var pos = Entity.TryGetComponent<OriginComponent>(out var oc) ? oc.Origin : Entity.Position;
                var instance = Entity.Scene.AddEntity(new Droppable(lootDrop.LootConfig));
                instance.SetPosition(pos);
            }
        }
    }
}
