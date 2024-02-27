using Microsoft.Xna.Framework;
using Nez;
using Nez.Tiled;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components.TiledComponents;

namespace Threadlock.Models
{
    public class DungeonNode
    {
        public int Id;
        public string Type;
        public List<DungeonConnection> Children;
        public Vector2 Position;
        public Entity Entity;
        public TmxMap Map;
        public List<DungeonDoorway> Doorways;

        public void CreateMap()
        {
            //TODO: get map options by type
            var tiledMap = Game1.Scene.Content.LoadTiledMap(Content.Tiled.Tilemaps.Forge.Forge_simple);

            var entity = Game1.Scene.CreateEntity("map");
            var mapRenderer = entity.AddComponent(new TiledMapRenderer(tiledMap));

            var exits = tiledMap.ObjectGroups.SelectMany(g => g.Objects).Where(g => g.Type == "DungeonDoorway").ToList();
        }
    }
}
