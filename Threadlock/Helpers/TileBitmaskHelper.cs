using Nez.Tiled;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Threadlock.SceneComponents.Dungenerator.CorridorPainter;
using Threadlock.Models;
using Microsoft.Xna.Framework;
using Threadlock.StaticData;

namespace Threadlock.Helpers
{
    /// <summary>
    /// static methods for bitwise operations
    /// </summary>
    public static class TileBitmaskHelper
    {
        public static readonly Dictionary<Corners, Corners> MatchingCornersDict = new Dictionary<Corners, Corners>()
        {
            [Corners.TopLeft] = Corners.BottomRight,
            [Corners.TopRight] = Corners.BottomLeft,
            [Corners.BottomLeft] = Corners.TopRight,
            [Corners.BottomRight] = Corners.TopLeft,
        };

        public static readonly Dictionary<Corners, Vector2> CornerDirectionDict = new Dictionary<Corners, Vector2>()
        {
            [Corners.TopLeft] = DirectionHelper.UpLeft,
            [Corners.TopRight] = DirectionHelper.UpRight,
            [Corners.BottomLeft] = DirectionHelper.DownLeft,
            [Corners.BottomRight] = DirectionHelper.DownRight,
        };

        public static int GetMask(Type enumType, TmxWangTile tile)
        {
            if (!enumType.IsEnum)
                return 0;

            int mask = 0;
            var shift = GetRequiredBitShift(enumType);

            var cornerValues = tile.WangId.Where((item, index) => index % 2 != 0).Select(i => Convert.ToInt32(i)).ToList();
            for (int i = 0; i < cornerValues.Count; i++)
            {
                var value = cornerValues[i];
                var corner = (Corners)i;

                var bitPos = (int)corner * shift;

                mask |= (value << bitPos);
            }

            //foreach (var pair in tile.CornerTerrains)
            //{
            //    if (Enum.TryParse<Corners>(pair.Key, out Corners corner))
            //    {
            //        var bitPos = (int)corner * shift;

            //        //parse string to enum
            //        if (Enum.TryParse(enumType, pair.Value, out var tileType))
            //            mask |= (Convert.ToInt32(tileType) << bitPos);
            //    }
            //}

            return mask;
        }

        /// <summary>
        /// Get bitmask of the terrain in a given tile
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="tile"></param>
        /// <returns></returns>
        public static int GetMask<TEnum>(TmxTilesetTile tile) where TEnum : struct, Enum
        {
            var shift = GetRequiredBitShift<TEnum>();
            int mask = 0;

            if (tile == null)
                return mask;

            if (tile.Properties != null)
            {
                foreach (Corners corner in Enum.GetValues(typeof(Corners)))
                {
                    var bitPos = (int)corner * shift;

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

        /// <summary>
        /// get a bitmask where every corner is set to a specific terrain
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="tileType"></param>
        /// <returns></returns>
        public static int InitMaskWithType<TEnum>(TEnum tileType) where TEnum : struct, Enum
        {
            var shift = GetRequiredBitShift<TEnum>();
            var mask = 0;

            foreach (Corners corner in Enum.GetValues(typeof(Corners)))
            {
                var bitPos = (int)corner * shift;

                mask |= (Convert.ToInt32(tileType) << bitPos);
            }

            return mask;
        }

        public static int GetPositionalMask<TEnum>(TileInfo<TEnum> currentTile, List<TileInfo<TEnum>> allTiles) where TEnum : struct, Enum
        {
            var mask = 0;
            var shift = GetRequiredBitShift<TEnum>();

            foreach (Corners corner in Enum.GetValues(typeof(Corners)))
            {
                var bitPos = (int)corner * shift;

                var dir = CornerDirectionDict[corner];
                var tilePos = currentTile.Position + (dir * 16);
                var tile = allTiles.FirstOrDefault(t => t.Position == tilePos);
                if (tile != null)
                {
                    //extract terrain type of neighboring tile
                    var terrainType = GetTerrainInCorner<TEnum>(tile.TerrainMask, MatchingCornersDict[corner]);

                    //apply it to the corner of the current tile
                    mask |= (Convert.ToInt32(terrainType) << (Convert.ToInt32(corner) * shift));
                }
                else
                    mask |= (0 << (Convert.ToInt32(corner) * shift));
            }

            return mask;
        }

        public static TEnum GetTerrainInCorner<TEnum>(int mask, Corners corner) where TEnum : struct, Enum
        {
            var shift = GetRequiredBitShift<TEnum>();
            var shiftMask = GetShiftMask<TEnum>();

            var maskSection = ((mask >> ((int)corner * shift)) & shiftMask);

            if (Enum.IsDefined(typeof(TEnum), maskSection))
                return (TEnum)Enum.ToObject(typeof(TEnum), maskSection);
            else
                throw new ArgumentException($"The value {maskSection} is not declared in the enum {typeof(TEnum)}.");
        }

        public static int GetRequiredBitShift<TEnum>() where TEnum : struct, Enum
        {
            int maxEnumValue = Convert.ToInt32(Enum.GetValues(typeof(TEnum)).Cast<TEnum>().Max());
            int bitsNeeded = (int)Math.Ceiling(Math.Log2(maxEnumValue + 1));
            return bitsNeeded;
        }

        public static int GetRequiredBitShift(Type enumType)
        {
            if (!enumType.IsEnum)
                return 0;

            int maxEnumValue = Convert.ToInt32(Enum.GetValues(enumType).Cast<int>().Max());
            int bitsNeeded = (int)Math.Ceiling(Math.Log2(maxEnumValue + 1));
            return bitsNeeded;
        }

        public static int GetShiftMask<TEnum>() where TEnum : struct, Enum
        {
            var shift = GetRequiredBitShift<TEnum>();
            return (1 << shift) - 1;
        }
    }
}
