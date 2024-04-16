using Microsoft.Xna.Framework;
using Nez.Tiled;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Threadlock.SceneComponents.Dungenerator.CorridorPainter;

namespace Threadlock.Models
{
    public class TileInfo
    {
        public Vector2 Position { get; set; }
        public int? TileId { get; set; }
        public ForestTileType TileType { get; set; }

        /// <summary>
        /// mask of the terrain within this tile
        /// </summary>
        public int Mask { get; set; }
        
        /// <summary>
        /// mask of the terrain types surrounding this tile
        /// </summary>
        public int PositionalMask { get; set; }
        public int Priority { get; set; }
        public int MaskIgnoringNone
        {
            get
            {
                var mask = 0;
                foreach (Corners corner in Enum.GetValues(typeof(Corners)))
                {
                    var posTerrainType = (ForestTileType)((PositionalMask >> (int)corner * 2) & 0b11);
                    var localTerrainType = (ForestTileType)((Mask >> (int)corner * 2) & 0b11);
                    var terrainType = posTerrainType == ForestTileType.None ? localTerrainType : posTerrainType;

                    mask |= ((int)terrainType << (int)corner * 2);
                }

                return mask;
            }
        }

        /// <summary>
        /// constructor for a new tile
        /// </summary>
        /// <param name="position"></param>
        /// <param name="tileType"></param>
        public TileInfo(Vector2 position, ForestTileType tileType, int priority)
        {
            Position = position;
            TileType = tileType;
            Mask = GetMask(tileType);
            Priority = priority;
        }

        /// <summary>
        /// constructor for an existing tile
        /// </summary>
        /// <param name="layerTile"></param>
        public TileInfo(TmxLayerTile layerTile)
        {
            Position = new Vector2(layerTile.X * layerTile.Tileset.TileWidth, layerTile.Y * layerTile.Tileset.TileHeight);
            TileId = layerTile.Gid;
            Mask = GetMask<ForestTileType>(layerTile.TilesetTile);
        }

        public static int GetMask<TEnum>(TmxTilesetTile tile) where TEnum : struct, Enum
        {
            int mask = 0;

            if (tile == null)
                return mask;

            if (tile.Properties != null)
            {
                foreach (Corners corner in Enum.GetValues(typeof(Corners)))
                {
                    var bitPos = (int)corner * 2;

                    if (tile.Properties.TryGetValue(corner.ToString(), out var tileTypeString))
                    {
                        //parse string to enum
                        if (Enum.TryParse<TEnum>(tileTypeString, out var tileType))
                            mask |= (Convert.ToInt32(tileType) << bitPos);
                    }
                }
            }

            return mask;
        }

        int GetMask(ForestTileType tileType)
        {
            var mask = 0;

            foreach (Corners corner in Enum.GetValues(typeof(Corners)))
            {
                var bitPos = (int)corner * 2;

                mask |= ((int)tileType << bitPos);
            }

            return mask;
        }

        public ForestTileType GetTerrainType(Corners corner, int shift)
        {
            return (ForestTileType)((Mask >> (int)corner * shift) & 0b11);
        }

        public ForestTileType GetPositionalTerrainType(Corners corner, int shift)
        {
            return (ForestTileType)((PositionalMask >> (int)corner * shift) & 0b11);
        }

        public void SetMaskValue(ForestTileType terrainType, Corners corner, int shift)
        {
            //var maskForCorner = (1 << ((int)corner * shift)) - 1;

            Mask &= ~(0b11 << (int)corner * shift);

            Mask |= ((int)terrainType << (int)corner * shift);
        }
    }
}
