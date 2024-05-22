using Microsoft.Xna.Framework;
using Nez;
using Nez.AI.Pathfinding;
using Nez.Persistence;
using Nez.Tiled;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Components.TiledComponents;
using Threadlock.Entities;
using Threadlock.Helpers;
using Threadlock.Models;
using Threadlock.StaticData;
using static Threadlock.SceneComponents.Dungenerator.CorridorPainter;
using static Threadlock.StaticData.Terrains;
using DoorwayPoint = Threadlock.Models.DoorwayPoint;

namespace Threadlock.SceneComponents.Dungenerator
{
    public class Dungenerator2 : SceneComponent
    {
        const int _graphRectPad = 5;
        const int _minRoomDistance = 7;
        const int _maxRoomDistance = 12;
        const int _wallPadding = 3;
        const int _minHallDistance = 4;

        readonly Dictionary<Corners, List<Tuple<Vector2, Corners>>> _matchingCornersDict = new Dictionary<Corners, List<Tuple<Vector2, Corners>>>()
        {
            [Corners.TopLeft] = new List<Tuple<Vector2, Corners>>()
            {
                new Tuple<Vector2, Corners>(DirectionHelper.Left, Corners.TopRight),
                new Tuple<Vector2, Corners>(DirectionHelper.Up, Corners.BottomLeft),
                new Tuple<Vector2, Corners>(DirectionHelper.UpLeft, Corners.BottomRight),
            },
            [Corners.TopRight] = new List<Tuple<Vector2, Corners>>()
            {
                new Tuple<Vector2, Corners>(DirectionHelper.Right, Corners.TopLeft),
                new Tuple<Vector2, Corners>(DirectionHelper.Up, Corners.BottomRight),
                new Tuple<Vector2, Corners>(DirectionHelper.UpRight, Corners.BottomLeft),
            },
            [Corners.BottomLeft] = new List<Tuple<Vector2, Corners>>()
            {
                new Tuple<Vector2, Corners>(DirectionHelper.Left, Corners.BottomRight),
                new Tuple<Vector2, Corners>(DirectionHelper.Down, Corners.TopLeft),
                new Tuple<Vector2, Corners>(DirectionHelper.DownLeft, Corners.TopRight),
            },
            [Corners.BottomRight] = new List<Tuple<Vector2, Corners>>()
            {
                new Tuple<Vector2, Corners>(DirectionHelper.Right, Corners.BottomLeft),
                new Tuple<Vector2, Corners>(DirectionHelper.Down, Corners.TopRight),
                new Tuple<Vector2, Corners>(DirectionHelper.DownRight, Corners.TopLeft),
            },
        };

        List<TmxMap> _allMaps = new List<TmxMap>();
        Dictionary<TmxMap, List<Vector2>> _mapWallDict = new Dictionary<TmxMap, List<Vector2>>();
        Dictionary<TmxMap, List<Vector2>> _mapDoorDict = new Dictionary<TmxMap, List<Vector2>>();
        Dictionary<TmxMap, List<TmxTilesetExt>> _mapTilesetDict = new Dictionary<TmxMap, List<TmxTilesetExt>>();
        Dictionary<Vector2, int> _floorDict = new Dictionary<Vector2, int>();
        Dictionary<Vector2, int> _wallDict = new Dictionary<Vector2, int>();

        List<DungeonComposite2> _loopComposites = new List<DungeonComposite2>();
        List<DungeonComposite2> _treeComposites = new List<DungeonComposite2>();
        List<DungeonComposite2> _allComposites { get => _treeComposites.Concat(_loopComposites).ToList(); }

        List<DungeonRoom> _allRooms { get => _allComposites.SelectMany(c => c.DungeonRooms).ToList(); }

        public void Generate(DungeonConfig dungeonConfig)
        {
            //read flow file
            DungeonFlow flow = new DungeonFlow();
            var flowFileNames = new List<string>(dungeonConfig.FlowFiles);
            while (flowFileNames.Any())
            {
                var fileName = flowFileNames.RandomItem();
                flowFileNames.Remove(fileName);
                if (File.Exists($"Content/Data/{fileName}.json"))
                {
                    var json = File.ReadAllText($"Content/Data/{fileName}.json");
                    flow = Json.FromJson<DungeonFlow>(json);
                    if (flow != null)
                        break;
                }
            }

            //init dungeon rooms with children
            Dictionary<int, DungeonRoom> roomDict = new Dictionary<int, DungeonRoom>();
            foreach (var node in flow.Nodes)
            {
                var room = new DungeonRoom(node);
                roomDict.Add(node.Id, room);
            }
            foreach (var node in flow.Nodes)
            {
                var room = roomDict[node.Id];
                foreach (var child in node.Children.Select(c => c.ChildNodeId))
                {
                    if (roomDict.TryGetValue(child, out var childRoom))
                        room.Children.Add(childRoom);
                }
            }

            //get composites
            var dungeonGraph = new DungeonGraph();
            dungeonGraph.ProcessGraph(flow.Nodes);
            foreach (var loop in dungeonGraph.Loops)
            {
                var composite = new DungeonComposite2();
                foreach (var node in loop)
                {
                    if (roomDict.TryGetValue(node.Id, out var room))
                    {
                        composite.DungeonRooms.Add(room);
                        room.ParentComposite = composite;
                    }
                }
                _loopComposites.Add(composite);
            }
            foreach (var tree in dungeonGraph.Trees)
            {
                var composite = new DungeonComposite2();
                foreach (var node in tree)
                {
                    if (roomDict.TryGetValue(node.Id, out var room))
                    {
                        composite.DungeonRooms.Add(room);
                        room.ParentComposite = composite;
                    }
                }
                _treeComposites.Add(composite);
            }

            //load all maps for this area
            Dictionary<TmxMap, string> mapDictionary = new Dictionary<TmxMap, string>(); //dictionary so unused maps can be unloaded
            FieldInfo[] fields = dungeonConfig.AreaType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
            foreach (var field in fields)
            {
                if (field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
                {
                    string value = (string)field.GetValue(null);
                    if (value.EndsWith(".tmx"))
                    {
                        //load map into content
                        var map = Scene.Content.LoadTiledMap(value);
                        _allMaps.Add(map);
                        mapDictionary.Add(map, value);

                        //handle doorways
                        var tilesToDelete = new List<Vector2>();
                        _mapDoorDict[map] = new List<Vector2>();
                        var doorways = map.ObjectGroups?.SelectMany(g => g.Objects).Where(o => o.Type == "DoorwayPoint").Select(o => new DoorwayPoint(null, o)).ToList();
                        foreach (var doorway in doorways)
                        {
                            //dir is vertical for horizontal doorways, and vice versa
                            var dir = new Vector2(Math.Abs(doorway.Direction.Y), Math.Abs(doorway.Direction.X));

                            var startPos = doorway.Position + (-dir * 16 * 2);
                            for (int i = 0; i < 5; i++)
                            {
                                var pos = startPos + (dir * 16 * i);
                                _mapDoorDict[map].Add(pos);
                                tilesToDelete.Add(pos);

                                tilesToDelete.Add(pos + (doorway.Direction * 16));
                            }
                        }

                        //get tile data from maps
                        _mapWallDict[map] = new List<Vector2>();
                        foreach (var layer in map.TileLayers)
                        {
                            foreach (var tile in layer.Tiles)
                            {
                                if (tile == null)
                                    continue;

                                //get world space tile pos
                                var tilePos = new Vector2(tile.X * tile.Tileset.TileWidth, tile.Y * tile.Tileset.TileHeight);

                                //add to map wall dictionary
                                if (layer.Name.StartsWith("Walls"))
                                    _mapWallDict[map].Add(tilePos);

                                //remove tiles in door positions
                                if (tilesToDelete.Contains(tilePos))
                                    layer.RemoveTile(tile.X, tile.Y);
                            }
                        }

                        //handle tilesets
                        foreach (var tileset in map.Tilesets)
                        {
                            if (!_mapTilesetDict.ContainsKey(map))
                                _mapTilesetDict[map] = new List<TmxTilesetExt>();
                            _mapTilesetDict[map].Add(new TmxTilesetExt(tileset));
                        }
                    }
                }
            }

            var attempts = 0;
            while (attempts < 100)
            {
                attempts++;

                foreach (var dungeonRoom in _allRooms)
                    dungeonRoom.Reset();

                //Handle trees
                foreach (var tree in _treeComposites)
                {
                    var treeAttempts = 0;
                    while (treeAttempts < 100)
                    {
                        treeAttempts++;

                        var processedRooms = new List<DungeonRoom>();
                        foreach (var room in tree.DungeonRooms)
                        {
                            room.Reset();

                            //get the number of doorways this room needs
                            var requiredDoors = room.Children.Count + _allRooms.Where(r => r.Children.Contains(room)).Count();

                            //find valid maps
                            var validMaps = _allMaps.Where(m => m.Properties != null && m.Properties.TryGetValue("RoomType", out var roomType) && roomType == room.RoomType && m.ObjectGroups?.SelectMany(g => g.Objects).Where(o => o.Type == "DoorwayPoint").Count() >= requiredDoors).ToList();

                            while (validMaps.Any())
                            {
                                //pick a random map
                                var map = validMaps.RandomItem();
                                room.Map = map;

                                //if this is the first room, no need to validate against other rooms
                                if (processedRooms.Count <= 0)
                                    break;

                                //get previously added room
                                var previousRoom = processedRooms.Last();

                                //try to connect rooms
                                if (ConnectRooms(previousRoom, room, processedRooms))
                                    break;

                                validMaps.Remove(map);
                            }

                            if (validMaps.Count <= 0)
                                break;

                            processedRooms.Add(room);
                        }

                        if (processedRooms.Count != tree.DungeonRooms.Count)
                            continue;

                        break;
                    }
                }

                //handle loops
                //foreach (var loop in _loopComposites)
                //{
                //    var loopAttempts = 0;
                //    while (loopAttempts < 100)
                //    {
                //        loopAttempts++;

                //        var processedRooms = new List<DungeonRoom>();

                //        //get placement order
                //        var placementOrder = new List<DungeonRoom>();
                //        for (int i = 0; i < (loop.DungeonRooms.Count + 1) / 2; i++)
                //        {
                //            placementOrder.Add(loop.DungeonRooms[i]);
                //            if (i != loop.DungeonRooms.Count - i - 1)
                //                placementOrder.Add(loop.DungeonRooms[loop.DungeonRooms.Count - i - 1]);
                //        }
                //    }
                //}

                //connect composites
                List<DungeonComposite2> processedComposites = new List<DungeonComposite2>();
                bool compositeFailed = false;
                foreach (var composite in _allComposites.OrderByDescending(c => c.GetRoomsFromChildrenComposites().Count))
                {
                    var parentRooms = composite.DungeonRooms.Where(r => r.ChildrenOutsideComposite.Any());
                    foreach (var parentRoom in parentRooms)
                    {
                        List<DungeonComposite2> localComposites = new List<DungeonComposite2>();
                        foreach (var child in parentRoom.ChildrenOutsideComposite)
                        {
                            var roomsToCheck = processedComposites
                                .SelectMany(c => c.DungeonRooms)
                                .Concat(composite.DungeonRooms)
                                .Concat(localComposites.SelectMany(c => c.DungeonRooms))
                                .Distinct()
                                .ToList();

                            var roomsToMove = new List<DungeonRoom>(child.ParentComposite.DungeonRooms);

                            if (!ConnectRooms(parentRoom, child, roomsToCheck, roomsToMove))
                            {
                                compositeFailed = true;
                                break;
                            }

                            localComposites.Add(child.ParentComposite);
                        }

                        if (compositeFailed)
                            break;
                    }

                    if (compositeFailed)
                        break;

                    processedComposites.Add(composite);
                }

                if (compositeFailed)
                    continue;

                break;
            }
        }

        /// <summary>
        /// called once scene has finished loading. creates entities and components for rooms and corridors
        /// </summary>
        public void FinalizeDungeon()
        {
            //init path tiles
            var pathTiles = new List<TileData>();

            //loop through rooms
            foreach (var room in _allRooms)
            {
                //set up map
                if (room.Map != null)
                {
                    var ent = Scene.CreateEntity($"dungeon-room-${room.Id}", room.Position);
                    TiledHelper.SetupMap(ent, room.Map);
                }

                //get back tile positions and their ids
                foreach (var tile in room.Map.TileLayers.Where(l => l.Name.StartsWith("Back")).SelectMany(l => l.Tiles))
                {
                    if (tile == null)
                        continue;
                    var tilePos = new Vector2(tile.X * tile.Tileset.TileWidth, tile.Y * tile.Tileset.TileHeight);
                    _floorDict.Add(tilePos + room.Position, tile.Gid);
                }

                //get wall tile positions and their ids
                foreach (var tile in room.Map.TileLayers.Where(l => l.Name.StartsWith("Walls")).SelectMany(l => l.Tiles))
                {
                    if (tile == null)
                        continue;
                    var tilePos = new Vector2(tile.X * tile.Tileset.TileWidth, tile.Y * tile.Tileset.TileHeight);
                    _wallDict.Add(tilePos + room.Position, tile.Gid);
                }

                //get all corridor positions
                foreach (var corridorTile in room.CorridorTiles)
                {
                    var test = Scene.CreateEntity("", corridorTile.Position).AddComponent(new PrototypeSpriteRenderer(4, 4));
                    test.SetColor(Color.Blue);
                    for (int x = -2; x <= 2; x++)
                    {
                        for (int y = -2; y <= 2; y++)
                        {
                            var pos = corridorTile.Position + new Vector2(x * 16, y * 16);
                            pathTiles.Add(new TileData(typeof(Corridor), pos));
                        }
                    }
                }

                //handle map door positions
                if (_mapDoorDict.TryGetValue(room.Map, out var doorPositions))
                {
                    foreach (var doorPos in doorPositions)
                    {
                        var worldDoorPos = doorPos + room.Position;
                        pathTiles.Add(new TileData(typeof(Corridor), worldDoorPos));
                    }
                }
            }

            GenerateCorridors(pathTiles);
        }

        void GenerateCorridors(List<TileData> pathTiles)
        {
            //this is bad but there should really only ever be one tileset per area
            var tileset = _mapTilesetDict.Values.FirstOrDefault().FirstOrDefault();
            if (tileset == null)
                return;

            //init wall positions dict
            Dictionary<Vector2, TileData> wallTiles = new Dictionary<Vector2, TileData>();

            //get terrain set
            if (tileset.TryGetTerrainSet(typeof(Corridor), out var corridorTerrainSet))
            {
                //loop through path tiles
                foreach (var tileData in pathTiles)
                {
                    var ent = Scene.CreateEntity("", tileData.Position).AddComponent(new PrototypeSpriteRenderer(2, 2));

                    //check corners to create a positional mask for the tile
                    foreach (Corners corner in Enum.GetValues(typeof(Corners)))
                    {
                        //get corner tile position
                        var cornerDir = DirectionHelper.CornerDictionary[corner];
                        var neighborPos = tileData.Position + (cornerDir * 16);

                        //check pre existing floor tiles
                        if (_floorDict.TryGetValue(neighborPos, out var neighborTileId))
                        {
                            if (corridorTerrainSet.TryGetMask(neighborTileId, out var neighborMask))
                            {
                                var neighborMaskCorner = TileBitmaskHelper.GetMaskInCorner<Corridor>(neighborMask, TileBitmaskHelper.MatchingCornersDict[corner]);
                                TileBitmaskHelper.SetMaskInCorner<Corridor>(neighborMaskCorner, corner, ref tileData.Mask);
                            }
                            else
                                TileBitmaskHelper.SetMaskInCorner<Corridor>(Corridor.Floor, corner, ref tileData.Mask);
                        }
                        else //check path tiles
                        {
                            var neighborPathTile = pathTiles.FirstOrDefault(t => t.Position == neighborPos);
                            if (neighborPathTile != null)
                            {
                                if (neighborPathTile.TileId >= 0)
                                {
                                    var neighborMaskCorner = TileBitmaskHelper.GetMaskInCorner<Corridor>(neighborPathTile.Mask, TileBitmaskHelper.MatchingCornersDict[corner]);
                                    TileBitmaskHelper.SetMaskInCorner<Corridor>(neighborMaskCorner, corner, ref tileData.Mask);
                                }
                                else
                                    TileBitmaskHelper.SetMaskInCorner<Corridor>(Corridor.Floor, corner, ref tileData.Mask);
                            }
                            else
                                TileBitmaskHelper.SetMaskInCorner<Corridor>(Corridor.Wall, corner, ref tileData.Mask);
                        }

                        //get the results of the mask for this corner
                        var currentMaskInCorner = TileBitmaskHelper.GetMaskInCorner<Corridor>(tileData.Mask, corner);

                        //udpdate neighboring tiles if necessary
                        foreach (var pair in _matchingCornersDict[corner])
                        {
                            //get values from pair
                            var dir = pair.Item1;
                            var matchingCorner = pair.Item2;

                            //get adjacent pos
                            var adjacentPos = tileData.Position + (dir * 16);

                            //see if there is a neighboring path tile there
                            var neighbor = pathTiles.FirstOrDefault(p => p.Position == adjacentPos);
                            if (neighbor != null)
                            {
                                //update neighbor mask in the matching corner
                                TileBitmaskHelper.SetMaskInCorner<Corridor>(currentMaskInCorner, matchingCorner, ref neighbor.Mask);

                                //try to replace neighbor tile only if it has previously selected one
                                if (neighbor.TileId >= 0)
                                {
                                    if (corridorTerrainSet.TryGetTile(neighbor.Mask, out var replacementTileId))
                                        neighbor.TileId = replacementTileId;
                                }
                            }
                        }

                        //check where we need walls
                        foreach (var dir in DirectionHelper.PrincipleDirections)
                        {
                            var pos = tileData.Position + (dir * 16);
                            if (!_floorDict.ContainsKey(pos) && !pathTiles.Any(t => t.Position == pos) && !wallTiles.ContainsKey(pos))
                            {
                                //init new wall tile
                                var wallTile = new TileData(typeof(WallTileType), pos);

                                //add to wall tiles dict
                                wallTiles.Add(pos, wallTile);
                            }
                        }
                    }

                    //use the mask to find an appropriate tile
                    if (corridorTerrainSet.TryGetTile(tileData.Mask, out var tileId))
                        tileData.TileId = tileId;
                }

                //handle walls, finding their masks and picking tiles for them
                foreach (var wallTile in wallTiles.Values)
                {
                    //get mask for this wall tile
                    foreach (Corners wallCorner in Enum.GetValues(typeof(Corners)))
                    {
                        var wallCornerDir = DirectionHelper.CornerDictionary[wallCorner];
                        var wallCornerPos = wallTile.Position + (wallCornerDir * 16);

                        if (_floorDict.ContainsKey(wallCornerPos) || pathTiles.Any(t => t.Position == wallCornerPos))
                            TileBitmaskHelper.SetMaskInCorner<WallTileType>(WallTileType.Floor, wallCorner, ref wallTile.Mask);
                        else if (wallTiles.ContainsKey(wallCornerPos))
                            TileBitmaskHelper.SetMaskInCorner<WallTileType>(WallTileType.Wall, wallCorner, ref wallTile.Mask);
                        else
                            TileBitmaskHelper.SetMaskInCorner<WallTileType>(WallTileType.None, wallCorner, ref wallTile.Mask);
                    }

                    //use the mask to find an appropriate tile
                    if (tileset.TryGetTerrainSet(typeof(WallTileType), out var wallTerrainSet))
                    {
                        if (wallTerrainSet.TryGetTile(wallTile.Mask, out var tileId))
                            wallTile.TileId = tileId;
                        else if (wallTerrainSet.TryGetTile(TileBitmaskHelper.ReplaceZerosWithEnumValue<WallTileType>(WallTileType.Wall, wallTile.Mask), out tileId))
                            wallTile.TileId = tileId;
                    }
                }
            }

            //handle extra tiles
            Dictionary<Vector2, ExtraTile> extraTileDict = new Dictionary<Vector2, ExtraTile>();
            foreach (var tile in wallTiles.Values)
            {
                if (tileset.ExtraTileDict.TryGetValue(tile.TileId, out var extraTiles))
                {
                    var groupedTiles = extraTiles
                        .GroupBy(t => t.Offset)
                        .Select(g =>
                        {
                            int index = Nez.Random.NextInt(g.Count());
                            return g.ElementAt(index);
                        });

                    foreach (var extraTile in groupedTiles)
                    {
                        var pos = tile.Position + (extraTile.Offset * 16);
                        extraTileDict.TryAdd(pos, extraTile);
                    }
                }
            }

            //make single tile dictionaries
            var pathTileDict = new Dictionary<Vector2, SingleTile>();
            var wallTileDict = new Dictionary<Vector2, SingleTile>();
            var aboveFrontTileDict = new Dictionary<Vector2, SingleTile>();
            foreach (var pathTile in pathTiles)
            {
                if (pathTile.TileId < 0)
                    continue;
                pathTileDict.TryAdd(pathTile.Position, new SingleTile(pathTile.TileId));
            }
            foreach (var wallTile in wallTiles.Values)
            {
                if (wallTile.TileId < 0)
                    continue;
                wallTileDict.TryAdd(wallTile.Position, new SingleTile(wallTile.TileId, true));
            }
            foreach (var kvp in extraTileDict)
            {
                var pos = kvp.Key;
                var extraTile = kvp.Value;

                switch (extraTile.RenderLayer)
                {
                    case RenderLayers.Back:
                        pathTileDict.TryAdd(pos, new SingleTile(extraTile.TileId));
                        break;
                    case RenderLayers.Walls:
                        wallTileDict.TryAdd(pos, new SingleTile(extraTile.TileId, true));
                        break;
                    case RenderLayers.AboveFront:
                        aboveFrontTileDict.TryAdd(pos, new SingleTile(extraTile.TileId));
                        break;
                }
            }

            //make corridor renderers
            var corridorRenderer = Scene.CreateEntity("back-corridor-renderer").AddComponent(new CorridorRenderer(tileset.Tileset, pathTileDict));
            corridorRenderer.SetRenderLayer(RenderLayers.Back);
            var wallRenderer = Scene.CreateEntity("walls-corridor-renderer").AddComponent(new CorridorRenderer(tileset.Tileset, wallTileDict, true));
            wallRenderer.SetRenderLayer(RenderLayers.AboveFront); //for the forest, walls should be AboveFront
            var aboveFrontRenderer = Scene.CreateEntity("above-front-corridor-renderer").AddComponent(new CorridorRenderer(tileset.Tileset, aboveFrontTileDict));
            aboveFrontRenderer.SetRenderLayer(RenderLayers.AboveFront);
        }

        bool ConnectRooms(DungeonRoom startRoom, DungeonRoom endRoom, List<DungeonRoom> roomsToCheck)
        {
            return ConnectRooms(startRoom, endRoom, roomsToCheck, new List<DungeonRoom> { endRoom });
        }

        bool ConnectRooms(DungeonRoom startRoom, DungeonRoom endRoom, List<DungeonRoom> roomsToCheck, List<DungeonRoom> roomsToMove)
        {
            //get possible doorway pairs
            var pairs = from d1 in startRoom.Doorways.Where(d => !d.HasConnection)
                        from d2 in endRoom.Doorways.Where(d => !d.HasConnection)
                        select new Tuple<DoorwayPoint, DoorwayPoint>(d1, d2);

            //init dictionary for list of valid positions and their associated valid doorway pairs
            Dictionary<Vector2, List<Tuple<DoorwayPoint, DoorwayPoint>>> validPositions = new Dictionary<Vector2, List<Tuple<DoorwayPoint, DoorwayPoint>>>();

            //try all positions in a radius around the start door
            for (int x = -_maxRoomDistance; x <= _maxRoomDistance; x++)
            {
                if (Math.Abs(x) <= _minRoomDistance)
                    continue;
                for (int y = -_maxRoomDistance; y <= _maxRoomDistance; y++)
                {
                    if (Math.Abs(y) <= _minRoomDistance)
                        continue;

                    //get the distance the end door should be from the start door
                    var dist = new Vector2(x * 16, y * 16);

                    var pairsList = pairs.ToList();

                    //try each possible doorway pair at this position
                    while (pairsList.Any())
                    {
                        //pick a random doorway pair
                        var pair = pairsList.RandomItem();
                        pairsList.Remove(pair);

                        //move rooms
                        var targetPos = pair.Item1.Position + dist;
                        var movement = targetPos - pair.Item2.Position;
                        for (int i = 0; i < roomsToMove.Count; i++)
                        {
                            var room = roomsToMove[i];
                            room.Position += movement;
                        }

                        //check for overlaps
                        if (roomsToCheck.Any(roomToCheck =>
                        {
                            //if the room to check doesn't have a map, there's nothing to overlap with
                            if (roomToCheck.Map == null)
                                return false;

                            //check against each room to move
                            if (roomsToMove.Any(roomToMove =>
                            {
                                //if room to move has no map, there's nothing to overlap with
                                if (roomToMove.Map == null)
                                    return false;

                                //if the collision bounds intersect, this position is invalid
                                if (roomToMove.CollisionBounds.Intersects(roomToCheck.CollisionBounds))
                                    return true;

                                //get walls from the room to move map
                                if (_mapWallDict.TryGetValue(roomToMove.Map, out var roomToMoveMapWalls))
                                {
                                    //adjust to world space
                                    var adjustedMapWalls = roomToMoveMapWalls.Select(w => w + roomToMove.Position).ToList();

                                    //check room to check's corridor tiles for overlaps with room to move wall tiles
                                    if (roomToCheck.CorridorTiles.Any(t =>
                                    {
                                        var tileRect = new RectangleF(t.Position + new Vector2(-_wallPadding * 16, -_wallPadding * 16), new Vector2(((_wallPadding * 2) + 1) * 16));
                                        return adjustedMapWalls.Any(w => tileRect.Contains(w));
                                    }))
                                        return true;
                                }

                                //get walls from the room to check map
                                if (_mapWallDict.TryGetValue(roomToCheck.Map, out var roomToCheckMapWalls))
                                {
                                    //adjust to world space
                                    var adjustedMapWalls = roomToCheckMapWalls.Select(w => w + roomToCheck.Position).ToList();

                                    //check room to move's corridor tiles for overlaps with room to check wall tiles
                                    if (roomToMove.CorridorTiles.Any(t =>
                                    {
                                        var tileRect = new RectangleF(t.Position + new Vector2(-_wallPadding * 16, -_wallPadding * 16), new Vector2(((_wallPadding * 2) + 1) * 16));
                                        return adjustedMapWalls.Any(w => tileRect.Contains(w));
                                    }))
                                        return true;
                                }

                                //no overlap with this room to move, return false
                                return false;
                            }))
                                return true; //overlap found with a room to move

                            //no overlap found
                            return false;
                        }))
                        {
                            //overlap found, continue to try next pair
                            continue;
                        }
                        else
                        {
                            //add pair and position to dictionary
                            if (!validPositions.ContainsKey(pair.Item2.ParentRoom.Position))
                                validPositions[pair.Item2.ParentRoom.Position] = new List<Tuple<DoorwayPoint, DoorwayPoint>>();
                            validPositions[pair.Item2.ParentRoom.Position].Add(pair);
                        }
                    }
                }
            }

            //try all valid room positions in order of closest to farthest away
            bool success = false;
            foreach (var kvp in validPositions.OrderBy(p => p.Value.Select(x => Vector2.Distance(x.Item1.Position, x.Item2.Position)).Min()))
            {
                var pos = kvp.Key;

                //move rooms
                var movement = pos - endRoom.Position;
                for (int i = 0; i < roomsToMove.Count; i++)
                {
                    var room = roomsToMove[i];
                    room.Position += movement;
                }

                //try all pairs at this position
                var possiblePairs = validPositions[pos].OrderBy(p => Vector2.Distance(p.Item1.Position, p.Item2.Position));
                foreach (var pair in possiblePairs)
                {
                    //try to connect doorways
                    var doorwayRoomsToCheck = new List<DungeonRoom>(roomsToCheck);
                    foreach (var room in roomsToMove)
                    {
                        if (!doorwayRoomsToCheck.Contains(room))
                            doorwayRoomsToCheck.Add(room);
                    }
                    if (ConnectDoorways(pair.Item1, pair.Item2, doorwayRoomsToCheck))
                    {
                        //update doorway status
                        success = true;
                        pair.Item1.HasConnection = true;
                        pair.Item2.HasConnection = true;
                        break;
                    }
                }

                if (success)
                    break;
            }

            return success;
        }

        /// <summary>
        /// try to connect two doorways. assumes rooms are already in their final positions
        /// </summary>
        /// <param name="startDoor"></param>
        /// <param name="endDoor"></param>
        /// <param name="roomsToCheck"></param>
        /// <returns></returns>
        bool ConnectDoorways(DoorwayPoint startDoor, DoorwayPoint endDoor, List<DungeonRoom> roomsToCheck)
        {
            //get rect around doorways
            var rectX = Math.Min(startDoor.ParentRoom.CollisionBounds.X, endDoor.ParentRoom.CollisionBounds.X) - (_graphRectPad * 16);
            var rectY = Math.Min(startDoor.ParentRoom.CollisionBounds.Y, endDoor.ParentRoom.CollisionBounds.Y) - (_graphRectPad * 16);
            var rectRight = Math.Max(startDoor.ParentRoom.CollisionBounds.Right, endDoor.ParentRoom.CollisionBounds.Right) + (_graphRectPad * 16);
            var rectBottom = Math.Max(startDoor.ParentRoom.CollisionBounds.Bottom, endDoor.ParentRoom.CollisionBounds.Bottom) + (_graphRectPad * 16);
            var rectWidth = rectRight - rectX;
            var rectHeight = rectBottom - rectY;

            //create graph rect
            var graphRect = new RectangleF(rectX, rectY, rectWidth, rectHeight);
            var graphOffset = graphRect.Location;
            List<Point> finalWalls = new List<Point>();

            //init final path list
            var beginningPath = new List<Vector2>();
            var endPath = new List<Vector2>();
            var finalPath = new List<Vector2>();

            var reservedPositions = new List<Vector2>();
            for (int i = Mathf.CeilToInt(_minHallDistance / 2); i <= _minHallDistance; i++)
            {
                var nextStartPos = startDoor.Position + (startDoor.Direction * i * 16);
                var nextEndPos = endDoor.Position + (endDoor.Direction * i * 16);
                reservedPositions.Add(nextStartPos);
                reservedPositions.Add(nextEndPos);

                beginningPath.Add(nextStartPos);
                endPath.Add(nextEndPos);

                //finalPath.Add(nextStartPos);
            }

            //reverse end positions
            endPath.Reverse();

            //get adjusted start and end positions
            var actualStartPos = beginningPath.Last() + (startDoor.Direction * 16);
            //var actualEndPos = endDoor.Position + (endDoor.Direction * 16 * (_minHallDistance + 1));
            var actualEndPos = endPath.First() + (endDoor.Direction * 16);

            reservedPositions.Add(actualStartPos);
            reservedPositions.Add(actualEndPos);

            //var reservedPositions = new List<Vector2>() { actualStartPos, actualEndPos };

            //get walls from each room
            var wallRooms = new List<DungeonRoom>(roomsToCheck);
            if (!wallRooms.Contains(startDoor.ParentRoom))
                wallRooms.Add(startDoor.ParentRoom);
            if (!wallRooms.Contains(endDoor.ParentRoom))
                wallRooms.Add(endDoor.ParentRoom);
            foreach (var room in wallRooms)
            {
                if (room.Map == null)
                    continue;

                //retrieve wall positions from dictionary
                if (_mapWallDict.TryGetValue(room.Map, out var mapWalls))
                {
                    foreach (var wall in mapWalls)
                    {
                        var wallPos = wall + room.Position;

                        //finalWalls.Add(((wallPos - graphOffset) / 16).ToPoint());
                        //continue;

                        ////extra walls for path padding
                        //for (int x = -_wallPadding; x <= _wallPadding; x++)
                        //{
                        //    var paddedPos = wallPos + new Vector2(x * 16, 0);

                        //    if (reservedPositions.Contains(paddedPos))
                        //        continue;

                        //    //if (graphRect.Contains(paddedPos))
                        //    finalWalls.Add(((paddedPos - graphOffset) / 16).ToPoint());
                        //}
                        //for (int y = -_wallPadding; y <= _wallPadding; y++)
                        //{
                        //    var paddedPos = wallPos + new Vector2(0, y * 16);

                        //    if (reservedPositions.Contains(paddedPos))
                        //        continue;

                        //    //if (graphRect.Contains(paddedPos))
                        //    finalWalls.Add(((paddedPos - graphOffset) / 16).ToPoint());
                        //}

                        for (int x = -_wallPadding; x <= _wallPadding; x++)
                        {
                            for (int y = -_wallPadding; y <= _wallPadding; y++)
                            {
                                var paddedPos = wallPos + new Vector2(x * 16, y * 16);
                                if (reservedPositions.Contains(paddedPos))
                                    continue;
                                if (graphRect.Contains(paddedPos))
                                    finalWalls.Add(((paddedPos - graphOffset) / 16).ToPoint());
                            }
                        }
                    }
                }
            }

            //create pathfinding graph and add walls
            var graph = new AstarGridGraph((int)graphRect.Width / 16, (int)graphRect.Height / 16);
            foreach (var wall in finalWalls.Distinct())
                graph.Walls.Add(wall);

            //search for path
            var pathfindResults = graph.Search(((actualStartPos - graphOffset) / 16).ToPoint(), ((actualEndPos - graphOffset) / 16).ToPoint());
            if (pathfindResults == null || pathfindResults.Count <= 0)
                return false;

            var pathPoints = pathfindResults.Select(p => (p.ToVector2() * 16) + graphOffset).ToList();

            //add to final path in proper order
            finalPath.AddRange(beginningPath);
            finalPath.AddRange(pathPoints);
            finalPath.AddRange(endPath);

            ////add pathfinding results to final path
            //finalPath.AddRange(pathfindResults.Select(p => (p.ToVector2() * 16) + graphOffset));

            ////add path section leading up to end door
            //for (int i = _minHallDistance; i >= Mathf.CeilToInt(_minHallDistance / 2); i--)
            //{
            //    finalPath.Add(endDoor.Position + (endDoor.Direction * 16 * i));
            //}

            //translate path positions into corridor tiles and add them to the start door's room
            List<CorridorTile> corridorPath = finalPath.Select(p => new CorridorTile(startDoor.ParentRoom, p - startDoor.ParentRoom.Position)).ToList();
            //List<CorridorTile> path = pathfindResults.Select(p => new CorridorTile(startDoor.ParentRoom, (p.ToVector2() * 16) + graphOffset - startDoor.ParentRoom.Position)).ToList();
            startDoor.ParentRoom.CorridorTiles.AddRange(corridorPath);

            return true;
        }
    }
}
