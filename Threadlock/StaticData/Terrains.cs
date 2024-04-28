using System;

namespace Threadlock.StaticData
{
    public class Terrains
    {
        [Flags]
        public enum ForestFloor
        {
            None = 0,
            LightGrass = 1,
            Dirt = 2,
            DarkGrass = 3
        }
    }
}
