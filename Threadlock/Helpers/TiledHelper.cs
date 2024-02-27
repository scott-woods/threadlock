using Microsoft.Xna.Framework;
using Nez;
using Nez.Tiled;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components.TiledComponents;

namespace Threadlock.Helpers
{
    public static class TiledHelper
    {
        public static void CreateEntitiesForTiledObjects(TiledMapRenderer mapRenderer)
        {
            foreach (var obj in mapRenderer.TiledMap.ObjectGroups.SelectMany(g => g.Objects))
            {
                if (string.IsNullOrWhiteSpace(obj.Type)) return;
                var type = Type.GetType("Threadlock.Components.TiledComponents." + obj.Type);
                var instance = Activator.CreateInstance(type) as TiledComponent;
                instance.TmxObject = obj;
                instance.MapEntity = mapRenderer.Entity;

                var position = new Vector2();
                switch (obj.ObjectType)
                {
                    case TmxObjectType.Basic:
                    case TmxObjectType.Ellipse:
                    case TmxObjectType.Tile:
                        //position = mapRenderer.Entity.Position + new Vector2(obj.X + obj.Width / 2, obj.Y + obj.Height / 2);
                        position = new Vector2(obj.X, obj.Y);
                        break;
                    case TmxObjectType.Polygon:
                        //position = mapRenderer.Entity.Position + new Vector2(obj.X, obj.Y);
                        position = new Vector2(obj.X, obj.Y);
                        break;
                    default:
                        //position = mapRenderer.Entity.Position + new Vector2(obj.X, obj.Y);
                        position = new Vector2(obj.X, obj.Y);
                        break;

                }

                var entity = mapRenderer.Entity.Scene.CreateEntity(obj.Name, position);
                entity.SetParent(mapRenderer.Entity);
                entity.AddComponent(instance);
            }
        }
    }
}
