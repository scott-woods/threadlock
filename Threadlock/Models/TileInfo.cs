using Microsoft.Xna.Framework;
using Nez.Tiled;
using System;
using static Threadlock.Helpers.TileBitmaskHelper;
using static Threadlock.SceneComponents.Dungenerator.CorridorPainter;

namespace Threadlock.Models
{
    public class TileInfo<TEnum> where TEnum : struct, Enum
    {
        public Vector2 Position { get; set; }
        public int? TileId { get; set; }

        /// <summary>
        /// mask of the terrain within this tile
        /// </summary>
        public int TerrainMask { get; set; }
        
        /// <summary>
        /// mask of the terrain types surrounding this tile
        /// </summary>
        public int PositionalMask { get; set; }
        public int Priority { get; set; }
        public int CombinedMask
        {
            get
            {
                var mask = 0;
                foreach (Corners corner in Enum.GetValues(typeof(Corners)))
                {
                    var bitPos = ((int)corner * GetRequiredBitShift<TEnum>());
                    var posTerrainType = ((PositionalMask >> bitPos) & 0b11);
                    var localTerrainType = ((TerrainMask >> bitPos) & 0b11);
                    var terrainType = posTerrainType == 0 ? localTerrainType : posTerrainType;

                    mask |= ((int)terrainType << bitPos);
                }

                return mask;
            }
        }

        int _shift { get => GetRequiredBitShift<TEnum>(); }
        int _shiftMask { get => GetShiftMask<TEnum>(); }

        /// <summary>
        /// constructor for a new tile
        /// </summary>
        /// <param name="position"></param>
        /// <param name="tileType"></param>
        public TileInfo(Vector2 position, TEnum tileType, int priority)
        {
            Position = position;
            TerrainMask = InitMaskWithType(tileType);
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
            TerrainMask = GetMask<TEnum>(layerTile.TilesetTile);
        }

        public TileInfo(Vector2 position, int tileId, int mask)
        {
            Position = position;
            TileId = tileId;
            TerrainMask = mask;
        }

        public void SetTerrainMaskValue(TEnum terrainType, Corners corner)
        {
            var terrain = GetTerrainInCorner<TEnum>(TerrainMask, corner);
            if (Convert.ToInt32(terrain) != Convert.ToInt32(terrainType))
            {
                TerrainMask &= ~(_shiftMask << ((int)corner * _shift));

                TerrainMask |= (Convert.ToInt32(terrainType) << ((int)corner * _shift));
            }
        }
    }
}
