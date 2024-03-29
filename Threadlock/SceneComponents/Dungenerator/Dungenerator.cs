using Microsoft.Xna.Framework;
using Nez;
using Nez.AI.Pathfinding;
using Nez.BitmapFonts;
using Nez.Persistence;
using Nez.Sprites;
using Nez.Tiled;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Threadlock.Components;
using Threadlock.Components.TiledComponents;
using Threadlock.Entities;
using Threadlock.Helpers;
using Threadlock.Models;
using Threadlock.StaticData;
using static Threadlock.SceneComponents.Dungenerator.CorridorPainter;
using Random = Nez.Random;
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
            if (File.Exists("Content/Data/DungeonFlows5.json"))
            {
                var json = File.ReadAllText("Content/Data/DungeonFlows5.json");
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

                //GetPathfindingGraph(_allMapEntities, out var graph, out var graphRect, true);

                //paint corridor tiles
                var allFloorPositions = _allMapEntities
                    .SelectMany(c => c.FloorTilePositions)
                    .Distinct()
                    .ToList();

                //combine nearby corridor tiles
                CorridorPainter.CombineTiles(allFloorPositions, 2, 4);

                List<Vector2> reservedPositions = new List<Vector2>();
                foreach (var map in _allMapEntities)
                {
                    //get world position of every Back (floor) tile on this map
                    var positions = TiledHelper.GetTilePositionsByLayer(map, "Back");
                    var back2Positions = TiledHelper.GetTilePositionsByLayer(map, "Back2");
                    reservedPositions.AddRange(positions);
                    reservedPositions.AddRange(back2Positions);
                }

                foreach (var doorway in Scene.FindComponentsOfType<DungeonDoorway>())
                {
                    if (doorway.HasConnection)
                        reservedPositions.AddRange(TiledHelper.GetTilePositionsByLayer(doorway.Entity, "Back"));
                    allFloorPositions.AddRange(TiledHelper.GetTilePositionsByLayer(doorway.Entity, "Fill"));
                }

                allFloorPositions = allFloorPositions.Distinct().ToList();

                //open tileset
                using (var stream = TitleContainer.OpenStream(Content.Tiled.Tilesets.Forge_tileset))
                {
                    var xDocTileset = XDocument.Load(stream);

                    string tsxDir = Path.GetDirectoryName(Content.Tiled.Tilesets.Forge_tileset);
                    var tileset = new TmxTileset().LoadTmxTileset(null, xDocTileset.Element("tileset"), 0, tsxDir);
                    tileset.TmxDirectory = tsxDir;

                    CorridorPainter.PaintCorridorTiles(allFloorPositions, reservedPositions, tileset);
                }

                //AddDecorations();

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
                                    var diffDirPairs = pairsList.Where(p => p.Item1.Direction != p.Item2.Direction).ToList();
                                    if (perfectPairs.Any())
                                        pair = perfectPairs.RandomItem();
                                    else if (diffDirPairs.Any())
                                        pair = diffDirPairs.RandomItem();
                                    else
                                        pair = pairsList.RandomItem();

                                    if (ConnectDoorways(pair.Item1, pair.Item2, processedRooms, new List<DungeonRoomEntity> { roomEntity }))
                                        break;
                                    else
                                    {
                                        pairsList.Remove(pair);
                                    }
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
            foreach (var loop in loops)
            {
                var processedRooms = new List<DungeonRoomEntity>();

                //get placement order
                var placementOrder = new List<DungeonRoomEntity>();
                for (int i = 0; i < (loop.RoomEntities.Count + 1) / 2; i++)
                {
                    placementOrder.Add(loop.RoomEntities[i]);
                    if (i != loop.RoomEntities.Count - i - 1)
                        placementOrder.Add(loop.RoomEntities[loop.RoomEntities.Count - i - 1]);
                }

                var attempts = 0;
                while (attempts < _maxAttempts)
                {
                    attempts++;

                    bool mapSelectionFailed = false;
                    for (int i = 0; i < placementOrder.Count; i++)
                    {
                        var room = placementOrder[i];
                        var possibleMaps = GetValidMaps(room);

                        while (possibleMaps.Count > 0)
                        {
                            //pick a random map
                            var map = possibleMaps.RandomItem();

                            //create map
                            room.CreateMap(map);

                            //no need to validate when placing the first map
                            if (i == 0)
                                break;

                            //get previous room
                            var previousRoom = i == 1 ? placementOrder[0] : placementOrder[i - 2];
                            DungeonRoomEntity oppositeRoom = i > 1 ? placementOrder[i - 1] : null;

                            //get doorways
                            var prevNodeDoorways = previousRoom.FindComponentsOnMap<DungeonDoorway>();
                            var newNodeDoorways = room.FindComponentsOnMap<DungeonDoorway>();

                            //get possible doorway pairs
                            var pairsList = GetValidDoorwayPairs(prevNodeDoorways, newNodeDoorways, false);

                            //sort pairs by direct matches first, then closest to room on the end of the other line
                            pairsList = pairsList
                                .OrderByDescending(p => p.Item1.IsDirectMatch(p.Item2))
                                .ThenBy(p => oppositeRoom != null ? Vector2.Distance(p.Item1.PathfindingOrigin, oppositeRoom.Position) : float.MaxValue)
                                .ToList();

                            //try to find a valid pair
                            while (pairsList.Count > 0)
                            {
                                //pick a pair
                                var pair = pairsList.First();

                                if (ConnectDoorways(pair.Item1, pair.Item2, processedRooms, new List<DungeonRoomEntity> { room }))
                                    break;
                                else
                                    pairsList.Remove(pair);
                            }

                            //no pairs were valid, try another map
                            if (pairsList.Count == 0)
                            {
                                //destroy map entity
                                room.ClearMap();

                                //remove the invalid map from possible maps list
                                possibleMaps.Remove(map);

                                //try again
                                continue;
                            }

                            break;
                        }

                        //no maps were valid
                        if (possibleMaps.Count == 0)
                        {
                            mapSelectionFailed = true;
                            break;
                        }

                        processedRooms.Add(room);
                    }

                    if (mapSelectionFailed)
                    {
                        loop.Reset();
                        processedRooms.Clear();
                        continue;
                    }

                    //CLOSE THE LOOP

                    //handle connecting end of loop to beginning
                    var startEntity = processedRooms[^2];
                    var endEntity = processedRooms[^1];

                    //get pairs and order by distance between each other
                    var pairs = GetValidDoorwayPairs(endEntity.FindComponentsOnMap<DungeonDoorway>(), startEntity.FindComponentsOnMap<DungeonDoorway>(), false);
                    pairs = pairs
                        .OrderByDescending(p => p.Item1.Direction != p.Item2.Direction)
                        .ThenBy(p => Vector2.Distance(p.Item1.PathfindingOrigin, p.Item2.PathfindingOrigin))
                        .ToList();

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

                    //if no pairs were valid, try making loop from scratch
                    if (pairs.Count == 0)
                    {
                        //clear any processed rooms
                        loop.Reset();
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
                                : pairsList.Any(p => p.Item1.Direction != p.Item2.Direction)
                                ? pairsList.Where(p => p.Item1.Direction != p.Item2.Direction).ToList().RandomItem()
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

        #region DECORATIONS

        void AddDecorations()
        {
            //handle decorations
            var dungeonFloor = new List<Vector2>();
            var backMapRenderers = Scene.FindComponentsOfType<TiledMapRenderer>().Where(r => r.RenderLayer == RenderLayers.Back);
            if (backMapRenderers.Any())
                dungeonFloor.AddRange(backMapRenderers.SelectMany(r => TiledHelper.GetTilePositionsByLayer(r.Entity, "Back")).ToList());
            var backCorridorRenderers = Scene.FindComponentsOfType<CorridorRenderer>().Where(r => r.RenderLayer == RenderLayers.Back);
            if (backCorridorRenderers.Any())
                dungeonFloor.AddRange(backCorridorRenderers.SelectMany(r => r.TileDictionary.Where(t => !t.Value.IsCollider).Select(t => t.Key)));
            List<FloorTile> floorTiles = new List<FloorTile>();
            foreach (var floor in dungeonFloor)
            {
                var mask = CorridorPainter.GetTileBitmask(floor, dungeonFloor);
                var orientation = CorridorPainter.GetTileOrientation(floor, dungeonFloor);
                floorTiles.Add(new FloorTile(floor, mask, orientation));
            }

            var topEdges = floorTiles
                .Where(t => t.TileOrientation == TileOrientation.TopEdge)
                .OrderBy(t => t.Position.Y)
                .ThenBy(t => t.Position.X)
                .ToList();
            var topSegments = GetTileSegments(topEdges);

            var bottomEdges = floorTiles
                .Where(t => t.TileOrientation == TileOrientation.BottomEdge)
                .OrderBy(t => t.Position.Y)
                .ThenBy(t => t.Position.X)
                .ToList();
            var bottomSegments = GetTileSegments(bottomEdges);

            var leftEdges = floorTiles
                .Where(t => t.TileOrientation == TileOrientation.LeftEdge)
                .OrderBy(t => t.Position.X)
                .ThenBy(t => t.Position.Y)
                .ToList();
            var leftSegments = GetTileSegments(leftEdges);

            var rightEdges = floorTiles
                .Where(t => t.TileOrientation == TileOrientation.RightEdge)
                .OrderBy(t => t.Position.X)
                .ThenBy(t => t.Position.Y)
                .ToList();
            var rightSegments = GetTileSegments(rightEdges);

            var atlas = Scene.Content.LoadSpriteAtlas($"Content/Textures/Decorations/Forge/Atlas/forge_decorations.atlas");
            foreach (var segment in topSegments)
                CreateDecorations(segment, atlas);
            foreach (var segment in bottomSegments) CreateDecorations(segment, atlas);
            foreach (var segment in leftSegments) CreateDecorations(segment, atlas);
            foreach (var segment in rightSegments) CreateDecorations(segment, atlas);
        }

        void CreateDecorations(List<FloorTile> segment, SpriteAtlas atlas)
        {
            if (Random.Chance(.75f))
                return;

            for (int i = 0; i < segment.Count; i++)
            {
                if (Random.Chance(.75f))
                    continue;
                var tile = segment[i];
                var sprites = atlas.Sprites.Where(s => s.SourceRect.Width == 16).ToList();
                var decorationEntity = new DungeonDecoration(tile.Position, new Vector2(16, 16), sprites.RandomItem());
                Scene.AddEntity(decorationEntity);
            }

            //var start = Random.Range(0, segment.Count);
            //var end = Random.Range(start + 1, segment.Count);

            //for (int i = start; i < end; i++)
            //{
            //    var tile = segment[i];
            //    var sprites = atlas.Sprites.Where(s => s.SourceRect.Width == 16).ToList();
            //    var decorationEntity = new DungeonDecoration(tile.Position, new Vector2(16, 16), sprites.RandomItem());
            //    Scene.AddEntity(decorationEntity);
            //}
        }

        #endregion

        #region HELPERS

        bool ConnectDoorways(DungeonDoorway startDoor, DungeonDoorway endDoor, List<DungeonRoomEntity> roomsToCheck, List<DungeonRoomEntity> roomsToMove)
        {
            return CorridorGenerator.ConnectDoorways(startDoor, endDoor, roomsToCheck, roomsToMove, out var floorPositions);
            //if (startDoor.IsDirectMatch(endDoor))
            //{
            //    var desiredDoorwayPos = startDoor.PathfindingOrigin;
            //    if (startDoor.Direction == "Top")
            //        desiredDoorwayPos.Y -= 16;
            //    else if (startDoor.Direction == "Bottom")
            //        desiredDoorwayPos.Y += 16;
            //    else if (startDoor.Direction == "Left")
            //        desiredDoorwayPos.X -= 16;
            //    else if (startDoor.Direction == "Right")
            //        desiredDoorwayPos.X += 16;

            //    var movementAmount = desiredDoorwayPos - endDoor.PathfindingOrigin;
            //    if (ValidateRoomMovement(movementAmount, roomsToMove, roomsToCheck))
            //    {
            //        foreach (var room in roomsToMove)
            //            room.MoveRoom(movementAmount);

            //        startDoor.SetOpen(true);
            //        endDoor.SetOpen(true);

            //        return true;
            //    }
            //}

            //if (startDoor.Direction == endDoor.Direction)
            //    return false;

            //var graphRooms = roomsToCheck.Concat(roomsToMove).ToList();
            //if (graphRooms.Contains(startDoor.DungeonRoomEntity))
            //    graphRooms.Remove(startDoor.DungeonRoomEntity);
            //if (graphRooms.Contains(endDoor.DungeonRoomEntity))
            //    graphRooms.Remove(endDoor.DungeonRoomEntity);
            //var roomPadding = 4;
            //List<RectangleF> paddedRects = new List<RectangleF>();
            //foreach (var room in graphRooms)
            //{
            //    var rect = room.CollisionBounds;
            //    rect.Location -= new Vector2(roomPadding * 16, roomPadding * 16);
            //    rect.Size += new Vector2(roomPadding * 16 * 2, roomPadding * 16 * 2);
            //    paddedRects.Add(rect);
            //}

            //var minDistance = 4;
            //var maxDistance = 20;
            //Vector2 startDir = startDoor.GetOutgingDirection();
            //Vector2 endDir = endDoor.GetIncomingDirection();

            //List<Vector2> possiblePositions = new List<Vector2>();

            //if (startDir != endDir)
            //{
            //    bool wallHit = false;
            //    for (int i = minDistance; i < maxDistance + 1; i++)
            //    {
            //        Vector2 targetPos = startDoor.PathfindingOrigin + (startDir * 16 * i);
            //        for (int j = minDistance; j < maxDistance + 1; j++)
            //        {
            //            targetPos += (endDir * 16 * j);
            //            var movement = targetPos - endDoor.PathfindingOrigin;

            //            if (paddedRects.Any(r => r.Contains(targetPos)))
            //            {
            //                wallHit = true;
            //                break;
            //            }

            //            if (ValidateRoomMovement(movement, roomsToMove, roomsToCheck))
            //                possiblePositions.Add(targetPos);
            //        }

            //        if (wallHit)
            //        {
            //            possiblePositions.Clear();
            //            break;
            //        }
            //    }
            //}
            //else
            //{
            //    for (int i = minDistance * 2; i < maxDistance + 1; i++)
            //    {
            //        Vector2 targetPos = startDoor.PathfindingOrigin + (startDir * 16 * i);
            //        var movement = targetPos - endDoor.PathfindingOrigin;

            //        if (paddedRects.Any(r => r.Contains(targetPos)))
            //        {
            //            possiblePositions.Clear();
            //            break;
            //        }

            //        if (ValidateRoomMovement(movement, roomsToMove, roomsToCheck))
            //            possiblePositions.Add(targetPos);
            //    }
            //}

            //possiblePositions = possiblePositions
            //    .OrderBy(m => Vector2.Distance(startDoor.PathfindingOrigin, m))
            //    .ToList();

            //while (possiblePositions.Count > 0)
            //{
            //    var pos = possiblePositions.First();
            //    var movement = pos - endDoor.PathfindingOrigin;
            //    foreach (var room in roomsToMove)
            //        room.MoveRoom(movement);

            //    if (CorridorGenerator.ConnectDoorways(startDoor, endDoor, roomsToCheck, out var floorPositions))
            //    {
            //        //found a valid path, set doorways as open
            //        startDoor.SetOpen(true);
            //        endDoor.SetOpen(true);

            //        startDoor.DungeonRoomEntity.FloorTilePositions.AddRange(floorPositions);

            //        break;
            //    }
            //    else
            //        possiblePositions.Remove(pos);
            //}

            //if (possiblePositions.Count == 0)
            //    return false;

            //return true;
        }

        public static bool ValidateRoomMovement(Vector2 movementAmount, List<DungeonRoomEntity> roomsToMove, List<DungeonRoomEntity> roomsToCheck)
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

                    var doorways = graphRoom.FindComponentsOnMap<DungeonDoorway>();
                    foreach (var doorway in doorways)
                    {
                        var originPos = doorway.PathfindingOrigin;
                        var direction = Vector2.Zero;
                        switch (doorway.Direction)
                        {
                            case "Top":
                                direction = DirectionHelper.Up; break;
                            case "Bottom":
                                direction = DirectionHelper.Down; break;
                            case "Left":
                                direction = DirectionHelper.Left; break;
                            case "Right":
                                direction = DirectionHelper.Right; break;
                        }

                        for (int i = 0; i < 5; i++)
                        {
                            var wallPos = originPos + (direction * 16 * i);
                            if (wallWorldPositions.Contains(wallPos))
                                wallWorldPositions.Remove(wallPos);
                        }
                    }
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

        List<List<FloorTile>> GetTileSegments(List<FloorTile> tiles)
        {
            List<List<FloorTile>> segments = new List<List<FloorTile>>();
            var currentSegment = new List<FloorTile> { tiles.First() };
            for (int i = 1; i < tiles.Count; i++)
            {
                var currentTile = tiles[i - 1];
                var nextTile = tiles[i];

                if (currentSegment.Count < 8 && IsTileAdjacent(currentTile, nextTile))
                    currentSegment.Add(nextTile);
                else
                {
                    segments.Add(currentSegment);
                    currentSegment = new List<FloorTile> { nextTile };
                }
            }

            segments.Add(currentSegment);
            segments.RemoveAll(s => s.Count < 3);

            return segments;
        }

        bool IsTileAdjacent(FloorTile currentTile, FloorTile nextTile)
        {
            if (currentTile.TileOrientation != nextTile.TileOrientation)
                return false;

            if (currentTile.TileOrientation == TileOrientation.TopEdge || currentTile.TileOrientation == TileOrientation.BottomEdge)
            {
                if (currentTile.Position.Y != nextTile.Position.Y)
                    return false;
                return currentTile.Position.X + 16 == nextTile.Position.X;
            }
            else if (currentTile.TileOrientation == TileOrientation.LeftEdge || currentTile.TileOrientation == TileOrientation.RightEdge)
            {
                if (currentTile.Position.X != nextTile.Position.X)
                    return false;
                return currentTile.Position.Y + 16 == nextTile.Position.Y;
            }

            return false;
        }

        #endregion
    }
}
