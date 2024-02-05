using Threadlock.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.StaticData
{
    public class DoorwayMaps
    {
        public static Map ForgeLeftOpen = new Map(Nez.Content.Tiled.Tilemaps.Forge.Doorways.Forge_left_open);
        public static Map ForgeLeftClosed = new Map(Nez.Content.Tiled.Tilemaps.Forge.Doorways.Forge_left_closed);
        public static Map ForgeRightOpen = new Map(Nez.Content.Tiled.Tilemaps.Forge.Doorways.Forge_right_open);
        public static Map ForgeRightClosed = new Map(Nez.Content.Tiled.Tilemaps.Forge.Doorways.Forge_right_closed);
        public static Map ForgeTopOpen = new Map(Nez.Content.Tiled.Tilemaps.Forge.Doorways.Forge_top_open);
        public static Map ForgeTopClosed = new Map(Nez.Content.Tiled.Tilemaps.Forge.Doorways.Forge_top_closed);
        public static Map ForgeBottomOpen = new Map(Nez.Content.Tiled.Tilemaps.Forge.Doorways.Forge_bottom_open);
        public static Map ForgeBottomClosed = new Map(Nez.Content.Tiled.Tilemaps.Forge.Doorways.Forge_bottom_closed);
    }
}
