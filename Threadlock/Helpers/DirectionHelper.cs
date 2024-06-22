using Microsoft.Xna.Framework;
using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Entities;
using static Threadlock.SceneComponents.Dungenerator.CorridorPainter;

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

        public static Dictionary<string, Vector2> StringDirectionDictionary
        {
            get
            {
                return new Dictionary<string, Vector2>()
                {
                    {"Up", new Vector2(0, -1) },
                    {"Down", new Vector2(0, 1) },
                    {"Left", new Vector2(-1, 0) },
                    {"Right", new Vector2(1, 0) }
                };
            }
        }

        public static Dictionary<Corners, Vector2> CornerDictionary
        {
            get
            {
                return new Dictionary<Corners, Vector2>()
                {
                    { Corners.TopRight, new Vector2(1, -1) },
                    { Corners.BottomRight, new Vector2(1, 1) },
                    { Corners.BottomLeft, new Vector2(-1, 1) },
                    { Corners.TopLeft, new Vector2(-1, -1) }
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

        /// <summary>
        /// expects a string with two numbers, delineated by a space. returns Vector2.Zero if invalid string
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Vector2 GetVectorFromString(string str)
        {
            var splitString = str.Split(' ');
            if (splitString.Length != 2)
                return Vector2.Zero;

            var x = Convert.ToInt32(splitString[0]);
            var y = Convert.ToInt32(splitString[1]);


            return new Vector2(x, y);
        }

        public static float GetDegreesFromDirection(Vector2 direction)
        {
            //get angle in degrees
            float angleInDegrees = (float)Math.Atan2(direction.Y, direction.X) * Mathf.Rad2Deg;

            //make sure angle is in bounds of 360
            if (angleInDegrees < 0)
                angleInDegrees += 360;

            return angleInDegrees;
        }

        public static float GetClampedAngle(Vector2 direction, float maxRotation)
        {
            var rotationRadians = (float)Math.Atan2(direction.Y, direction.X);
            var rotationDegrees = MathHelper.ToDegrees(rotationRadians);

            float actualMinRotation, actualMaxRotation;
            if (direction.X >= 0)
            {
                actualMinRotation = 0;
                actualMaxRotation = maxRotation;
            }
            else
            {
                actualMinRotation = 180 - maxRotation;
                actualMaxRotation = 180;
            }

            var clampedDegrees = Math.Clamp(Math.Abs(rotationDegrees), actualMinRotation, actualMaxRotation);
            rotationRadians = MathHelper.ToRadians(clampedDegrees * Math.Sign(rotationDegrees));

            return rotationRadians;
        }
    }
}
