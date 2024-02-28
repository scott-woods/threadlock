using Nez;
using Nez.Tiled;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components.TiledComponents;
using Threadlock.Helpers;
using Threadlock.StaticData;

namespace Threadlock.Entities
{
    public class DungeonRoomEntity : Entity
    {
        public int RoomId;

        public DungeonRoomEntity(int roomId) : base(roomId.ToString())
        {
            RoomId = roomId;
        }

        #region LIFECYCLE

        public override void OnRemovedFromScene()
        {
            base.OnRemovedFromScene();

            var comps = Scene.FindComponentsOfType<TiledComponent>().Where(c => c.MapEntity == this);
            foreach (var comp in comps)
                comp.Entity.Destroy();
        }

        #endregion

        public void CreateMap(TmxMap map)
        {
            var mapRenderer = AddComponent(new TiledMapRenderer(map, "Walls"));
            mapRenderer.SetLayersToRender(new[] { "Back", "Walls" }.Where(l => map.Layers.Contains(l)).ToArray());
            mapRenderer.RenderLayer = RenderLayers.Back;
            Flags.SetFlagExclusive(ref mapRenderer.PhysicsLayer, PhysicsLayers.Environment);
            TiledHelper.CreateEntitiesForTiledObjects(mapRenderer);

            var frontRenderer = AddComponent(new TiledMapRenderer(map));
            frontRenderer.SetLayersToRender(new[] { "Front", "AboveFront" }.Where(l => map.Layers.Contains(l)).ToArray());
            frontRenderer.RenderLayer = RenderLayers.Front;
        }

        public List<T> FindComponentsOnMap<T>() where T : TiledComponent
        {
            return Scene.FindComponentsOfType<T>().Where(c => c.MapEntity == this).ToList();
        }
    }
}
