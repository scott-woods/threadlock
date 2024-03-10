using Microsoft.Xna.Framework;
using Nez;
using Nez.Tiled;
using Nez.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Helpers;
using Threadlock.Models;
using Threadlock.StaticData;

namespace Threadlock.Components.TiledComponents
{
    public class DungeonDoorway : TiledComponent
    {
        public bool HasConnection = false;
        public Vector2 PathfindingOrigin
        {
            get
            {
                if (_parentMap != null && _parentMap.Properties != null && _parentMap.Properties.TryGetValue("Area", out var area))
                {
                    var mapName = $"{area.ToLower()}_{Direction.ToLower()}_open";
                    var openMap = base.Entity.Scene.Content.LoadTiledMap($@"Content\Tiled\Tilemaps\{area}\Doorways\{mapName}.tmx");

                    if (openMap != null && openMap.ObjectGroups != null && openMap.ObjectGroups.SelectMany(g => g.Objects) != null)
                    {
                        var offsetObj = openMap.ObjectGroups.SelectMany(g => g.Objects).First(o => o.Name == "PathfindingOrigin");
                        if (offsetObj != null)
                        {
                            return Entity.Position + new Vector2(offsetObj.X, offsetObj.Y);
                        }
                    }
                }

                return Entity.Position;
            }
        }
        public string Direction;
        public DungeonRoom DungeonRoom;

        List<TiledMapRenderer> _mapRenderers = new List<TiledMapRenderer>();
        TmxMap _map;
        public TmxMap Map { get { return _map; } }
        TmxMap _parentMap;
        TiledMapRenderer _gateRenderer;
        bool _processed = false;

        public override void Initialize()
        {
            base.Initialize();

            //get pathfinding offset
            //if (TmxObject.Properties.TryGetValue("PathfindingOffset", out var pathfindingOffset))
            //{
            //    var offsetValues = pathfindingOffset.Split(' ');
            //    PathfindingOffset = new Vector2(Convert.ToInt32(offsetValues[0]), Convert.ToInt32(offsetValues[1]));
            //}

            //get direction exit is facing
            if (TmxObject.Properties.TryGetValue("Direction", out var direction))
            {
                Direction = direction;
            }

            //get parent dungeon room
            if (MapEntity.TryGetComponent<DungeonRoom>(out var dungeonRoom))
            {
                DungeonRoom = dungeonRoom;
                dungeonRoom.Doorways.Add(this);
            }

            //parent dungeon map
            if (MapEntity.TryGetComponent<TiledMapRenderer>(out var mapRenderer))
            {
                _parentMap = mapRenderer.TiledMap;
            }

            //Entity.SetPosition(new Microsoft.Xna.Framework.Vector2(base.Entity.Position.X - (_map.Width * _map.TileWidth) / 2, base.Entity.Position.Y - (_map.Height * _map.TileHeight) / 2));
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            if (!_processed)
                CreateMap();
        }

        public void SetOpen(bool open)
        {
            HasConnection = open;

            //remove previous renderers
            foreach (var renderer in _mapRenderers)
            {
                renderer.RemoveColliders();
                Entity.RemoveComponent(renderer);
            }
            _mapRenderers.Clear();

            CreateMap();
        }

        public void SetGateOpen(bool gateOpen)
        {
            if (!HasConnection)
                return;

            if (gateOpen)
            {
                if (_gateRenderer == null)
                    return;
                else
                {
                    _gateRenderer.RemoveColliders();
                    Entity.RemoveComponent(_gateRenderer);
                }
            }
            else
            {
                if (_gateRenderer != null)
                    return;
                else
                {
                    _gateRenderer = Entity.AddComponent(new TiledMapRenderer(_map, "GateWalls"));
                    var layersToRender = new List<string>();
                    if (_map.Layers.Contains("GateWalls"))
                        layersToRender.Add("GateWalls");
                    if (_map.Layers.Contains("GateFront"))
                        layersToRender.Add("GateFront");
                    if (_map.Layers.Contains("GateAboveFront"))
                        layersToRender.Add("GateAboveFront");
                    _gateRenderer.SetLayersToRender(layersToRender.ToArray());
                    Flags.SetFlagExclusive(ref _gateRenderer.PhysicsLayer, PhysicsLayers.Environment);
                }
            }
        }

        public bool IsDirectMatch(DungeonDoorway doorway)
        {
            switch (doorway.Direction)
            {
                case "Top":
                    return Direction == "Bottom";
                case "Bottom":
                    return Direction == "Top";
                case "Left":
                    return Direction == "Right";
                case "Right":
                    return Direction == "Left";
                default:
                    return false;
            }
        }

        /// <summary>
        /// create the actual map entity. will be open if IsConnection is true, and closed if it is false
        /// </summary>
        void CreateMap()
        {
            if (_parentMap != null && _parentMap.Properties != null && _parentMap.Properties.TryGetValue("Area", out var area))
            {
                var doorwayStatus = HasConnection ? "Open" : "Closed";
                var mapName = $"{area.ToLower()}_{Direction.ToLower()}_{doorwayStatus.ToLower()}";
                _map = base.Entity.Scene.Content.LoadTiledMap($@"Content\Tiled\Tilemaps\{area}\Doorways\{mapName}.tmx");

                //create main map renderer
                var mapRenderer = Entity.AddComponent(new TiledMapRenderer(_map));
                if (_map.TileLayers.TryGetValue($"Walls", out var wallLayer))
                    mapRenderer.CollisionLayer = wallLayer;
                mapRenderer.SetLayersToRender(_map.Layers
                    .Where(l => new[] { $"Back", $"Walls" }.Contains(l.Name))
                    .Select(l => l.Name).ToArray());
                mapRenderer.RenderLayer = 10;
                Flags.SetFlagExclusive(ref mapRenderer.PhysicsLayer, PhysicsLayers.Environment);

                //create above map renderer
                var tiledMapDetailsRenderer = Entity.AddComponent(new TiledMapRenderer(_map));
                tiledMapDetailsRenderer.SetLayersToRender(_map.Layers
                    .Where(l => new[] { $"Front", $"AboveFront" }.Contains(l.Name))
                    .Select(l => l.Name).ToArray());
                tiledMapDetailsRenderer.RenderLayer = RenderLayers.Front;
                //tiledMapDetailsRenderer.Material = Material.StencilWrite();

                TiledHelper.CreateEntitiesForTiledObjects(mapRenderer);

                _mapRenderers.Add(mapRenderer);
                _mapRenderers.Add(tiledMapDetailsRenderer);

                _processed = true;

                ////create main map renderer
                //var mapRenderer = Entity.AddComponent(new TiledMapRenderer(_map, "Walls"));
                //mapRenderer.SetLayersToRender(new[] { "Back", "Walls" });
                //mapRenderer.RenderLayer = 10;
                //Flags.SetFlagExclusive(ref mapRenderer.PhysicsLayer, PhysicsLayers.Environment);

                ////create above map renderer
                //var tiledMapDetailsRenderer = Entity.AddComponent(new TiledMapRenderer(_map));
                //var layersToRender = new List<string>();
                //if (_map.Layers.Contains("Front"))
                //    layersToRender.Add("Front");
                //if (_map.Layers.Contains("AboveFront"))
                //    layersToRender.Add("AboveFront");
                //tiledMapDetailsRenderer.SetLayersToRender(layersToRender.ToArray());
                //tiledMapDetailsRenderer.RenderLayer = RenderLayers.Front;
                ////tiledMapDetailsRenderer.Material = Material.StencilWrite();

                //TiledHelper.CreateEntitiesForTiledObjects(mapRenderer);

                //_mapRenderers.Add(mapRenderer);
                //_mapRenderers.Add(tiledMapDetailsRenderer);
            }
        }
    }
}
