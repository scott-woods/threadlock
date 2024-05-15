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

        /// <summary>
        /// get the mask of a wang tile given a specific terrain type enum
        /// </summary>
        /// <param name="enumType"></param>
        /// <param name="tile"></param>
        /// <returns></returns>
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

            return mask;
        }

        public static int GetMask<TEnum>(TmxWangTile tile) where TEnum : struct, Enum
        {
            return GetMask(typeof(TEnum), tile);
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
                    var terrainType = GetMaskInCorner<TEnum>(tile.TerrainMask, MatchingCornersDict[corner]);

                    //apply it to the corner of the current tile
                    mask |= (Convert.ToInt32(terrainType) << (Convert.ToInt32(corner) * shift));
                }
                else
                    mask |= (0 << (Convert.ToInt32(corner) * shift));
            }

            return mask;
        }

        public static int GetMaskInCorner(Type enumType, int mask, Corners corner)
        {
            var shift = GetRequiredBitShift(enumType);
            var shiftMask = GetShiftMask(enumType);

            var maskSection = ((mask >> ((int)corner * shift)) & shiftMask);

            return maskSection;
        }

        public static TEnum GetMaskInCorner<TEnum>(int mask, Corners corner) where TEnum : struct, Enum
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
            return GetRequiredBitShift(typeof(TEnum));
        }

        /// <summary>
        /// get the size of the bit sections based on enum type
        /// </summary>
        /// <param name="enumType"></param>
        /// <returns></returns>
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
            return GetShiftMask(typeof(TEnum));
        }

        public static int GetShiftMask(Type enumType)
        {
            if (!enumType.IsEnum)
                return -1;

            var shift = GetRequiredBitShift(enumType);
            return (1 << shift) - 1;
        }

        public static void SetMaskInCorner<TEnum>(TEnum maskValue, Corners corner, ref int mask) where TEnum : struct, Enum
        {
            var shift = GetRequiredBitShift<TEnum>();
            var shiftMask = GetShiftMask<TEnum>();

            mask &= ~(shiftMask << ((int)corner * shift));

            mask |= (Convert.ToInt32(maskValue) << ((int)corner * shift));
        }

        public static int ReplaceZerosWithEnumValue<TEnum>(TEnum maskValue, int mask) where TEnum : struct, Enum
        {
            var replaceMask = 0;
            
            foreach (Corners corner in Enum.GetValues(typeof(Corners)))
            {
                var maskInCorner = GetMaskInCorner<TEnum>(mask, corner);
                if (Convert.ToInt32(maskInCorner) == 0)
                    SetMaskInCorner<TEnum>(maskValue, corner, ref replaceMask);
                else
                    SetMaskInCorner<TEnum>(maskInCorner, corner, ref replaceMask);
            }

            return replaceMask;
        }
    }
}
