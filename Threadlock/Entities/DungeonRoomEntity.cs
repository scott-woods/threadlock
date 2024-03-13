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

        TextComponent _textComponent;

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
                var width = Map != null ? Map.WorldWidth : 0;
                var height = Map != null ? Map.WorldHeight : 0;
                return new RectangleF(Position.X, Position.Y, width, height);
            }
        }

        List<int> _childrenIds = new List<int>();

        public DungeonRoomEntity(DungeonComposite composite, DungeonNode dungeonNode) : base(dungeonNode.Id.ToString())
        {
            ParentComposite = composite;
            RoomId = dungeonNode.Id;
            Type = dungeonNode.Type;
            _childrenIds = dungeonNode.Children.Select(c => c.ChildNodeId).ToList();
            _textComponent = AddComponent(new TextComponent(Graphics.Instance.BitmapFont, $"{dungeonNode.Id}", Vector2.Zero, Color.Black));
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

            _textComponent.SetLocalOffset(new Vector2(map.WorldWidth / 2, map.WorldHeight / 2));
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

        public bool OverlapsRoom(List<Vector2> positions, out List<Vector2> overlappingPositions, bool checkDoorways = true)
        {
            overlappingPositions = new List<Vector2>();

            //if no collision layer, can't overlap
            var roomRenderer = GetComponents<TiledMapRenderer>().FirstOrDefault(r => r.CollisionLayer != null);
            if (roomRenderer == null)
                return false;

            //check if bounds intersect
            var minPos = new Vector2(positions.Select(p => p.X).Min(), positions.Select(p => p.Y).Min());
            var maxPos = new Vector2(positions.Select(p => p.X).Max(), positions.Select(p => p.Y).Max());
            var rect = new RectangleF(minPos, maxPos - minPos);
            if (!Bounds.Intersects(rect))
                return false;

            foreach (var tile in roomRenderer.CollisionLayer.Tiles.Where(t => t != null))
            {
                var tileWorldPos = Position + new Vector2(tile.X * 16, tile.Y * 16);
                var matchingTiles = positions.Where(p => p == tileWorldPos);
                if (matchingTiles.Any())
                {
                    overlappingPositions.AddRange(matchingTiles);
                }
            }

            if (checkDoorways)
            {
                var doorways = FindComponentsOnMap<DungeonDoorway>();
                foreach (var doorway in doorways)
                {
                    for (int y = 0; y < doorway.TmxObject.Height / 16; y++)
                    {
                        for (int x = 0; x < doorway.TmxObject.Width / 16; x++)
                        {
                            var doorwayTileWorldPos = doorway.Entity.Position + new Vector2(x * 16, y * 16);
                            var matchingTiles = positions.Where(p => p == doorwayTileWorldPos);
                            if (matchingTiles.Any())
                                overlappingPositions.AddRange(matchingTiles);
                        }
                    }
                }
            }

            return overlappingPositions.Any();
        }

        public bool OverlapsRoom(DungeonRoomEntity otherRoom, bool checkDoorways = true)
        {
            //if there is no overlap, continue
            if (!Bounds.Intersects(otherRoom.Bounds))
                return false;

            var otherRoomRenderer = otherRoom.GetComponents<TiledMapRenderer>().FirstOrDefault(r => r.CollisionLayer != null);
            if (otherRoomRenderer == null)
                return false;

            var roomRenderer = GetComponents<TiledMapRenderer>().FirstOrDefault(r => r.CollisionLayer != null);
            if (roomRenderer == null)
                return false;


            var otherRoomCollisionRect = GetCollisionLayerRect(otherRoomRenderer);
            var roomCollisionRect = GetCollisionLayerRect(roomRenderer);

            if (roomCollisionRect.Intersects(otherRoomCollisionRect))
                return true;

            List<Vector2> tilePositions = new List<Vector2>();
            List<Vector2> otherRoomTilePositions = new List<Vector2>();

            if (checkDoorways)
            {
                //var doorways = FindComponentsOnMap<DungeonDoorway>();
                //foreach (var doorway in doorways)
                //{
                //    for (int y = 0; y < doorway.TmxObject.Height / 16; y++)
                //    {
                //        for (int x = 0; x < doorway.TmxObject.Width / 16; x++)
                //        {
                //            var doorwayTileWorldPos = doorway.Entity.Position + new Vector2(x * 16, y * 16);
                //            tilePositions.Add(doorwayTileWorldPos);
                //        }
                //    }
                //}

                //var otherDoorways = otherRoom.FindComponentsOnMap<DungeonDoorway>();
                //foreach (var doorway in otherDoorways)
                //{
                //    for (int y = 0; y < doorway.TmxObject.Height / 16; y++)
                //    {
                //        for (int x = 0; x < doorway.TmxObject.Width / 16; x++)
                //        {
                //            var doorwayTileWorldPos = doorway.Entity.Position + new Vector2(x * 16, y * 16);
                //            otherRoomTilePositions.Add(doorwayTileWorldPos);
                //        }
                //    }
                //}
            }

            //return tilePositions.Any(t => otherRoomTilePositions.Contains(t));

            return false;
        }

        Rectangle GetCollisionLayerRect(TiledMapRenderer renderer)
        {
            if (renderer.CollisionLayer == null)
                return new Rectangle();

            var minX = renderer.CollisionLayer.Tiles.Where(t => t != null).Select(t => t.X).Min();
            var maxX = renderer.CollisionLayer.Tiles.Where(t => t != null).Select(t => t.X).Max();
            var minY = renderer.CollisionLayer.Tiles.Where(t => t != null).Select(t => t.Y).Min();
            var maxY = renderer.CollisionLayer.Tiles.Where(t => t != null).Select(t => t.Y).Max();

            var rect = new Rectangle(minX, minY, (maxX - minX) * 16, (maxY - minY) * 16);
            rect.Location += renderer.Entity.Position.ToPoint();
            return rect;
        }
    }
}
