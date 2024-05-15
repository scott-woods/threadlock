using Nez;
using Nez.Tiled;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Threadlock.Helpers;

namespace Threadlock.Models
{
    public class TmxTilesetExt
    {
        public TmxTileset Tileset;
        public Dictionary<int, List<ExtraTile>> ExtraTileDict = new Dictionary<int, List<ExtraTile>>();
        public Dictionary<Type, TerrainSetExt> TerrainDictionary = new Dictionary<Type, TerrainSetExt>();

        public TmxTilesetExt(TmxTileset tileset)
        {
            Tileset = tileset;

            //read terrain sets
            if (tileset.TerrainSets != null)
            {
                foreach (var terrainSet in tileset.TerrainSets)
                {
                    //check that we have a matching enum for this terrain
                    Type enumType = Type.GetType($"Threadlock.StaticData.Terrains+{terrainSet.Name}");
                    if (enumType == null || !enumType.IsEnum)
                        continue;

                    //get the mask for each tile in this terrain set
                    Dictionary<int, int> tileDictionary = new Dictionary<int, int>(terrainSet.Tiles.Count);
                    Dictionary<int, List<int>> maskDictionary = new Dictionary<int, List<int>>();
                    foreach (var tile in terrainSet.Tiles)
                    {
                        //get the mask
                        var mask = TileBitmaskHelper.GetMask(enumType, tile);

                        //add to tile dictionary
                        tileDictionary.Add(tile.TileId + Tileset.FirstGid, mask);

                        //add to mask dictionary
                        if (!maskDictionary.TryGetValue(mask, out List<int> tileIds))
                        {
                            tileIds = new List<int>();
                            maskDictionary[mask] = tileIds;
                        }
                        tileIds.Add(tile.TileId + Tileset.FirstGid);
                    }

                    //add to terrain sets list
                    var terrainSetExt = new TerrainSetExt() { EnumType = enumType, TileDictionary = tileDictionary, MaskDictionary = maskDictionary };
                    TerrainDictionary.Add(enumType, terrainSetExt);
                }
            }

            //read tiles
            foreach (var tile in tileset.Tiles)
            {
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
                                if (!ExtraTileDict.ContainsKey(parentId + tileset.FirstGid))
                                    ExtraTileDict.Add(parentId + tileset.FirstGid, new List<ExtraTile>());
                                ExtraTileDict[parentId + tileset.FirstGid].Add(new ExtraTile(tile.Key + tileset.FirstGid, layerName, offset));
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// get a terrain set by enum type
        /// </summary>
        /// <param name="enumType"></param>
        /// <param name="terrainSet"></param>
        /// <returns></returns>
        public bool TryGetTerrainSet(Type enumType, out TerrainSetExt terrainSet)
        {
            terrainSet = null;
            if (!enumType.IsEnum)
                return false;

            return TerrainDictionary.TryGetValue(enumType, out terrainSet);
        }

        /// <summary>
        /// given a bitmask, find a tile that matches it for a specific terrain
        /// </summary>
        /// <param name="enumType"></param>
        /// <param name="mask"></param>
        /// <param name="tileId"></param>
        /// <returns></returns>
        public bool TryFindTile(Type enumType, int mask, out int tileId)
        {
            tileId = -1;

            if (!enumType.IsEnum)
                return false;

            if (!TryGetTerrainSet(enumType, out var terrainSet))
                return false;

            return terrainSet.TryGetTile(mask, out tileId);
        }

        /// <summary>
        /// find a tile for a specific mask and enum type
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="enumType"></param>
        /// <returns></returns>
        //public int FindMatchingTile(int mask, Type enumType)
        //{
        //    var terrainSet = TerrainSets.FirstOrDefault(t => t.EnumType == enumType);
        //    if (terrainSet == null)
        //        return -1;

        //    var tileIds = terrainSet.TileDictionary.Where(x => x.Value == mask).Select(x => x.Key).ToList();
        //    if (tileIds.Count > 0)
        //        return tileIds.RandomItem();

        //    return -1;
        //}

        //public static TmxTilesetExt CreateFromTileset(TmxTileset tileset)
        //{
        //    var tilesetExt = new TmxTilesetExt();

        //    tilesetExt.Tileset = tileset;

        //    foreach (var terrainSet in tileset.TerrainSets)
        //    {
        //        //key is a specific mask, value is list of tile ids with that mask
        //        Dictionary<int, List<int>> terrainDict = new Dictionary<int, List<int>>();

        //        Type enumType = Type.GetType($"Threadlock.StaticData.Terrains+{terrainSet.Name}");
        //        if (enumType == null || !enumType.IsEnum)
        //            continue;

        //        foreach (var tile in terrainSet.Tiles)
        //        {
        //            var mask = TileBitmaskHelper.GetMask(enumType, tile);
        //            if (!terrainDict.ContainsKey(mask))
        //                terrainDict.Add(mask, new List<int>());
        //            terrainDict[mask].Add(tile.TileId);
        //        }

        //        tilesetExt.TerrainDictionaries.Add(enumType, terrainDict);
        //    }

        //    //get tileset tile mask dictionary 
        //    foreach (var tile in tileset.Tiles)
        //    {
        //        //if (tile.Value.Type == "TerrainTile")
        //        //{
        //        //    if (tile.Value.Properties != null)
        //        //    {
        //        //        var tilesetTileMask = TileBitmaskHelper.GetMask<TEnum>(tile.Value);
        //        //        if (!tileDict.ContainsKey(tilesetTileMask))
        //        //            tileDict.Add(tilesetTileMask, new List<int>());
        //        //        tileDict[tilesetTileMask].Add(tile.Key);
        //        //    }
        //        //}
        //        //else if (tile.Value.Type == "WallTile")
        //        //{
        //        //    if (tile.Value.Properties != null)
        //        //    {
        //        //        var tilesetTileMask = TileBitmaskHelper.GetMask<WallTileType>(tile.Value);
        //        //        if (!wallTileDict.ContainsKey(tilesetTileMask))
        //        //            wallTileDict.Add(tilesetTileMask, new List<int>());
        //        //        wallTileDict[tilesetTileMask].Add(tile.Key);
        //        //    }
        //        //}
        //        if (tile.Value.Type == "ExtraTile")
        //        {
        //            if (tile.Value.Properties != null)
        //            {
        //                if (tile.Value.Properties.TryGetValue("ParentTileIds", out var parentIds)
        //                    && tile.Value.Properties.TryGetValue("Layer", out var layerName)
        //                    && tile.Value.Properties.TryGetValue("Offset", out var offset))
        //                {
        //                    var splitParentIds = parentIds.Split(' ').Select(i => Convert.ToInt32(i)).ToList();
        //                    foreach (var parentId in splitParentIds)
        //                    {
        //                        if (!tilesetExt.ExtraTileDict.ContainsKey(parentId))
        //                            tilesetExt.ExtraTileDict.Add(parentId, new List<ExtraTile>());
        //                        tilesetExt.ExtraTileDict[parentId].Add(new ExtraTile(tile.Key, layerName, offset));
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    return tilesetExt;
        //}
    }
}
