using Threadlock.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.StaticData
{
    public class HallwayMaps
    {
        public static Map ForgeHorizontal = new Map(Nez.Content.Tiled.Tilemaps.Forge.Halls.Forge_horizontal);
        public static Map ForgeVertical = new Map(Nez.Content.Tiled.Tilemaps.Forge.Halls.Forge_vertical);
        public static Map ForgeBottomLeft = new Map(Nez.Content.Tiled.Tilemaps.Forge.Halls.Forge_bottom_left_corner);
        public static Map ForgeBottomRight = new Map(Nez.Content.Tiled.Tilemaps.Forge.Halls.Forge_bottom_right_corner);
        public static Map ForgeTopLeft = new Map(Nez.Content.Tiled.Tilemaps.Forge.Halls.Forge_top_left_corner);
        public static Map ForgeTopRight = new Map(Nez.Content.Tiled.Tilemaps.Forge.Halls.Forge_top_right_corner);
    }
}
