using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Components.TiledComponents
{
    public class DoorwayPoint : TiledComponent
    {
        public DoorwayPointDirection Direction;

        public override void Initialize()
        {
            base.Initialize();

            if (TmxObject.Properties != null && TmxObject.Properties.TryGetValue("Direction", out var dir))
            {
                Direction = dir switch
                {
                    "Up" => DoorwayPointDirection.Up,
                    "Down" => DoorwayPointDirection.Down,
                    "Left" => DoorwayPointDirection.Left,
                    "Right" => DoorwayPointDirection.Right,
                    _ => DoorwayPointDirection.None,
                };
            }
        }
    }

    public enum DoorwayPointDirection
    {
        None,
        Up,
        Down,
        Left,
        Right
    }
}
