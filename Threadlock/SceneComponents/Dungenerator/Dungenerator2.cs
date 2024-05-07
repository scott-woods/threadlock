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
using Threadlock.Components.TiledComponents;
using Threadlock.Helpers;
using Threadlock.Models;
using DoorwayPoint = Threadlock.Models.DoorwayPoint;

namespace Threadlock.SceneComponents.Dungenerator
{
    public class Dungenerator2 : SceneComponent
    {
        const int _graphRectPad = 5;
        const int _minRoomDistance = 4;
        const int _maxRoomDistance = 12;

        List<TmxMap> _allMaps = new List<TmxMap>();
        Dictionary<TmxMap, List<Vector2>> _mapWallDict = new Dictionary<TmxMap, List<Vector2>>();

        List<DungeonComposite2> _loopComposites = new List<DungeonComposite2>();
        List<DungeonComposite2> _treeComposites = new List<DungeonComposite2>();
        List<DungeonComposite2> _allComposites { get => _treeComposites.Concat(_loopComposites).ToList(); }

        List<DungeonRoom> _allRooms { get => _allComposites.SelectMany(c => c.DungeonRooms).ToList(); }

        public Dungenerator2()
        {
            //get data from maps
            foreach (var map in _allMaps)
            {
                _mapWallDict[map] = new List<Vector2>();

                var tiles = map.TileLayers.Where(l => l.Name.StartsWith("Walls")).SelectMany(l => l.Tiles.Where(t => t != null));
                foreach (var tile in tiles)
                    _mapWallDict[map].Add(new Vector2(tile.X * tile.Tileset.TileWidth, tile.Y * tile.Tileset.TileHeight));
            }
        }

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
                        var map = Scene.Content.LoadTiledMap(value);
                        _allMaps.Add(map);

                        mapDictionary.Add(map, value);
                    }
                }
            }

            var attempts = 0;
            while (attempts < 100)
            {
                attempts++;

                foreach (var dungeonRoom in _allRooms)
                    dungeonRoom.Reset();

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

                        //do one final check for overlaps
                        bool overlapFound = false;
                        foreach (var room1 in processedRooms)
                        {
                            foreach (var room2 in processedRooms)
                            {
                                if (room1 == room2)
                                    continue;

                                if (room1.Position == room2.Position)
                                {
                                    overlapFound = true;
                                    break;
                                }

                                if (room1.CollisionBounds.Intersects(room2.CollisionBounds))
                                {
                                    overlapFound = true;
                                    break;
                                }
                            }

                            if (overlapFound)
                                break;
                        }

                        if (overlapFound)
                            continue;

                        break;
                    }
                }

                //connect composites
                List<DungeonComposite2> processedComposites = new List<DungeonComposite2>();
                bool compositeFailed = false;
                foreach (var composite in _allComposites.OrderByDescending(c => c.GetRoomsFromChildrenComposites().Count))
                {
                    var parentRooms = composite.DungeonRooms.Where(r => r.ChildrenOutsideComposite.Any());
                    foreach (var parentRoom in parentRooms)
                    {
                        foreach (var child in parentRoom.ChildrenOutsideComposite)
                        {
                            var roomsToCheck = processedComposites
                                .SelectMany(c => c.DungeonRooms)
                                .Concat(composite.DungeonRooms)
                                .Distinct()
                                .ToList();

                            var roomsToMove = new List<DungeonRoom>(child.ParentComposite.DungeonRooms);

                            if (!ConnectRooms(parentRoom, child, roomsToCheck, roomsToMove))
                            {
                                compositeFailed = true;
                                break;
                            }
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

        public void FinalizeDungeon()
        {
            foreach (var room in _allRooms)
            {
                if (room.Map != null)
                {
                    var ent = Scene.CreateEntity($"dungeon-room-${room.Id}", room.Position);
                    TiledHelper.SetupMap(ent, room.Map);
                }
            }

            var corridorPositions = _allRooms.SelectMany(r => r.CorridorTiles.Select(t => t.Position)).ToList();
            foreach (var pos in corridorPositions)
            {
                var ent = Scene.CreateEntity("", pos);
                ent.AddComponent(new PrototypeSpriteRenderer(2, 2));
            }
        }

        bool ConnectRooms(DungeonRoom startRoom, DungeonRoom endRoom, List<DungeonRoom> roomsToCheck)
        {
            return ConnectRooms(startRoom, endRoom, roomsToCheck, new List<DungeonRoom> { endRoom });
        }

        bool ConnectRooms(DungeonRoom startRoom, DungeonRoom endRoom, List<DungeonRoom> roomsToCheck, List<DungeonRoom> roomsToMove)
        {
            var pairs = from d1 in startRoom.Doorways.Where(d => !d.HasConnection)
                        from d2 in endRoom.Doorways.Where(d => !d.HasConnection)
                        select new Tuple<DoorwayPoint, DoorwayPoint>(d1, d2);

            Dictionary<Vector2, List<Tuple<DoorwayPoint, DoorwayPoint>>> validPositions = new Dictionary<Vector2, List<Tuple<DoorwayPoint, DoorwayPoint>>>();

            for (int x = -_maxRoomDistance; x <= _maxRoomDistance; x++)
            {
                if (Math.Abs(x) <= _minRoomDistance)
                    continue;
                for (int y = -_maxRoomDistance; y <= _maxRoomDistance; y++)
                {
                    if (Math.Abs(y) <= _minRoomDistance)
                        continue;

                    var dist = new Vector2(x * 16, y * 16);

                    var pairsList = pairs.ToList();

                    while (pairsList.Any())
                    {
                        var pair = pairsList.RandomItem();
                        pairsList.Remove(pair);

                        var targetPos = pair.Item1.Position + dist;
                        var movement = targetPos - pair.Item2.Position;
                        for (int i = 0; i < roomsToMove.Count; i++)
                        {
                            var room = roomsToMove[i];
                            room.Position += movement;
                        }

                        //check for overlaps
                        if (roomsToCheck.Any(r => r.Map != null && roomsToMove.Any(x => x.Map != null && x.CollisionBounds.Intersects(r.CollisionBounds))))
                            continue;

                        if (!validPositions.ContainsKey(pair.Item2.ParentRoom.Position))
                            validPositions[pair.Item2.ParentRoom.Position] = new List<Tuple<DoorwayPoint, DoorwayPoint>>();
                        validPositions[pair.Item2.ParentRoom.Position].Add(pair);
                        break;
                    }
                }
            }

            while (validPositions.Any())
            {
                var pos = validPositions.Keys.ToList().RandomItem();

                var movement = pos - endRoom.Position;

                for (int i = 0; i < roomsToMove.Count; i++)
                {
                    var room = roomsToMove[i];
                    room.Position += movement;
                }

                var possiblePairs = validPositions[pos];

                while (possiblePairs.Any())
                {
                    var pair = possiblePairs.RandomItem();

                    if (ConnectDoorways(pair.Item1, pair.Item2, roomsToCheck))
                    {
                        pair.Item1.HasConnection = true;
                        pair.Item2.HasConnection = true;
                        break;
                    }

                    possiblePairs.Remove(pair);
                }

                if (possiblePairs.Any())
                    break;

                validPositions.Remove(pos);
            }

            return validPositions.Any();
        }

        bool ConnectDoorways(DoorwayPoint startDoor, DoorwayPoint endDoor, List<DungeonRoom> roomsToCheck)
        {
            //get rect around doorways
            var rectX = Math.Min(startDoor.Position.X, endDoor.Position.X) - (_graphRectPad * 16);
            var rectY = Math.Min(startDoor.Position.Y, endDoor.Position.Y) - (_graphRectPad * 16);
            var rectRight = Math.Max(startDoor.Position.X, endDoor.Position.X) + (_graphRectPad * 16);
            var rectBottom = Math.Max(startDoor.Position.Y, endDoor.Position.Y) + (_graphRectPad * 16);
            var rectWidth = rectRight - rectX;
            var rectHeight = rectBottom - rectY;

            //create graph rect
            var graphRect = new RectangleF(rectX, rectY, rectWidth, rectHeight);
            var graphOffset = graphRect.Location;
            List<Point> finalWalls = new List<Point>();

            //get walls from each room
            foreach (var room in roomsToCheck)
            {
                if (room.Map == null)
                    continue;
                if (_mapWallDict.TryGetValue(room.Map, out var mapWalls))
                {
                    foreach (var wall in mapWalls)
                    {
                        var wallPos = wall + room.Position;

                        if (wallPos == startDoor.Position || wallPos == endDoor.Position)
                            continue;

                        if (graphRect.Contains(wallPos))
                            finalWalls.Add(((wallPos - graphOffset) / 16).ToPoint());
                    }
                }
            }

            var graph = new AstarGridGraph((int)graphRect.Width, (int)graphRect.Height);
            foreach (var wall in finalWalls)
                graph.Walls.Add(wall);

            var pathfindResults = graph.Search(((startDoor.Position - graphOffset) / 16).ToPoint(), ((endDoor.Position - graphOffset) / 16).ToPoint());
            if (pathfindResults == null || pathfindResults.Count <= 0)
                return false;

            List<CorridorTile> path = pathfindResults.Select(p => new CorridorTile(startDoor.ParentRoom, (p.ToVector2() * 16) + graphOffset - startDoor.ParentRoom.Position)).ToList();
            startDoor.ParentRoom.CorridorTiles.AddRange(path);

            return true;
        }
    }
}
