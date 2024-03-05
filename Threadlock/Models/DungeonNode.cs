using Microsoft.Xna.Framework;
using Nez;
using Nez.Tiled;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components.TiledComponents;
using Threadlock.Entities;

namespace Threadlock.Models
{
    public class DungeonNode
    {
        public int Id;
        public string Type;
        public List<DungeonConnection> Children;
        public DungeonRoomEntity RoomEntity;
    }
}
