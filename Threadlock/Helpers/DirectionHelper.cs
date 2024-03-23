using Microsoft.Xna.Framework;
using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Helpers
{
    public static class DirectionHelper
    {
        /// <summary>
        /// all directions around a point (both horizontal and diagonal)
        /// </summary>
        public static List<Vector2> PrincipleDirections
        {
            get
            {
                return new List<Vector2>()
                {
                    new Vector2(0, -1),
                    new Vector2(1, -1),
                    new Vector2(1, 0),
                    new Vector2(1, 1),
                    new Vector2(0, 1),
                    new Vector2(-1, 1),
                    new Vector2(-1, 0),
                    new Vector2(-1, -1)
                };
            }
        }
        
        /// <summary>
        /// four horizontal directions
        /// </summary>
        public static List<Vector2> CardinalDirections
        {
            get
            {
                return new List<Vector2>()
                {
                    new Vector2(0, -1),
                    new Vector2(1, 0),
                    new Vector2(0, 1),
                    new Vector2(-1, 0)
                };
            }
        }

        /// <summary>
        /// four diagonal directions
        /// </summary>
        public static List<Vector2> OrdinalDirections
        {
            get
            {
                return new List<Vector2>()
                {
                    new Vector2(1, -1),
                    new Vector2(1, 1),
                    new Vector2(-1, 1),
                    new Vector2(-1, -1)
                };
            }
        }

        public static Vector2 Up { get => new Vector2(0, -1); }
        public static Vector2 Down { get => new Vector2(0, 1); }
        public static Vector2 Left { get => new Vector2(-1, 0); }
        public static Vector2 Right { get => new Vector2(1, 0); }
        public static Vector2 UpLeft { get => new Vector2(-1, -1); }
        public static Vector2 DownLeft { get => new Vector2(-1, 1); }
        public static Vector2 UpRight { get => new Vector2(1, -1); }
        public static Vector2 DownRight { get => new Vector2(1, 1); }

        /// <summary>
        /// given a direction, returns an empty string for left and right, Down for an angle facing down, and Up for an angle facing up
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static string GetDirectionStringByVector(Vector2 direction, bool includeHorizontal = false)
        {
            var angle = Math.Atan2(direction.Y, direction.X) * Mathf.Rad2Deg;
            angle = (angle + 360) % 360;
            var directionString = "";
            if (angle >= 45 && angle < 135) directionString = "Down";
            else if (angle >= 225 && angle < 315) directionString = "Up";

            if (includeHorizontal)
            {
                if (angle >= 135 && angle < 225) directionString = "Left";
                else if (angle >= 315 || angle < 45) directionString = "Right";
            }

            return directionString;
        }
    }
}
