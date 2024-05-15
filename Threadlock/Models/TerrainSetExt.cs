using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Models
{
    public class TerrainSetExt
    {
        /// <summary>
        /// terrain enum type
        /// </summary>
        public Type EnumType { get; set; }

        /// <summary>
        /// key is the tile id, value is the mask
        /// </summary>
        public Dictionary<int, int> TileDictionary { get; set; }

        /// <summary>
        /// key is a specific bitmask, value is a list of Tile Ids that match that mask
        /// </summary>
        public Dictionary<int, List<int>> MaskDictionary { get; set; } 

        public bool TryGetMask(int id, out int mask)
        {
            return TileDictionary.TryGetValue(id, out mask);
        }

        /// <summary>
        /// given a bitmask, try to find a tile id that matches it
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="tileId"></param>
        /// <returns></returns>
        public bool TryGetTile(int mask, out int tileId)
        {
            tileId = -1;
            if (MaskDictionary.TryGetValue(mask, out var tileIds))
                tileId = tileIds.RandomItem();

            return tileId >= 0;
        }
    }
}
