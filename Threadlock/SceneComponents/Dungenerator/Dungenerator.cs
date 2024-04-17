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
        const int _maxLoopAttempts = 5;

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

                foreach (var mapEntity in _allMapEntities)
                {
                    if (mapEntity.TryGetComponent<TiledMapRenderer>(out var renderer))
                        TiledHelper.SetupLightingTiles(mapEntity, renderer.TiledMap);
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
                            var chosenMaps = _allMapEntities.Select(m => m.Map).Concat(processedRooms.Select(m => m.Map)).ToList();
                            var pickCounts = possibleMaps.Select(m => new
                            {
                                Map = m,
                                Count = chosenMaps.Count(c => c == m)
                            });
                            var minPickCount = pickCounts.Min(m => m.Count);
                            var leastPickedMaps = pickCounts
                                .Where(m => m.Count == minPickCount)
                                .Select(m => m.Map)
                                .ToList();
                            var map = leastPickedMaps.RandomItem();

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
                var attempts = 0;
                while (attempts < _maxLoopAttempts)
                {
                    attempts++;

                    var processedRooms = new List<DungeonRoomEntity>();

                    //get placement order
                    var placementOrder = new List<DungeonRoomEntity>();
                    for (int i = 0; i < (loop.RoomEntities.Count + 1) / 2; i++)
                    {
                        placementOrder.Add(loop.RoomEntities[i]);
                        if (i != loop.RoomEntities.Count - i - 1)
                            placementOrder.Add(loop.RoomEntities[loop.RoomEntities.Count - i - 1]);
                    }

                    string oddLineDir = "";
                    string evenLineDir = "";

                    bool mapSelectionFailed = false;
                    for (int i = 0; i < placementOrder.Count; i++)
                    {
                        var room = placementOrder[i];
                        var possibleMaps = GetValidMaps(room);

                        var oppositeDir = Mathf.IsOdd(i) ? evenLineDir : oddLineDir;
                        var currentDir = Mathf.IsOdd(i) ? oddLineDir : evenLineDir;

                        var oppOppositeDir = "";
                        switch (oppositeDir)
                        {
                            case "Top":
                                oppOppositeDir = "Bottom"; break;
                            case "Bottom":
                                oppOppositeDir = "Top"; break;
                            case "Left":
                                oppOppositeDir = "Right"; break;
                            case "Right":
                                oppOppositeDir = "Left"; break;
                        }

                        while (possibleMaps.Count > 0)
                        {
                            TmxMap map;

                            if (i < 2)
                            {
                                //pick a random map
                                var chosenMaps = _allMapEntities.Select(m => m.Map).Concat(processedRooms.Select(m => m.Map)).ToList();
                                var pickCounts = possibleMaps.Select(m => new
                                {
                                    Map = m,
                                    Count = chosenMaps.Count(c => c == m)
                                });
                                var minPickCount = pickCounts.Min(m => m.Count);
                                var leastPickedMaps = pickCounts
                                    .Where(m => m.Count == minPickCount)
                                    .Select(m => m.Map)
                                    .ToList();
                                map = leastPickedMaps.RandomItem();

                                room.CreateMap(map);

                                if (i == 0)
                                    break;
                            }
                            else
                            {
                                List<string> desiredDirs = new List<string>();
                                if (!string.IsNullOrWhiteSpace(currentDir))
                                    desiredDirs.Add(currentDir);
                                if (!string.IsNullOrWhiteSpace(oppOppositeDir))
                                    desiredDirs.Add(oppOppositeDir);

                                //get maps that have doorways in the desired directions
                                var mapsWithDir = possibleMaps.Where(m =>
                                {
                                    var objs = m.ObjectGroups.SelectMany(g => g.Objects).Where(o => o.Type == "DungeonDoorway" && o.Properties != null && o.Properties.ContainsKey("Direction"));
                                    if (desiredDirs.Any(d => !objs.Any(o => o.Properties["Direction"] == d)))
                                        return false;
                                    return true;
                                }).ToList();

                                if (mapsWithDir.Any())
                                    map = mapsWithDir.RandomItem();
                                else
                                    map = possibleMaps.RandomItem();

                                room.CreateMap(map);
                            }

                            var previousRoom = i == 1 ? placementOrder[0] : placementOrder[i - 2];

                            //get doorways
                            var prevDoorways = previousRoom.FindComponentsOnMap<DungeonDoorway>();
                            var nextDoorways = room.FindComponentsOnMap<DungeonDoorway>();

                            var possiblePairs = GetValidDoorwayPairs(prevDoorways, nextDoorways, false);

                            //filter possible pairs differently based on where we are in loop
                            if (i == 1)
                            {
                                possiblePairs = possiblePairs
                                    .OrderByDescending(p => p.Item1.IsDirectMatch(p.Item2))
                                    .ToList();
                            }
                            //filter for before halfway through loop
                            else if (i == 2 || i <= (placementOrder.Count + 1) / 2)
                            {

                                possiblePairs = possiblePairs
                                    .OrderByDescending(p => p.Item2.Direction == currentDir)
                                    .ThenByDescending(p => p.Item1.IsDirectMatch(p.Item2))
                                    .ThenByDescending(p => p.Item2.Direction != oppOppositeDir)
                                    .ThenByDescending(p => p.Item2.Direction == oppositeDir)
                                    .ToList();
                            }
                            //filter for after halfway through loop
                            else
                            {
                                possiblePairs = possiblePairs
                                    .OrderByDescending(p => p.Item2.Direction == oppositeDir)
                                    .ThenByDescending(p => p.Item1.Direction == oppOppositeDir)
                                    .ThenByDescending(p => p.Item2.Direction != oppOppositeDir)
                                    .ThenByDescending(p => p.Item1.IsDirectMatch(p.Item2))
                                    .ToList();
                            }

                            //try to find a valid pair
                            while (possiblePairs.Count > 0)
                            {
                                //pick a pair
                                var pair = possiblePairs.First();

                                if (ConnectDoorways(pair.Item1, pair.Item2, processedRooms, new List<DungeonRoomEntity> { room }))
                                {
                                    if (Mathf.IsOdd(i))
                                        oddLineDir = pair.Item2.Direction;
                                    else if (Mathf.IsEven(i))
                                        evenLineDir = pair.Item2.Direction;

                                    break;
                                }
                                else
                                    possiblePairs.Remove(pair);
                            }

                            //no pairs were valid, try another map
                            if (possiblePairs.Count == 0)
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
                        //.Where(p => Vector2.Distance(p.Item1.PathfindingOrigin, p.Item2.PathfindingOrigin) <= 1200)
                        .OrderBy(p => Vector2.Distance(p.Item1.PathfindingOrigin, p.Item2.PathfindingOrigin))
                        .ThenByDescending(p => p.Item1.Direction != p.Item2.Direction)
                        .ToList();

                    //try pairs
                    while (pairs.Count > 0)
                    {
                        //pick first pair
                        var pair = pairs.First();

                        if (CorridorGenerator.ConnectStaticDoorways(pair.Item1, pair.Item2, processedRooms))
                            break;
                        else
                            pairs.Remove(pair);
                    }

                    //if no pairs were valid, try making loop from scratch
                    if (pairs.Count == 0)
                    {
                        //clear any processed rooms
                        loop.Reset();
                        processedRooms.Clear();
                        continue;
                    }

                    //finished with loop. add all map entities from this loop to the total list
                    _allMapEntities.AddRange(processedRooms);

                    break;
                }
                
                //loop failed after all attempts, start over
                if (attempts >= _maxLoopAttempts)
                {
                    return false;
                }
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

                        var pairsListCopy = new List<Tuple<DungeonDoorway, DungeonDoorway>>(pairsList);

                        while (pairsListCopy.Count > 0)
                        {
                            //pick random pair, preference for perfect pairs
                            var pair = pairsListCopy.Any(p => p.Item1.IsDirectMatch(p.Item2))
                                ? pairsListCopy.Where(p => p.Item1.IsDirectMatch(p.Item2)).ToList().RandomItem()
                                : pairsListCopy.Any(p => p.Item1.Direction != p.Item2.Direction)
                                ? pairsListCopy.Where(p => p.Item1.Direction != p.Item2.Direction).ToList().RandomItem()
                                : pairsListCopy.RandomItem();

                            if (ConnectDoorways(pair.Item1, pair.Item2, roomsToCheck, childEntity.ParentComposite.RoomEntities))
                                break;
                            else
                                pairsListCopy.Remove(pair);
                        }

                        if (pairsListCopy.Count == 0)
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
            var topSegments = FloorTile.GetTileSegments(topEdges);

            var bottomEdges = floorTiles
                .Where(t => t.TileOrientation == TileOrientation.BottomEdge)
                .OrderBy(t => t.Position.Y)
                .ThenBy(t => t.Position.X)
                .ToList();
            var bottomSegments = FloorTile.GetTileSegments(bottomEdges);

            var leftEdges = floorTiles
                .Where(t => t.TileOrientation == TileOrientation.LeftEdge)
                .OrderBy(t => t.Position.X)
                .ThenBy(t => t.Position.Y)
                .ToList();
            var leftSegments = FloorTile.GetTileSegments(leftEdges);

            var rightEdges = floorTiles
                .Where(t => t.TileOrientation == TileOrientation.RightEdge)
                .OrderBy(t => t.Position.X)
                .ThenBy(t => t.Position.Y)
                .ToList();
            var rightSegments = FloorTile.GetTileSegments(rightEdges);

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
        }

        #endregion

        #region HELPERS

        bool ConnectDoorways(DungeonDoorway startDoor, DungeonDoorway endDoor, List<DungeonRoomEntity> roomsToCheck, List<DungeonRoomEntity> roomsToMove)
        {
            var success = CorridorGenerator.ConnectDoorways(startDoor, endDoor, roomsToCheck, roomsToMove, out var floorPositions);
            return success;
        }

        /// <summary>
        /// check that if a set of rooms is moved by a certain amount, if any of those rooms will overlap with another set of rooms
        /// </summary>
        /// <param name="movementAmount"></param>
        /// <param name="roomsToMove"></param>
        /// <param name="roomsToCheck"></param>
        /// <returns></returns>
        public static bool ValidateRoomMovement(Vector2 movementAmount, List<DungeonRoomEntity> roomsToMove, List<DungeonRoomEntity> roomsToCheck)
        {
            return !roomsToMove.Any(roomToMove => roomsToCheck.Any(roomToCheck => roomToMove.OverlapsRoom(roomToCheck, movementAmount)));
        }

        public List<TmxMap> GetValidMaps(DungeonRoomEntity room)
        {
            List<TmxMap> possibleMaps = new List<TmxMap>();

            var requiredCount = _allComposites.SelectMany(c => c.RoomEntities).Where(m => m.AllChildren.Contains(room)).Count() + room.AllChildren.Count;

            //get potential maps
            var validMaps = _allMaps.Where(m =>
            {
                if (m.Properties == null)
                    return false;
                if (!m.Properties.ContainsKey("RoomType"))
                    return false;
                if (m.ObjectGroups.SelectMany(g => g.Objects).Where(o => o.Type == "DungeonDoorway").Count() < requiredCount)
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

        #endregion
    }
}
