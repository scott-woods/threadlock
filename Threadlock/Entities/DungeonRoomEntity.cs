using Microsoft.Xna.Framework;
using Nez;
using Nez.Tiled;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components.TiledComponents;
using Threadlock.Helpers;
using Threadlock.Models;
using Threadlock.StaticData;

namespace Threadlock.Entities
{
    public class DungeonRoomEntity : Entity
    {
        /// <summary>
        /// Id for this room from the Dungeon Flow File
        /// </summary>
        public int RoomId;

        /// <summary>
        /// Room type
        /// </summary>
        public string Type;

        /// <summary>
        /// all children rooms
        /// </summary>
        public List<DungeonRoomEntity> AllChildren
        {
            get
            {
                return Scene.EntitiesOfType<DungeonRoomEntity>().Where(d => _childrenIds.Contains(d.RoomId)).ToList();
            }
        }

        /// <summary>
        /// children rooms in the same composite as this room
        /// </summary>
        public List<DungeonRoomEntity> ChildrenInComposite
        {
            get
            {
                return ParentComposite.RoomEntities.Where(r => _childrenIds.Contains(r.RoomId)).ToList();
            }
        }

        /// <summary>
        /// children rooms outside of this room's composite
        /// </summary>
        public List<DungeonRoomEntity> ChildrenOutsideComposite
        {
            get
            {
                return Scene.EntitiesOfType<DungeonRoomEntity>()
                    .Where(r => _childrenIds.Contains(r.RoomId) && !ParentComposite.RoomEntities.Contains(r))
                    .ToList();
            }
        }

        /// <summary>
        /// The composite this room belongs to
        /// </summary>
        public DungeonComposite ParentComposite;

        public TmxMap Map;

        public RectangleF Bounds
        {
            get
            {
                var width = Map != null ? Map.Width * Map.TileWidth : 0;
                var height = Map != null ? Map.Height * Map.TileHeight : 0;
                return new RectangleF(Position.X, Position.Y, width, height);
            }
        }

        List<int> _childrenIds = new List<int>();

        public DungeonRoomEntity(DungeonComposite composite, DungeonNode dungeonNode)
        {
            ParentComposite = composite;
            RoomId = dungeonNode.Id;
            Type = dungeonNode.Type;
            _childrenIds = dungeonNode.Children.Select(c => c.ChildNodeId).ToList();
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
            Map = map;

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

        public List<TmxMap> GetPossibleMaps()
        {
            //TODO: use type to determine which maps to use
            return new List<TmxMap>()
            {
                Scene.Content.LoadTiledMap(Nez.Content.Tiled.Tilemaps.Forge.Forge_simple_3)
            };
        }

        public void ClearMap()
        {
            Map = null;

            var comps = Scene.FindComponentsOfType<TiledComponent>().Where(c => c.MapEntity == this);
            foreach (var comp in comps)
                comp.Entity.Destroy();
        }

        public bool OverlapsRoom(DungeonRoomEntity otherRoom)
        {
            //if there is no overlap, continue
            if (!Bounds.Intersects(otherRoom.Bounds))
                return false;

            //if there is some overlap, check each tile on the new map
            foreach (var layer in Map.TileLayers)
            {
                //check each non-null tile
                foreach (var tile in layer.Tiles.Where(t => t != null))
                {
                    //get bounds of this tile
                    var tileBounds = new RectangleF(tile.X * Map.TileWidth, tile.Y * Map.TileHeight, Map.TileWidth, Map.TileHeight);
                    tileBounds.X += Position.X;
                    tileBounds.Y += Position.Y;

                    //check if bounds of this tile overlaps any layers in previously placed maps
                    if (otherRoom.Map.TileLayers.Any(l => l.GetTilesIntersectingBounds(tileBounds).Count > 0))
                    {
                        //tiles overlap, return true
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
