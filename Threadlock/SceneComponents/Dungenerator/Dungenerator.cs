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
using System.Xml.Linq;
using Threadlock.Components.TiledComponents;
using Threadlock.Entities;
using Threadlock.Helpers;
using Threadlock.Models;
using Threadlock.StaticData;
using static Threadlock.SceneComponents.Dungenerator.CorridorPainter;
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
                var allFloorPositions = _allMapEntities
                    .SelectMany(c => c.FloorTilePositions)
                    .Distinct()
                    .ToList();

                List<Vector2> reservedPositions = new List<Vector2>();
                foreach (var map in _allMapEntities)
                {
                    //get world position of every Back (floor) tile on this map
                    reservedPositions.AddRange(TiledHelper.GetTilePositionsByLayer(map, "Back"));
                }

                foreach (var doorway in Scene.FindComponentsOfType<DungeonDoorway>())
                {
                    if (doorway.HasConnection)
                        reservedPositions.AddRange(TiledHelper.GetTilePositionsByLayer(doorway.Entity, "Back"));
                    allFloorPositions.AddRange(TiledHelper.GetTilePositionsByLayer(doorway.Entity, "Fill"));
                }

                //GetPathfindingGraph(_allMapEntities, out var graph, out var graphRect, true);

                //List<Vector2> reservedPositions = new List<Vector2>();
                //foreach (var doorway in Scene.FindComponentsOfType<DungeonDoorway>().Where(d => d.HasConnection))
                //{
                //    reservedPositions.Add(doorway.PathfindingOrigin);
                //    switch (doorway.Direction)
                //    {
                //        case "Top":
                //        case "Bottom":
                //            reservedPositions.Add(doorway.PathfindingOrigin + (DirectionHelper.Left * 16));
                //            reservedPositions.Add(doorway.PathfindingOrigin + (DirectionHelper.Right * 16));
                //            break;
                //        case "Left":
                //        case "Right":
                //            reservedPositions.Add(doorway.PathfindingOrigin + (DirectionHelper.Up * 16));
                //            reservedPositions.Add(doorway.PathfindingOrigin + (DirectionHelper.Down * 16));
                //            break;
                //    }
                //}

                //open tileset
                using (var stream = TitleContainer.OpenStream(Content.Tiled.Tilesets.Forge_tileset))
                {
                    var xDocTileset = XDocument.Load(stream);

                    string tsxDir = Path.GetDirectoryName(Content.Tiled.Tilesets.Forge_tileset);
                    var tileset = new TmxTileset().LoadTmxTileset(null, xDocTileset.Element("tileset"), 0, tsxDir);
                    tileset.TmxDirectory = tsxDir;

                    //var tileRenderers = CorridorPainter.PaintFloorTiles(floorPositions, tileset, endEntity);
                    CorridorPainter.PaintCorridorTiles(allFloorPositions, reservedPositions, tileset);
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
                                var pairsList = GetValidDoorwayPairs(prevNodeDoorways, newNodeDoorways, false);

                                while (pairsList.Count > 0)
                                {
                                    //get a pair, preference for perfect pairs
                                    Tuple<DungeonDoorway, DungeonDoorway> pair;
                                    var perfectPairs = pairsList.Where(p => p.Item1.IsDirectMatch(p.Item2)).ToList();
                                    if (perfectPairs.Any())
                                        pair = perfectPairs.RandomItem();
                                    else
                                        pair = pairsList.RandomItem();

                                    //if perfect pair, try aligning without pathfinding
                                    //if (pair.Item1.IsDirectMatch(pair.Item2))
                                    //{
                                    //    if (ConnectPerfectPair(pair.Item1, pair.Item2, processedRooms))
                                    //    {
                                    //        pair.Item1.SetOpen(true);
                                    //        pair.Item2.SetOpen(true);

                                    //        break;
                                    //    }
                                    //    else
                                    //    {
                                    //        pairsList.Remove(pair);
                                    //        continue;
                                    //    }
                                    //}

                                    if (ConnectDoorways(pair.Item1, pair.Item2, processedRooms, new List<DungeonRoomEntity> { roomEntity }))
                                        break;
                                    else
                                    {
                                        pairsList.Remove(pair);
                                    }

                                    ////get the ideal position for the new room entity based on the pair
                                    //var movement = GetMovementToAlignDoorways(pair.Item1, pair.Item2);

                                    ////set the map's position
                                    //roomEntity.Position += movement;

                                    ////check for overlap with already processed rooms
                                    //if (processedRooms.Any(r => roomEntity.OverlapsRoom(r)))
                                    //{
                                    //    //overlap found, remove pair and try again
                                    //    pairsList.Remove(pair);
                                    //    continue;
                                    //}

                                    //pair successfully validated. Set doorways as open
                                    //pair.Item1.SetOpen(true);
                                    //pair.Item2.SetOpen(true);

                                    //break;
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
                        tree.Reset();
                        //foreach (var room in processedRooms)
                        //    room.ClearMap();

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
                                    else if (pairsList.Any(p => p.Item1.IsDirectMatch(p.Item2)))
                                        pair = pairsList.Where(p => p.Item1.IsDirectMatch(p.Item2)).ToList().RandomItem();
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

                                    ////get the ideal position for the new room entity based on the pair
                                    //var movement = GetMovementToAlignDoorways(pair.Item1, pair.Item2);

                                    ////set the map's position
                                    //roomEntity.Position += movement;

                                    ////check if the new room would overlap any previously processed rooms
                                    //if (processedRooms.Any(r => roomEntity.OverlapsRoom(r)))
                                    //{
                                    //    //overlap found, remove pair and try again
                                    //    pairsList.Remove(pair);
                                    //    continue;
                                    //}

                                    //if perfect pair, try aligning without pathfinding
                                    //if (pair.Item1.IsDirectMatch(pair.Item2))
                                    //{
                                    //    if (ConnectPerfectPair(pair.Item1, pair.Item2, processedRooms))
                                    //    {
                                    //        pair.Item1.SetOpen(true);
                                    //        pair.Item2.SetOpen(true);

                                    //        break;
                                    //    }
                                    //    else
                                    //    {
                                    //        pairsList.Remove(pair);
                                    //        continue;
                                    //    }
                                    //}

                                    if (ConnectDoorways(pair.Item1, pair.Item2, processedRooms, new List<DungeonRoomEntity> { roomEntity }))
                                        break;
                                    else
                                    {
                                        pairsList.Remove(pair);
                                    }

                                    //var graph = loop.GetPathfindingGraph(out var graphRect);
                                    //var roomsToCheck = new List<DungeonRoomEntity>() { roomEntity };
                                    //roomsToCheck.AddRange(processedRooms);
                                    //if (CorridorGenerator.ConnectDoorways(pair.Item1, pair.Item2, graph, roomsToCheck, graphRect, out var floorPositions))
                                    //{
                                    //    pair.Item1.SetOpen(true);
                                    //    pair.Item2.SetOpen(true);

                                    //    break;
                                    //}

                                    ////pair successfully validated. Set doorways as open
                                    //pair.Item1.SetOpen(true);
                                    //pair.Item2.SetOpen(true);

                                    //break;
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
                        loop.Reset();
                        //foreach (var room in processedRooms)
                        //{
                        //    room.ClearMap();
                        //}
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

                        GetPathfindingGraph(processedRooms, out var graph, out var graphRect);

                        //try to find a path between the doorways
                        if (CorridorGenerator.ConnectDoorways(pair.Item1, pair.Item2, graph, processedRooms, graphRect, out List<Vector2> floorPositions))
                        {
                            //found a valid path, set doorways as open
                            pair.Item1.SetOpen(true);
                            pair.Item2.SetOpen(true);

                            endEntity.FloorTilePositions.AddRange(floorPositions);
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

                            if (ConnectDoorways(pair.Item1, pair.Item2, roomsToCheck, childEntity.ParentComposite.RoomEntities))
                                break;
                            else
                                pairsList.Remove(pair);
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

        bool ConnectDoorways(DungeonDoorway startDoor, DungeonDoorway endDoor, List<DungeonRoomEntity> roomsToCheck, List<DungeonRoomEntity> roomsToMove)
        {
            //var minDistance = 8;
            //var maxDistance = 25;
            //List<Vector2> possibleTiles = new List<Vector2>();

            ////define the range for X and Y based on direction
            //int xStart, xEnd, yStart, yEnd;
            //switch (startDoor.Direction)
            //{
            //    case "Top":
            //        xStart = -maxDistance; xEnd = maxDistance;
            //        yStart = -maxDistance; yEnd = -minDistance;
            //        break;
            //    case "Bottom":
            //        xStart = -maxDistance; xEnd = maxDistance;
            //        yStart = minDistance; yEnd = maxDistance;
            //        break;
            //    case "Left":
            //        xStart = -maxDistance; xEnd = -minDistance;
            //        yStart = -maxDistance; yEnd = maxDistance;
            //        break;
            //    case "Right":
            //        xStart = minDistance; xEnd = maxDistance;
            //        yStart = -maxDistance; yEnd = maxDistance;
            //        break;
            //    default:
            //        throw new InvalidOperationException("Invalid direction");
            //}

            ////iterate over the defined ranges
            //for (int y = yStart; y <= yEnd; y++)
            //{
            //    for (int x = xStart; x <= xEnd; x++)
            //    {
            //        if ((startDoor.Direction == "Top" || startDoor.Direction == "Bottom") && Math.Abs(x) >= minDistance ||
            //            (startDoor.Direction == "Left" || startDoor.Direction == "Right") && Math.Abs(y) >= minDistance)
            //        {
            //            possibleTiles.Add(new Vector2(x, y));
            //        }
            //    }
            //}

            //var minX = roomsToMove.Select(r => r.CollisionBounds.Left).Min();
            //var minY = roomsToMove.Select(r => r.CollisionBounds.Top).Min();
            //var maxX = roomsToMove.Select(r => r.CollisionBounds.Right).Max();
            //var maxY = roomsToMove.Select(r => r.CollisionBounds.Bottom).Max();
            //var rectLocation = new Vector2(minX, minY);
            //var rectSize = new Vector2(maxX, maxY) - rectLocation;
            //var rect = new RectangleF(rectLocation, rectSize);

            //List<Vector2> tilesToRemove = new List<Vector2>();
            //foreach (var possibleTile in possibleTiles)
            //{
            //    //remove any tiles that already overlap rooms to check
            //    if (roomsToCheck.Any(r => r.CollisionBounds.Contains(possibleTile)))
            //    {
            //        tilesToRemove.Add(possibleTile);
            //        continue;
            //    }

            //    var tileWorldDistance = possibleTile * 16;
            //    var idealDoorwayOriginPos = startDoor.PathfindingOrigin + tileWorldDistance;
            //    var movementAmount = idealDoorwayOriginPos - endDoor.PathfindingOrigin;

            //    var projectedRect = new RectangleF(rect.Location, new Vector2(rect.Width, rect.Height));
            //    projectedRect.Location += movementAmount;

            //    //if (roomsToCheck.Any(r => r.CollisionBounds.Intersects(projectedRect)))
            //    //{
            //    //    tilesToRemove.Add(possibleTile);
            //    //    continue;
            //    //}

            //    bool floorOverlap = false;
            //    foreach (var floorPos in roomsToCheck.SelectMany(r => r.FloorTilePositions))
            //    {
            //        if (projectedRect.Contains(floorPos))
            //        {
            //            tilesToRemove.Add(possibleTile);
            //            floorOverlap = true;
            //            break;
            //        }
            //    }

            //    if (floorOverlap)
            //        continue;

            //    List<Vector2> projectedFloorPositions = new List<Vector2>();
            //    projectedFloorPositions.AddRange(roomsToMove.SelectMany(r => r.FloorTilePositions.Select(t => t + movementAmount)));

            //    if (roomsToCheck.Any(r =>
            //    {
            //        if (r.CollisionBounds.Intersects(projectedRect))
            //            return true;
            //        if (r.OverlapsRoom(projectedFloorPositions, out var overlaps))
            //            return true;
            //        return false;
            //    }))
            //    {
            //        tilesToRemove.Add(possibleTile);
            //        continue;
            //    }
            //}

            //possibleTiles = possibleTiles.Except(tilesToRemove).ToList();



            ////try all possible tiles
            //while (possibleTiles.Count > 0)
            //{
            //    //get how far the child doorway should be from outgoing doorway
            //    var tileDistance = possibleTiles.RandomItem();
            //    var tileWorldDistance = tileDistance * 16;

            //    //determine how far to move the child rooms
            //    var idealDoorwayOriginPos = startDoor.PathfindingOrigin + tileWorldDistance;
            //    var movementAmount = idealDoorwayOriginPos - endDoor.PathfindingOrigin;

            //    //move child rooms
            //    foreach (var room in roomsToMove)
            //        room.MoveRoom(movementAmount, false);

            //    var joinedRooms = roomsToCheck.Concat(roomsToMove).ToList();

            //    //get pathfinding between two rooms and their composites
            //    var graphRooms = startDoor.DungeonRoomEntity.ParentComposite.RoomEntities.Concat(endDoor.DungeonRoomEntity.ParentComposite.RoomEntities).ToList();
            //    GetPathfindingGraph(graphRooms, out var graph, out var graphRect);

            //    //try to find a path between the doorways
            //    if (CorridorGenerator.ConnectDoorways(startDoor, endDoor, graph, joinedRooms.ToList(), graphRect, out List<Vector2> floorPositions))
            //    {
            //        //found a valid path, set doorways as open
            //        startDoor.SetOpen(true);
            //        endDoor.SetOpen(true);

            //        startDoor.DungeonRoomEntity.FloorTilePositions.AddRange(floorPositions);

            //        break;
            //    }
            //    else
            //    {
            //        return false;
            //        possibleTiles.Remove(tileDistance);
            //        continue;
            //    }
            //}

            //if (possibleTiles.Count == 0)
            //    return false;

            //return true;

            if (startDoor.IsDirectMatch(endDoor))
            {
                var desiredDoorwayPos = startDoor.PathfindingOrigin;
                if (startDoor.Direction == "Top")
                    desiredDoorwayPos.Y -= 16;
                else if (startDoor.Direction == "Bottom")
                    desiredDoorwayPos.Y += 16;
                else if (startDoor.Direction == "Left")
                    desiredDoorwayPos.X -= 16;
                else if (startDoor.Direction == "Right")
                    desiredDoorwayPos.X += 16;

                var movementAmount = desiredDoorwayPos - endDoor.PathfindingOrigin;
                if (ValidateRoomMovement(movementAmount, roomsToMove, roomsToCheck))
                {
                    foreach (var room in roomsToMove)
                        room.MoveRoom(movementAmount);

                    startDoor.SetOpen(true);
                    endDoor.SetOpen(true);

                    return true;
                }
            }

            //List<Vector2> possiblePositions = new List<Vector2>();
            List<PositionInfo> possiblePosObjects = new List<PositionInfo>();

            var minRadius = 5;
            var radius = 20;
            for (int y = -radius; y < radius + 1; y++)
            {
                if (Math.Abs(y) < minRadius)
                    continue;
                for (int x = -radius; x < radius + 1; x++)
                {
                    if (Math.Abs(x) < minRadius)
                        continue;

                    var gridMovement = new Vector2(x, y);
                    var potentialPos = startDoor.PathfindingOrigin + (gridMovement * 16);
                    var actualMovement = potentialPos - endDoor.PathfindingOrigin;

                    if (ValidateRoomMovement(actualMovement, roomsToMove, roomsToCheck))
                    {
                        foreach (var room in roomsToMove)
                            room.MoveRoom(actualMovement, false);

                        var hasLineOfSight = Physics.Linecast(startDoor.PathfindingOrigin, potentialPos, 1 << PhysicsLayers.Environment).Collider != null;
                        //possiblePositions.Add(potentialPos);
                        possiblePosObjects.Add(new PositionInfo(potentialPos, hasLineOfSight));
                    }
                }
            }

            while (possiblePosObjects.Count > 0)
            {
                Vector2 targetPosition = possiblePosObjects.RandomItem().Position;
                if (possiblePosObjects.Any(p => p.HasLineOfSight))
                    targetPosition = possiblePosObjects.Where(p => p.HasLineOfSight).ToList().RandomItem().Position;

                var movementAmount = targetPosition - endDoor.PathfindingOrigin;

                foreach (var room in roomsToMove)
                    room.MoveRoom(movementAmount, false);

                //get pathfinding between two rooms and their composites
                var graphRooms = startDoor.DungeonRoomEntity.ParentComposite.RoomEntities.Concat(endDoor.DungeonRoomEntity.ParentComposite.RoomEntities).ToList();
                GetPathfindingGraph(graphRooms, out var graph, out var graphRect);

                //try to find a path between the doorways
                if (CorridorGenerator.ConnectDoorways(startDoor, endDoor, graph, graphRooms, graphRect, out List<Vector2> floorPositions))
                {
                    //found a valid path, set doorways as open
                    startDoor.SetOpen(true);
                    endDoor.SetOpen(true);

                    startDoor.DungeonRoomEntity.FloorTilePositions.AddRange(floorPositions);

                    break;
                }
                else
                {
                    //return false;
                    possiblePosObjects.RemoveAll(p => p.Position == targetPosition);
                    continue;
                }
            }

            if (possiblePosObjects.Count == 0)
                return false;
            return true;
        }

        bool ValidateRoomMovement(Vector2 movementAmount, List<DungeonRoomEntity> roomsToMove, List<DungeonRoomEntity> roomsToCheck)
        {
            var floorPositionsToCheck = roomsToCheck.SelectMany(r => r.FloorTilePositions).ToList();

            return !roomsToMove.Any(roomToMove =>
            {
                var rect = roomToMove.CollisionBounds;
                rect.Location += movementAmount;

                //make sure rooms wouldn't overlap
                if (roomsToCheck.Any(roomToCheck => roomToCheck.OverlapsRoom(rect)))
                    return true;

                //make sure doorways wouldn't overlap
                var doorways = roomToMove.FindComponentsOnMap<DungeonDoorway>();
                if (doorways.Any(doorway =>
                {
                    var doorwayRect = new RectangleF(doorway.Entity.Position, new Vector2(doorway.TmxObject.Width, doorway.TmxObject.Height));
                    doorwayRect.Location += movementAmount;
                    if (roomsToCheck.Any(roomToCheck => roomToCheck.OverlapsRoom(doorwayRect)))
                        return true;
                    if (floorPositionsToCheck.Any(f => doorwayRect.Contains(f)))
                        return true;
                    return false;
                }))
                    return true;

                if (roomToMove.FloorTilePositions.Any())
                {
                    var floorPositions = roomToMove.FloorTilePositions.Select(f => f + movementAmount).ToList();
                    if (roomsToCheck.Any(roomToCheck => roomToCheck.OverlapsRoom(floorPositions, out var overlaps)))
                        return true;
                }

                if (floorPositionsToCheck.Any(f => rect.Contains(f)))
                    return true;

                return false;
            });
        }

        void GetPathfindingGraph(List<DungeonRoomEntity> joinedRooms, out WeightedGridGraph graph, out RectangleF graphRect, bool createProtos = false)
        {
            //get rectangle around entire set of rooms, and add some tiles of padding around that rectangle
            var xPadding = 2;
            var yPadding = 4;
            graphRect = GetCombinedBounds(joinedRooms);
            graphRect.X -= xPadding * 16;
            graphRect.Y -= yPadding * 16;
            graphRect.Width += (xPadding * 16 * 2);
            graphRect.Height += (yPadding * 16 * 2);

            //create graph
            graph = new WeightedGridGraph((int)graphRect.Width / 16, (int)graphRect.Height / 16);
            foreach (var graphRoom in joinedRooms)
            {
                //if no collision tiles in this room, continue
                var collisionRenderer = graphRoom.GetComponents<TiledMapRenderer>().FirstOrDefault(r => r.CollisionLayer != null);
                if (collisionRenderer == null)
                    continue;

                //get collision tile positions in world space
                var collisionTiles = collisionRenderer.CollisionLayer.Tiles
                    .Where(t => t != null)
                    .Select(t => graphRoom.Position + new Vector2(t.X * t.Tileset.TileWidth, t.Y * t.Tileset.TileHeight))
                    .ToList();

                //list of wall positions in world space
                var wallWorldPositions = new List<Vector2>();
                foreach (var collisionTile in collisionTiles)
                {
                    wallWorldPositions.Add(collisionTile);

                    //based on tile orientation, add additional walls outwards
                    //var orientation = CorridorPainter.GetTileOrientation(collisionTile, collisionTiles);
                    //if (new CorridorPainter.TileOrientation[] { CorridorPainter.TileOrientation.LeftEdge, CorridorPainter.TileOrientation.TopLeft, CorridorPainter.TileOrientation.BottomLeft }.Contains(orientation))
                    //    for (int i = 1; i < tilePadding + 1; i++)
                    //        wallWorldPositions.Add(collisionTile + (DirectionHelper.Left * 16 * i));
                    //if (new CorridorPainter.TileOrientation[] { CorridorPainter.TileOrientation.RightEdge, CorridorPainter.TileOrientation.TopRight, CorridorPainter.TileOrientation.BottomRight }.Contains(orientation))
                    //    for (int i = 1; i < tilePadding + 1; i++)
                    //        wallWorldPositions.Add(collisionTile + (DirectionHelper.Right * 16 * i));
                    //if (new CorridorPainter.TileOrientation[] {CorridorPainter.TileOrientation.TopEdge, CorridorPainter.TileOrientation.TopRight, CorridorPainter.TileOrientation.TopLeft}.Contains(orientation))
                    //    for (int i = 1; i < tilePadding + 1; i++)
                    //        wallWorldPositions.Add(collisionTile + (DirectionHelper.Up * 16 * i));
                    //if (new CorridorPainter.TileOrientation[] { CorridorPainter.TileOrientation.BottomEdge, CorridorPainter.TileOrientation.BottomLeft, CorridorPainter.TileOrientation.BottomRight }.Contains(orientation))
                    //    for (int i = 1; i < tilePadding + 1; i++)
                    //        wallWorldPositions.Add(collisionTile + (DirectionHelper.Down * 16 * i));

                    var mask = CorridorPainter.GetTileBitmask(collisionTile, collisionTiles);
                    if ((mask & CorridorPainter.TileDirection.Left) == 0)
                        for (int i = 1; i < xPadding + 1; i++)
                            wallWorldPositions.Add(collisionTile + (DirectionHelper.Left * 16 * i));
                    if ((mask & CorridorPainter.TileDirection.Right) == 0)
                        for (int i = 1; i < xPadding + 1; i++)
                            wallWorldPositions.Add(collisionTile + (DirectionHelper.Right * 16 * i));
                    if ((mask & CorridorPainter.TileDirection.Top) == 0)
                        for (int i = 1; i < yPadding + 1; i++)
                            wallWorldPositions.Add(collisionTile + (DirectionHelper.Up * 16 * i));
                    if ((mask & CorridorPainter.TileDirection.Bottom) == 0)
                        for (int i = 1; i < yPadding + 1; i++)
                            wallWorldPositions.Add(collisionTile + (DirectionHelper.Down * 16 * i));
                    if ((mask & (TileDirection.Bottom | TileDirection.Right)) == 0)
                        for (int i = 1; i < yPadding + 1; i++)
                            wallWorldPositions.Add(collisionTile + (DirectionHelper.DownRight * 16 * i));
                    if ((mask & (TileDirection.Bottom | TileDirection.Left)) == 0)
                        for (int i = 1; i < yPadding + 1; i++)
                            wallWorldPositions.Add(collisionTile + (DirectionHelper.DownLeft * 16 * i));
                    if ((mask & (TileDirection.Top | TileDirection.Right)) == 0)
                        for (int i = 1; i < yPadding + 1; i++)
                            wallWorldPositions.Add(collisionTile + (DirectionHelper.UpRight * 16 * i));
                    if ((mask & (TileDirection.Top | TileDirection.Left)) == 0)
                        for (int i = 1; i < yPadding + 1; i++)
                            wallWorldPositions.Add(collisionTile + (DirectionHelper.UpLeft * 16 * i));

                    //switch (orientation)
                    //{
                    //    case CorridorPainter.TileOrientation.LeftEdge:
                    //        for (int i = 1; i < tilePadding + 1; i++)
                    //            wallWorldPositions.Add(collisionTile + (DirectionHelper.Left * 16 * i));
                    //        break;
                    //    case CorridorPainter.TileOrientation.RightEdge:
                    //        for (int i = 1; i < tilePadding + 1; i++)
                    //            wallWorldPositions.Add(collisionTile + (DirectionHelper.Right * 16 * i));
                    //        break;
                    //    case CorridorPainter.TileOrientation.TopEdge:
                    //        for (int i = 1; i < tilePadding + 1; i++)
                    //            wallWorldPositions.Add(collisionTile + (DirectionHelper.Up * 16 * i));
                    //        break;
                    //    case CorridorPainter.TileOrientation.BottomEdge:
                    //        for (int i = 1; i < tilePadding + 1; i++)
                    //            wallWorldPositions.Add(collisionTile + (DirectionHelper.Down * 16 * i));
                    //        break;
                    //}
                }

                //adjust wall world positions into valid positions on the graph and add the walls
                foreach (var wallWorldPosition in wallWorldPositions)
                {
                    if (createProtos)
                        Scene.CreateEntity("", wallWorldPosition).AddComponent(new PrototypeSpriteRenderer(2, 2));
                    var adjustedWallWorldPos = (wallWorldPosition / 16).ToPoint() - (graphRect.Location / 16).ToPoint();
                    if (!graph.Walls.Contains(adjustedWallWorldPos))
                        graph.Walls.Add(adjustedWallWorldPos);
                }

                ////add a wall for each tile in each doorway
                //var doorways = graphRoom.FindComponentsOnMap<DungeonDoorway>();
                //if (doorways != null)
                //{
                //    foreach (var doorway in doorways)
                //    {
                //        for (var y = 0; y < doorway.TmxObject.Height / 16; y++)
                //        {
                //            for (var x = 0; x < doorway.TmxObject.Width / 16; x++)
                //            {
                //                var tilePos = new Vector2(x, y);
                //                var adjustedTilePos = tilePos + (doorway.Entity.Position / 16);
                //                graph.Walls.Add(adjustedTilePos.ToPoint() - (graphRect.Location / 16).ToPoint());

                //                //var tilePoint = adjustedTilePos.ToPoint();
                //                //if (!graph.Walls.Contains(tilePoint))
                //                //    graph.Walls.Add(tilePoint);
                //            }
                //        }
                //    }
                //}
            }
        }

        RectangleF GetCombinedBounds(List<DungeonRoomEntity> rooms)
        {
            var minX = rooms.Select(r => r.Bounds.Left).Min();
            var minY = rooms.Select(r => r.Bounds.Top).Min();
            var maxX = rooms.Select(r => r.Bounds.Right).Max();
            var maxY = rooms.Select(r => r.Bounds.Bottom).Max();

            var pos = new Vector2(minX, minY);
            var size = new Vector2(maxX - minX, maxY - minY);

            return new RectangleF(pos, size);
        }

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
