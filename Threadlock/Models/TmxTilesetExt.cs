using Nez.Tiled;
using System;
using System.Collections.Generic;
using System.Linq;
using Threadlock.Helpers;

namespace Threadlock.Models
{
    public class TmxTilesetExt
    {
        public TmxTileset Tileset;
        public Dictionary<Type, Dictionary<int, List<int>>> TerrainDictionaries = new Dictionary<Type, Dictionary<int, List<int>>>();
        public Dictionary<int, List<ExtraTile>> ExtraTileDict = new Dictionary<int, List<ExtraTile>>();

        public static TmxTilesetExt CreateFromTileset(TmxTileset tileset)
        {
            var tilesetExt = new TmxTilesetExt();

            tilesetExt.Tileset = tileset;

            foreach (var terrainSet in tileset.TerrainSets)
            {
                //key is a specific mask, value is list of tile ids with that mask
                Dictionary<int, List<int>> terrainDict = new Dictionary<int, List<int>>();

                Type enumType = Type.GetType($"Threadlock.StaticData.Terrains+{terrainSet.Name}");
                if (enumType == null || !enumType.IsEnum)
                    continue;

                foreach (var tile in terrainSet.Tiles)
                {
                    var mask = TileBitmaskHelper.GetMask(enumType, tile);
                    if (!terrainDict.ContainsKey(mask))
                        terrainDict.Add(mask, new List<int>());
                    terrainDict[mask].Add(tile.TileId);
                }

                tilesetExt.TerrainDictionaries.Add(enumType, terrainDict);
            }

            //get tileset tile mask dictionary 
            foreach (var tile in tileset.Tiles)
            {
                //if (tile.Value.Type == "TerrainTile")
                //{
                //    if (tile.Value.Properties != null)
                //    {
                //        var tilesetTileMask = TileBitmaskHelper.GetMask<TEnum>(tile.Value);
                //        if (!tileDict.ContainsKey(tilesetTileMask))
                //            tileDict.Add(tilesetTileMask, new List<int>());
                //        tileDict[tilesetTileMask].Add(tile.Key);
                //    }
                //}
                //else if (tile.Value.Type == "WallTile")
                //{
                //    if (tile.Value.Properties != null)
                //    {
                //        var tilesetTileMask = TileBitmaskHelper.GetMask<WallTileType>(tile.Value);
                //        if (!wallTileDict.ContainsKey(tilesetTileMask))
                //            wallTileDict.Add(tilesetTileMask, new List<int>());
                //        wallTileDict[tilesetTileMask].Add(tile.Key);
                //    }
                //}
                if (tile.Value.Type == "ExtraTile")
                {
                    if (tile.Value.Properties != null)
                    {
                        if (tile.Value.Properties.TryGetValue("ParentTileIds", out var parentIds)
                            && tile.Value.Properties.TryGetValue("Layer", out var layerName)
                            && tile.Value.Properties.TryGetValue("Offset", out var offset))
                        {
                            var splitParentIds = parentIds.Split(' ').Select(i => Convert.ToInt32(i)).ToList();
                            foreach (var parentId in splitParentIds)
                            {
                                if (!tilesetExt.ExtraTileDict.ContainsKey(parentId))
                                    tilesetExt.ExtraTileDict.Add(parentId, new List<ExtraTile>());
                                tilesetExt.ExtraTileDict[parentId].Add(new ExtraTile(tile.Key, layerName, offset));
                            }
                        }
                    }
                }
            }

            return tilesetExt;
        }
    }
}
