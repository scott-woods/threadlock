using Microsoft.Xna.Framework;
using Nez;
using Nez.Tiled;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
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
        //public List<DungeonRoomEntity> AllChildren
        //{
        //    get
        //    {
        //        return Scene.EntitiesOfType<DungeonRoomEntity>().Where(d => _childrenIds.Contains(d.RoomId)).ToList();
        //    }
        //}

        public List<DungeonRoomEntity> AllChildren = new List<DungeonRoomEntity>();

        /// <summary>
        /// children rooms in the same composite as this room
        /// </summary>
        public List<DungeonRoomEntity> ChildrenInComposite
        {
            get
            {
                return ParentComposite.RoomEntities.Where(r => ChildrenIds.Contains(r.RoomId)).ToList();
            }
        }

        /// <summary>
        /// children rooms outside of this room's composite
        /// </summary>
        public List<DungeonRoomEntity> ChildrenOutsideComposite
        {
            get
            {
                return AllChildren.Where(c => c.ParentComposite != ParentComposite).ToList();
                return Scene.EntitiesOfType<DungeonRoomEntity>()
                    .Where(r => ChildrenIds.Contains(r.RoomId) && !ParentComposite.RoomEntities.Contains(r))
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
                return new RectangleF(Position, new Vector2(width, height));
            }
        }
        
        public RectangleF CollisionBounds
        {
            get
            {
                var collisionRenderers = GetComponents<TiledMapRenderer>().Where(r => r.CollisionLayer != null);
                var minX = int.MaxValue;
                var maxX = int.MinValue;
                var minY = int.MaxValue;
                var maxY = int.MinValue;
                foreach (var renderer in collisionRenderers)
                {
                    for (int x = 0; x < renderer.CollisionLayer.Width; x++)
                    {
                        for (int y = 0; y < renderer.CollisionLayer.Height; y++)
                        {
                            var tile = renderer.CollisionLayer.GetTile(x, y);
                            if (tile != null)
                            {
                                minX = Math.Min(minX, x * tile.Tileset.TileWidth);
                                maxX = Math.Max(maxX, (x + 1) * tile.Tileset.TileWidth);
                                minY = Math.Min(minY, y * tile.Tileset.TileHeight);
                                maxY = Math.Max(maxY, (y + 1) * tile.Tileset.TileHeight);
                            }
                        }
                    }
                }

                return new RectangleF(Position + new Vector2(minX, minY), new Vector2(maxX, maxY) - new Vector2(minX, minY));

                //var collisionRenderers = GetComponents<TiledMapRenderer>().Where(r => r.CollisionLayer != null);
                //var tiles = collisionRenderers.SelectMany(r => r.CollisionLayer.Tiles.Values.Where(t => t != null));
                //var minX = int.MaxValue;
                //var maxX = int.MinValue;
                //var minY = int.MaxValue;
                //var maxY = int.MinValue;
                //foreach (var tile in tiles)
                //{
                //    var x = tile.X * tile.Tileset.TileWidth;
                //    var y = tile.Y * tile.Tileset.TileHeight;
                //    minX = Math.Min(minX, x);
                //    maxX = Math.Max(maxX, x + tile.Tileset.TileWidth);
                //    minY = Math.Min(minY, y);
                //    maxY = Math.Max(maxY, y + tile.Tileset.TileHeight);
                //}

                //var rectPos = new Vector2(minX, minY);
                //var bottomRight = new Vector2(maxX, maxY);
                //var rectSize = bottomRight - rectPos;

                //return new RectangleF(rectPos + Position, rectSize);
            }
        }

        public List<Vector2> AllCollisionTilePositions
        {
            get
            {
                var allPositions = new List<Vector2>();

                var collisionRenderers = GetComponents<TiledMapRenderer>().Where(r => r.CollisionLayer != null);
                foreach (var renderer in collisionRenderers)
                {
                    for (int x = 0; x < renderer.CollisionLayer.Width; x++)
                    {
                        for (int y = 0; y < renderer.CollisionLayer.Height; y++)
                        {
                            var tile = renderer.CollisionLayer.GetTile(x, y);
                            if (tile != null)
                                allPositions.Add((new Vector2(x * tile.Tileset.TileWidth, y * tile.Tileset.TileHeight) + Position));
                        }
                    }
                }

                return allPositions;
            }
        }

        public List<Vector2> FloorTilePositions = new List<Vector2>();

        public List<int> ChildrenIds = new List<int>();
        public List<Entity> TiledObjectEntities = new List<Entity>();

        public DungeonRoomEntity(DungeonComposite composite, DungeonNode dungeonNode) : base(dungeonNode.Id.ToString())
        {
            ParentComposite = composite;
            RoomId = dungeonNode.Id;
            Type = dungeonNode.Type;
            ChildrenIds = dungeonNode.Children.Select(c => c.ChildNodeId).ToList();
            //_textComponent = AddComponent(new TextComponent(Graphics.Instance.BitmapFont, $"{dungeonNode.Id}", Vector2.Zero, Color.Black));
        }

        #region LIFECYCLE

        public override void OnRemovedFromScene()
        {
            base.OnRemovedFromScene();

            foreach (var ent in TiledObjectEntities)
                ent.Destroy();
        }

        #endregion

        public void SetComponentsOnMapEnabled(bool enabled)
        {
            foreach (var ent in TiledObjectEntities)
                ent.SetEnabled(enabled);
        }

        public void CreateMap(TmxMap map)
        {
            Map = map;

            foreach (var ent in TiledObjectEntities)
                ent.Destroy();

            TiledObjectEntities.Clear();

            TiledObjectEntities = TiledHelper.SetupMap(this, map, false);
            SetComponentsOnMapEnabled(false);

            //_textComponent.SetLocalOffset(new Vector2(map.WorldWidth / 2, map.WorldHeight / 2));
        }

        public void MoveRoom(Vector2 movementAmount, bool moveChildComposites = false)
        {
            Position += movementAmount;
            for (int i = 0; i < FloorTilePositions.Count; i++)
                FloorTilePositions[i] += movementAmount;

            if (ChildrenOutsideComposite != null && ChildrenOutsideComposite.Count > 0)
            {
                foreach (var child in ChildrenOutsideComposite)
                    child.ParentComposite.MoveRooms(movementAmount);
            }
        }

        public List<T> FindComponentsOnMap<T>() where T : TiledComponent
        {
            return TiledObjectEntities.SelectMany(e => e.GetComponents<T>()).ToList();
        }

        public void ClearMap()
        {
            Map = null;

            FloorTilePositions.Clear();

            var renderers = GetComponents<TiledMapRenderer>();
            foreach (var renderer in renderers)
            {
                renderer.RemoveColliders();
                RemoveComponent(renderer);
            }

            foreach (var ent in TiledObjectEntities)
                ent.Destroy();

            TiledObjectEntities.Clear();

            //var comps = Scene.FindComponentsOfType<TiledComponent>().Where(c => c.MapEntity == this);
            //foreach (var comp in comps)
            //    comp.Entity.Destroy();
        }

        public bool OverlapsRoom(DungeonRoomEntity otherRoom, Vector2? movement = null)
        {
            var localCollisionBounds = CollisionBounds;
            var localCollisionTiles = AllCollisionTilePositions;
            var localFloorPositions = FloorTilePositions;
            var localDoorways = FindComponentsOnMap<DungeonDoorway>().Select(d => d.Bounds).ToList();
            if (movement != null)
            {
                localCollisionBounds.Location += movement.Value;
                localCollisionTiles = localCollisionTiles.Select(t => t + movement.Value).ToList();
                localFloorPositions = localFloorPositions.Select(p => p + movement.Value).ToList();
                localDoorways = localDoorways.Select(d =>
                {
                    d.Location += movement.Value;
                    return d;
                }).ToList();
            }

            var otherCollisionBounds = otherRoom.CollisionBounds;
            var otherCollisionTiles = otherRoom.AllCollisionTilePositions;
            var otherFloorPositions = otherRoom.FloorTilePositions;
            var otherDoorways = otherRoom.FindComponentsOnMap<DungeonDoorway>().Select(d => d.Bounds).ToList();

            foreach (var localPos in localFloorPositions)
            {
                if (otherDoorways.Any(d => d.Contains(localPos)))
                    return true;

                if (otherCollisionBounds.Contains(localPos))
                {
                    if (otherCollisionTiles.Contains(localPos))
                        return true;
                }
            }

            foreach (var localDoor in localDoorways)
            {
                if (otherFloorPositions.Any(op => localDoor.Contains(op)))
                    return true;

                if (otherDoorways.Any(od => od.Intersects(localDoor)))
                    return true;

                if (otherCollisionBounds.Intersects(localDoor))
                {
                    if (otherCollisionTiles.Any(p => localDoor.Contains(p)))
                        return true;
                }
            }

            if (localCollisionBounds.Intersects(otherCollisionBounds))
            {
                if (localCollisionTiles.Any(p => otherCollisionTiles.Any(op => p == op)))
                    return true;
            }

            foreach (var pos in otherFloorPositions)
            {
                if (localCollisionBounds.Contains(pos))
                {
                    if (localCollisionTiles.Any(p => p == pos))
                        return true;
                }
            }

            foreach (var otherDoor in otherDoorways)
            {
                if (localCollisionBounds.Intersects(otherDoor))
                {
                    if (localCollisionTiles.Any(p => otherDoor.Contains(p)))
                        return true;
                }
            }

            return false;

            ////check if local doorways intersect other doorways or other floor positions
            //if (localDoorways.Any(ld => otherDoorways.Any(od => od.Intersects(ld)) || otherFloorPositions.Any(op => ld.Contains(op))))
            //    return true;

            ////check if local floor positions are overlapping any other room doorways
            //if (localFloorPositions.Any(lp => otherDoorways.Any(od => od.Contains(lp))))
            //    return true;

            //if (!localCollisionBounds.Intersects(otherCollisionBounds)
            //    && !otherDoorways.Any(od => od.Intersects(localCollisionBounds))
            //    && !otherFloorPositions.Any(localCollisionBounds.Contains)
            //    && !localDoorways.Any(ld => ld.Intersects(otherCollisionBounds))
            //    && !localFloorPositions.Any(lp => otherCollisionBounds.Contains(lp)))
            //    return false;
        }
    }
}
