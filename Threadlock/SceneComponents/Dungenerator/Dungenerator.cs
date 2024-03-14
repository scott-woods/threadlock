using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez;
using Nez.AI.Pathfinding;
using Nez.Persistence;
using Nez.Tiled;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Threadlock.Components;
using Threadlock.Components.TiledComponents;
using Threadlock.Entities;
using Threadlock.Helpers;
using Threadlock.Models;
using Threadlock.StaticData;
using static Threadlock.StaticData.Tiles.Forge;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using RectangleF = Nez.RectangleF;

namespace Threadlock.SceneComponents.Dungenerator
{
    public class Dungenerator : SceneComponent
    {
        const int _maxAttempts = 100;
        List<DungeonRoomEntity> _allMapEntities = new List<DungeonRoomEntity>();
        List<DungeonComposite> _allComposites = new List<DungeonComposite>();
        List<TmxMap> _allMaps = new List<TmxMap>();

        public void Generate()
        {
            //read flow file
            DungeonFlow flow = new DungeonFlow();
            if (File.Exists("Content/Data/DungeonFlows.json"))
            {
                var json = File.ReadAllText("Content/Data/DungeonFlows.json");
                flow = Json.FromJson<DungeonFlow>(json);
            }
            else return;

            //get composites
            var dungeonGraph = new DungeonGraph();
            dungeonGraph.ProcessGraph(flow.Nodes);
            foreach (var loop in dungeonGraph.Loops)
            {
                var composite = new DungeonComposite(loop, DungeonCompositeType.Loop);
                _allComposites.Add(composite);
            }
            foreach (var tree in dungeonGraph.Trees)
            {
                var composite = new DungeonComposite(tree, DungeonCompositeType.Tree);
                _allComposites.Add(composite);
            }

            //load all maps for this area
            Dictionary<TmxMap, string> mapDictionary = new Dictionary<TmxMap, string>(); //dictionary so unused maps can be unloaded
            Type forgeType = typeof(Content.Tiled.Tilemaps.Forge);
            FieldInfo[] fields = forgeType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
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
            while (attempts < _maxAttempts)
            {
                //increment attempts
                attempts++;

                //handle tree composites
                var treeSuccess = HandleTrees(_allComposites.Where(c => c.CompositeType == DungeonCompositeType.Tree).ToList());
                if (!treeSuccess)
                {
                    foreach (var composite in _allComposites)
                    {
                        composite.Reset();
                    }
                    _allMapEntities.Clear();
                    continue;
                }

                //handle loop composites
                var loopSuccess = HandleLoops(_allComposites.Where(c => c.CompositeType == DungeonCompositeType.Loop).ToList());
                if (!loopSuccess)
                {
                    foreach (var composite in _allComposites)
                    {
                        composite.Reset();
                    }
                    _allMapEntities.Clear();
                    continue;
                }

                //connect composites
                var connectionSuccessful = ConnectComposites();
                if (!connectionSuccessful)
                {
                    foreach (var composite in _allComposites)
                    {
                        composite.Reset();
                    }
                    _allMapEntities.Clear();
                    continue;
                }

                //paint corridor tiles
                var allFloorPositions = _allComposites
                    .SelectMany(c => c.FloorTilePositions)
                    .Distinct()
                    .ToList();

                List<Vector2> reservedPositions = new List<Vector2>();
                foreach (var doorway in Scene.FindComponentsOfType<DungeonDoorway>().Where(d => d.HasConnection))
                {
                    reservedPositions.Add(doorway.PathfindingOrigin);
                    switch (doorway.Direction)
                    {
                        case "Top":
                        case "Bottom":
                            reservedPositions.Add(doorway.PathfindingOrigin + (DirectionHelper.Left * 16));
                            reservedPositions.Add(doorway.PathfindingOrigin + (DirectionHelper.Right * 16));
                            break;
                        case "Left":
                        case "Right":
                            reservedPositions.Add(doorway.PathfindingOrigin + (DirectionHelper.Up * 16));
                            reservedPositions.Add(doorway.PathfindingOrigin + (DirectionHelper.Down * 16));
                            break;
                    }
                }

                //open tileset
                using (var stream = TitleContainer.OpenStream(Content.Tiled.Tilesets.Forge_tileset))
                {
                    var xDocTileset = XDocument.Load(stream);

                    string tsxDir = Path.GetDirectoryName(Content.Tiled.Tilesets.Forge_tileset);
                    var tileset = new TmxTileset().LoadTmxTileset(null, xDocTileset.Element("tileset"), 0, tsxDir);
                    tileset.TmxDirectory = tsxDir;

                    //var tileRenderers = CorridorPainter.PaintFloorTiles(floorPositions, tileset, endEntity);
                    var tileRenderers = CorridorPainter.PaintCorridorTiles(allFloorPositions, reservedPositions, tileset);
                }

                break;
            }

            //unload unused maps
            foreach (var map in _allMaps)
            {
                if (!_allMapEntities.Any(m => m.Map == map))
                {
                    if (mapDictionary.TryGetValue(map, out var name))
                    {
                        Scene.Content.UnloadAsset<TmxMap>(name);
                    }
                }
            }
        }

        #region HANDLE LOOPS/TREES

        bool HandleTrees(List<DungeonComposite> trees)
        {
            foreach (var tree in trees)
            {
                //rooms that have been processed
                var processedRooms = new List<DungeonRoomEntity>();

                var attempts = 0;
                while (attempts < _maxAttempts)
                {
                    attempts++;

                    DungeonRoomEntity prevMapEntity = null;
                    bool treeSuccess = true;
                    foreach (var roomEntity in tree.RoomEntities)
                    {
                        var possibleMaps = GetValidMaps(roomEntity);

                        //try maps until a valid one is found
                        while (possibleMaps.Count > 0)
                        {
                            //pick a random map
                            var map = possibleMaps.RandomItem();

                            //create map
                            roomEntity.CreateMap(map);

                            //only validate for previous node if it isn't null (ie this isn't the first in composite)
                            if (prevMapEntity != null)
                            {
                                //get doorways
                                var prevNodeDoorways = prevMapEntity.FindComponentsOnMap<DungeonDoorway>();
                                var newNodeDoorways = roomEntity.FindComponentsOnMap<DungeonDoorway>();

                                //get possible doorway pairs
                                var pairsList = GetValidDoorwayPairs(prevNodeDoorways, newNodeDoorways);

                                while (pairsList.Count > 0)
                                {
                                    //pick a random doorway pair
                                    var pair = pairsList.RandomItem();

                                    //get the ideal position for the new room entity based on the pair
                                    var movement = GetMovementToAlignDoorways(pair.Item1, pair.Item2);

                                    //set the map's position
                                    roomEntity.Position += movement;

                                    //check for overlap with already processed rooms
                                    if (processedRooms.Any(r => roomEntity.OverlapsRoom(r)))
                                    {
                                        //overlap found, remove pair and try again
                                        pairsList.Remove(pair);
                                        continue;
                                    }

                                    //pair successfully validated. Set doorways as open
                                    pair.Item1.SetOpen(true);
                                    pair.Item2.SetOpen(true);

                                    break;
                                }

                                //room not placed for any pair. map is invalid.
                                if (pairsList.Count == 0)
                                {
                                    //destroy map entity
                                    roomEntity.ClearMap();

                                    //remove the invalid map from possible maps list
                                    possibleMaps.Remove(map);

                                    //try again
                                    continue;
                                }
                            }

                            //map validation successful
                            break;
                        }

                        //no maps were valid, try making tree again
                        if (possibleMaps.Count == 0)
                        {
                            treeSuccess = false;
                            break;
                        }

                        //add entity to list
                        processedRooms.Add(roomEntity);

                        //update prev map entity for next loop iteration
                        prevMapEntity = roomEntity;
                    }

                    if (!treeSuccess)
                    {
                        foreach (var room in processedRooms)
                            room.ClearMap();

                        processedRooms.Clear();

                        continue;
                    }

                    //finished with tree. add all map entities from this tree to the total list
                    _allMapEntities.AddRange(processedRooms);

                    break;
                }

                if (attempts == _maxAttempts)
                    return false;
            }

            return true;
        }

        bool HandleLoops(List<DungeonComposite> loops)
        {
            //loop through each loop composite
            foreach (var loop in loops)
            {
                //processed rooms
                var processedRooms = new List<DungeonRoomEntity>();

                //try creating the loop
                var attempts = 0;
                while (attempts < _maxAttempts)
                {
                    //increment attempts
                    attempts++;

                    //create rooms/maps
                    bool roomsSuccessfullyCreated = true;
                    DungeonRoomEntity prevMapEntity = null;
                    for (int i = 0; i < loop.RoomEntities.Count; i++)
                    {
                        //grab room entity
                        var roomEntity = loop.RoomEntities[i];

                        //get potential maps
                        var possibleMaps = GetValidMaps(roomEntity);

                        //try maps until a valid one is found
                        while (possibleMaps.Count > 0)
                        {
                            //pick a random map
                            var map = possibleMaps.RandomItem();

                            //create map
                            roomEntity.CreateMap(map);

                            //validate for previous node if it isn't null (ie this isn't the first in composite)
                            if (prevMapEntity != null)
                            {
                                //get doorways
                                var prevNodeDoorways = prevMapEntity.FindComponentsOnMap<DungeonDoorway>();
                                var newNodeDoorways = roomEntity.FindComponentsOnMap<DungeonDoorway>();

                                //get possible doorway pairs
                                var pairsList = GetValidDoorwayPairs(prevNodeDoorways, newNodeDoorways, false);

                                //sort by distance to starting room
                                //if (i > 0)
                                //{
                                //    pairsList = pairsList.OrderBy(p => Vector2.Distance(p.Item1.Entity.Position, processedRooms.First().Position)).ToList();
                                //}

                                //try to find a valid pair
                                while (pairsList.Count > 0)
                                {
                                    //pick a pair based on where we are in the loop
                                    Tuple<DungeonDoorway, DungeonDoorway> pair = null;
                                    if (i >= loop.RoomEntities.Count / 2)
                                    {
                                        var min = pairsList.Select(p => Vector2.Distance(p.Item1.PathfindingOrigin, processedRooms.First().Position)).Min();
                                        var minPairs = pairsList.Where(p => Vector2.Distance(p.Item1.PathfindingOrigin, processedRooms.First().Position) == min).ToList();
                                        pair = minPairs.RandomItem();
                                    }
                                    else
                                        pair = pairsList.RandomItem();
                                    //if (i == 0)
                                    //    pair = pairsList.RandomItem();
                                    //else if (i < loop.RoomEntities.Count / 2)
                                    //    pair = pairsList.Last();
                                    //else if (i >= loop.RoomEntities.Count / 2)
                                    //    pair = pairsList.First();
                                    //else
                                    //    pair = pairsList.RandomItem();

                                    //get the ideal position for the new room entity based on the pair
                                    var movement = GetMovementToAlignDoorways(pair.Item1, pair.Item2);

                                    //set the map's position
                                    roomEntity.Position += movement;

                                    //check if the new room would overlap any previously processed rooms
                                    if (processedRooms.Any(r => roomEntity.OverlapsRoom(r)))
                                    {
                                        //overlap found, remove pair and try again
                                        pairsList.Remove(pair);
                                        continue;
                                    }

                                    //pair successfully validated. Set doorways as open
                                    pair.Item1.SetOpen(true);
                                    pair.Item2.SetOpen(true);

                                    break;
                                }

                                //no pairs were valid, try another map
                                if (pairsList.Count == 0)
                                {
                                    //destroy map entity
                                    roomEntity.ClearMap();

                                    //remove the invalid map from possible maps list
                                    possibleMaps.Remove(map);

                                    //try again
                                    continue;
                                }
                            }

                            //map validation successful
                            break;
                        }

                        //no maps were valid
                        if (possibleMaps.Count == 0)
                        {
                            roomsSuccessfullyCreated = false;
                            break;
                        }

                        //add entity to list
                        processedRooms.Add(roomEntity);

                        //update prev map entity for next loop iteration
                        prevMapEntity = roomEntity;
                    }

                    //if error occurred while creating rooms, move to next attempt
                    if (!roomsSuccessfullyCreated)
                    {
                        //clear any processed rooms
                        foreach (var room in processedRooms)
                        {
                            room.ClearMap();
                        }
                        processedRooms.Clear();

                        continue;
                    }

                    //CLOSE THE LOOP

                    //adjust room positions to prepare for pathfinding
                    //loop.AdjustForPathfinding(25);

                    //handle connecting end of loop to beginning
                    var startEntity = processedRooms.First();
                    var endEntity = processedRooms.Last();

                    //get pairs and order by distance between each other
                    var pairs = GetValidDoorwayPairs(endEntity.FindComponentsOnMap<DungeonDoorway>(), startEntity.FindComponentsOnMap<DungeonDoorway>(), false);
                    pairs = pairs.OrderBy(p => Vector2.Distance(p.Item1.Entity.Position, p.Item2.Entity.Position)).ToList();

                    //try pairs
                    while (pairs.Count > 0)
                    {
                        //pick first pair
                        var pair = pairs.First();

                        var graph = loop.GetPathfindingGraph(out var paddedRect);

                        //try to find a path between the doorways
                        if (CorridorGenerator.ConnectDoorways(pair.Item1, pair.Item2, graph, processedRooms, paddedRect, out List<Vector2> floorPositions))
                        {
                            //found a valid path, set doorways as open
                            pair.Item1.SetOpen(true);
                            pair.Item2.SetOpen(true);

                            loop.FloorTilePositions.AddRange(floorPositions);
                        }
                        else
                        {
                            //pair was invalid, remove and try again
                            pairs.Remove(pair);
                            continue;
                        }

                        break;
                    }

                    //if no pairs were valid, try another map
                    if (pairs.Count == 0)
                    {
                        //clear any processed rooms
                        foreach (var room in processedRooms)
                        {
                            room.ClearMap();
                        }
                        processedRooms.Clear();

                        continue;
                    }

                    break;
                }

                if (attempts == _maxAttempts)
                    return false;

                //finished with loop. add all map entities from this loop to the total list
                _allMapEntities.AddRange(processedRooms);
            }

            return true;
        }

        #endregion

        #region COMPOSITES

        bool ConnectComposites()
        {
            var compositesToCheck = new List<DungeonComposite>();

            foreach (var composite in _allComposites.OrderByDescending(c => c.GetRoomsFromChildrenComposites().Count))
            {
                compositesToCheck.Add(composite);

                var parentRooms = composite.RoomEntities
                    .Where(r => r.ChildrenOutsideComposite != null && r.ChildrenOutsideComposite.Count > 0)
                    .OrderByDescending(r => r.ChildrenOutsideComposite.Count).ToList();

                foreach (var room in parentRooms)
                {
                    //handle connections in random order
                    var children = room.ChildrenOutsideComposite;
                    children.Shuffle();

                    //loop through children that aren't in this composite
                    foreach (var childEntity in children)
                    {
                        var roomsToCheck = compositesToCheck.SelectMany(c => c.RoomEntities).ToList();

                        //get doorways
                        var parentNodeDoorways = room.FindComponentsOnMap<DungeonDoorway>();
                        var childNodeDoorways = childEntity.FindComponentsOnMap<DungeonDoorway>();

                        //get possible doorway pairs
                        var pairsList = GetValidDoorwayPairs(parentNodeDoorways, childNodeDoorways, false)
                            .OrderByDescending(p => p.Item1.IsDirectMatch(p.Item2))
                            .ThenByDescending(p => p.Item1.Direction != p.Item2.Direction)
                            .ToList();

                        while (pairsList.Count > 0)
                        {
                            //pick random pair, preference for perfect pairs
                            var pair = pairsList.Any(p => p.Item1.IsDirectMatch(p.Item2))
                                ? pairsList.Where(p => p.Item1.IsDirectMatch(p.Item2)).ToList().RandomItem()
                                : pairsList.RandomItem();

                            var minDistance = 8;
                            var maxDistance = 25;
                            List<Vector2> possibleTiles = new List<Vector2>();

                            //define the range for X and Y based on direction
                            int xStart, xEnd, yStart, yEnd;
                            switch (pair.Item1.Direction)
                            {
                                case "Top":
                                    xStart = -maxDistance; xEnd = maxDistance;
                                    yStart = -maxDistance; yEnd = -minDistance;
                                    break;
                                case "Bottom":
                                    xStart = -maxDistance; xEnd = maxDistance;
                                    yStart = minDistance; yEnd = maxDistance;
                                    break;
                                case "Left":
                                    xStart = -maxDistance; xEnd = -minDistance;
                                    yStart = -maxDistance; yEnd = maxDistance;
                                    break;
                                case "Right":
                                    xStart = minDistance; xEnd = maxDistance;
                                    yStart = -maxDistance; yEnd = maxDistance;
                                    break;
                                default:
                                    throw new InvalidOperationException("Invalid direction");
                            }

                            //iterate over the defined ranges
                            for (int y = yStart; y <= yEnd; y++)
                            {
                                for (int x = xStart; x <= xEnd; x++)
                                {
                                    if ((pair.Item1.Direction == "Top" || pair.Item1.Direction == "Bottom") && Math.Abs(x) >= minDistance ||
                                        (pair.Item1.Direction == "Left" || pair.Item1.Direction == "Right") && Math.Abs(y) >= minDistance)
                                    {
                                        possibleTiles.Add(new Vector2(x, y));
                                    }
                                }
                            }

                            var childCompRect = childEntity.ParentComposite.Bounds;
                            List<Vector2> tilesToRemove = new List<Vector2>();

                            foreach (var possibleTile in possibleTiles)
                            {
                                if (roomsToCheck.Any(r => r.Bounds.Contains(possibleTile)))
                                {
                                    tilesToRemove.Add(possibleTile);
                                    continue;
                                }

                                var tileWorldDistance = possibleTile * 16;
                                var idealDoorwayOriginPos = pair.Item1.PathfindingOrigin + tileWorldDistance;
                                var movementAmount = idealDoorwayOriginPos - pair.Item2.PathfindingOrigin;

                                var projectedRect = new RectangleF(childCompRect.Location, childCompRect.Size);
                                projectedRect.Location += movementAmount;

                                if (roomsToCheck.Any(r => r.Bounds.Intersects(projectedRect)))
                                {
                                    tilesToRemove.Add(possibleTile);
                                    continue;
                                }
                            }

                            possibleTiles = possibleTiles.Except(tilesToRemove).ToList();

                            //try all possible tiles
                            while (possibleTiles.Count > 0)
                            {
                                var tileDistance = possibleTiles.RandomItem();

                                var tileWorldDistance = tileDistance * 16;

                                var idealDoorwayOriginPos = pair.Item1.PathfindingOrigin + tileWorldDistance;
                                var movementAmount = idealDoorwayOriginPos - pair.Item2.PathfindingOrigin;
                                childEntity.ParentComposite.MoveRooms(movementAmount, false);

                                if (childEntity.ParentComposite.RoomEntities.Any(childRoom =>
                                {
                                    return roomsToCheck.Any(r => childRoom.OverlapsRoom(r));
                                }))
                                {
                                    possibleTiles.Remove(tileDistance);
                                    continue;
                                }
                                else
                                {
                                    //at this point, we know rooms don't directly overlap. time to try making a path between them

                                    var tilePadding = 8;
                                    var joinedRect = Rectangle.Union(childEntity.ParentComposite.Bounds, room.ParentComposite.Bounds);
                                    joinedRect.X -= tilePadding * 16;
                                    joinedRect.Y -= tilePadding * 16;
                                    joinedRect.Width += (tilePadding * 16 * 2);
                                    joinedRect.Height += (tilePadding * 16 * 2);

                                    var graph = new WeightedGridGraph(joinedRect.Width / 16, joinedRect.Height / 16);
                                    foreach (var graphRoom in room.ParentComposite.RoomEntities.Concat(childEntity.ParentComposite.RoomEntities))
                                    {
                                        var collisionRenderer = graphRoom.GetComponents<TiledMapRenderer>().FirstOrDefault(r => r.CollisionLayer != null);
                                        if (collisionRenderer == null)
                                            continue;

                                        //add wall for each collision tile
                                        for (var y = 0; y < collisionRenderer.CollisionLayer.Map.Height / 16; y++)
                                        {
                                            for (var x = 0; x < collisionRenderer.CollisionLayer.Map.Width / 16; x++)
                                            {
                                                if (collisionRenderer.CollisionLayer.GetTile(x, y) != null)
                                                {
                                                    var tileWorldPos = new Point(x, y) + (graphRoom.Position / 16).ToPoint();
                                                    graph.Walls.Add(tileWorldPos - (joinedRect.Location.ToVector2() / 16).ToPoint());
                                                }
                                            }
                                        }

                                        var doorways = graphRoom.FindComponentsOnMap<DungeonDoorway>();
                                        if (doorways != null)
                                        {
                                            foreach (var doorway in doorways)
                                            {
                                                for (var y = 0; y < doorway.TmxObject.Height / 16; y++)
                                                {
                                                    for (var x = 0; x < doorway.TmxObject.Width / 16; x++)
                                                    {
                                                        var tilePos = new Vector2(x, y);
                                                        var adjustedTilePos = tilePos + (doorway.Entity.Position / 16);
                                                        graph.Walls.Add(adjustedTilePos.ToPoint() - (joinedRect.Location.ToVector2() / 16).ToPoint());

                                                        //var tilePoint = adjustedTilePos.ToPoint();
                                                        //if (!graph.Walls.Contains(tilePoint))
                                                        //    graph.Walls.Add(tilePoint);
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    //get pathfinding graph
                                    var roomsForGraph = roomsToCheck.Concat(childEntity.ParentComposite.RoomEntities).ToList();
                                    //var graph = CreateDungeonGraph(roomsForGraph);

                                    //try to find a path between the doorways
                                    if (CorridorGenerator.ConnectDoorways(pair.Item1, pair.Item2, graph, roomsForGraph, new RectangleF(joinedRect.Location.X, joinedRect.Location.Y, joinedRect.Width, joinedRect.Height), out List<Vector2> floorPositions))
                                    {
                                        //found a valid path, set doorways as open
                                        pair.Item1.SetOpen(true);
                                        pair.Item2.SetOpen(true);

                                        room.ParentComposite.FloorTilePositions.AddRange(floorPositions);

                                        break;
                                    }
                                    else
                                    {
                                        possibleTiles.Remove(tileDistance);
                                        continue;
                                    }
                                }
                            }

                            if (possibleTiles.Count == 0)
                            {
                                pairsList.Remove(pair);
                                continue;
                            }

                            break;
                        }

                        if (pairsList.Count == 0)
                            return false;
                    }
                }
            }

            return true;
        }

        #endregion

        #region HELPERS

        public List<TmxMap> GetValidMaps(DungeonRoomEntity room)
        {
            List<TmxMap> possibleMaps = new List<TmxMap>();

            //get potential maps
            var validMaps = _allMaps.Where(m =>
            {
                if (m.Properties == null)
                    return false;
                if (!m.Properties.ContainsKey("RoomType"))
                    return false;
                return m.Properties["RoomType"] == room.Type;
            });

            possibleMaps.AddRange(validMaps);
            return possibleMaps;
        }

        WeightedGridGraph CreateDungeonGraph(List<DungeonRoomEntity> roomsToCheck)
        {
            //prepare entire dungeon for pathfinding by moving bounds above 0, 0
            var top = _allComposites.Select(c => c.Bounds.Top).Min();
            var left = _allComposites.Select(c => c.Bounds.Left).Min();
            var topLeft = new Vector2(left, top);
            var desiredPos = Vector2.Zero + new Vector2(1, 1) * 16 * 25;
            var amountToMove = desiredPos - topLeft;
            foreach (var composite in _allComposites)
            {
                composite.MoveRooms(amountToMove, false);
            }

            var bottom = _allComposites.Select(c => c.Bounds.Bottom).Max();
            var right = _allComposites.Select(c => c.Bounds.Right).Max();

            var graph = new WeightedGridGraph((int)right / 16, (int)bottom / 16);

            foreach (var map in roomsToCheck)
            {
                var collisionRenderer = map.GetComponents<TiledMapRenderer>().FirstOrDefault(r => r.CollisionLayer != null);
                if (collisionRenderer != null)
                {
                    foreach (var tile in collisionRenderer.CollisionLayer.Tiles.Where(t => t != null))
                    {
                        var tileWorldPos = map.Position + new Vector2(tile.X * 16, tile.Y * 16);
                        var adjustedTilePos = tileWorldPos / 16;

                        var tilePoint = adjustedTilePos.ToPoint();
                        if (!graph.Walls.Contains(tilePoint))
                            graph.Walls.Add(tilePoint);
                    }
                }

                var doorways = map.FindComponentsOnMap<DungeonDoorway>();
                foreach (var doorway in doorways)
                {
                    for (int y = 0; y < doorway.TmxObject.Height / 16; y++)
                    {
                        for (int x = 0; x < doorway.TmxObject.Width / 16; x++)
                        {
                            var doorwayTileWorldPos = doorway.Entity.Position + new Vector2(x * 16, y * 16);
                            var adjustedDoorwayTilePos = doorwayTileWorldPos / 16;

                            var doorwayPoint = adjustedDoorwayTilePos.ToPoint();
                            if (!graph.Walls.Contains(doorwayPoint))
                                graph.Walls.Add(doorwayPoint);
                        }
                    }
                }
            }

            return graph;
        }

        Vector2 GetMovementToAlignDoorways(DungeonDoorway previousDoorway, DungeonDoorway nextDoorway)
        {
            var desiredDoorwayPos = previousDoorway.PathfindingOrigin;
            if (previousDoorway.Direction == "Top")
                desiredDoorwayPos.Y -= 16;
            else if (previousDoorway.Direction == "Bottom")
                desiredDoorwayPos.Y += 16;
            else if (previousDoorway.Direction == "Left")
                desiredDoorwayPos.X -= 16;
            else if (previousDoorway.Direction == "Right")
                desiredDoorwayPos.X += 16;

            return desiredDoorwayPos - nextDoorway.PathfindingOrigin;
        }

        List<Tuple<DungeonDoorway, DungeonDoorway>> GetValidDoorwayPairs(List<DungeonDoorway> room1Doorways, List<DungeonDoorway> room2Doorways, bool perfectMatchOnly = true)
        {
            var pairsList = new List<Tuple<DungeonDoorway, DungeonDoorway>>();
            var pairs = from d1 in room1Doorways.Where(d => !d.HasConnection)
                        from d2 in room2Doorways.Where(d => !d.HasConnection)
                        where !perfectMatchOnly || d1.IsDirectMatch(d2)
                        select new Tuple<DungeonDoorway, DungeonDoorway>(d1, d2);

            if (pairs.Any())
                pairsList = pairs.ToList();

            return pairsList;
        }

        #endregion
    }
}
