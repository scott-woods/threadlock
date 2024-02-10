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
        /// given a direction, returns an empty string for left and right, Down for an angle facing down, and Up for an angle facing up
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static string GetDirectionStringByVector(Vector2 direction)
        {
            var angle = Math.Atan2(direction.Y, direction.X) * Mathf.Rad2Deg;
            angle = (angle + 360) % 360;
            var directionString = "";
            if (angle >= 45 && angle < 135) directionString = "Down";
            else if (angle >= 225 && angle < 315) directionString = "Up";

            return directionString;
        }
    }
}
