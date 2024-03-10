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
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Threadlock.Components;
using Threadlock.Components.TiledComponents;
using Threadlock.Entities;
using Threadlock.Helpers;
using Threadlock.Models;
using Threadlock.StaticData;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using RectangleF = Nez.RectangleF;

namespace Threadlock.SceneComponents
{
    public class Dungenerator : SceneComponent
    {
        const int _maxAttempts = 100;
        List<DungeonRoomEntity> _allMapEntities = new List<DungeonRoomEntity>();
        List<DungeonComposite> _allComposites = new List<DungeonComposite>();

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
                        foreach (var tileRenderer in composite.SingleTileRenderers)
                        {
                            tileRenderer.Entity.Destroy();
                        }
                        composite.SingleTileRenderers.Clear();
                    }
                    foreach (var map in _allMapEntities)
                        map.ClearMap();
                    _allMapEntities.Clear();
                    continue;
                }

                //handle loop composites
                var loopSuccess = HandleLoops(_allComposites.Where(c => c.CompositeType == DungeonCompositeType.Loop).ToList());
                if (!loopSuccess)
                {
                    foreach (var composite in _allComposites)
                    {
                        foreach (var tileRenderer in composite.SingleTileRenderers)
                        {
                            tileRenderer.Entity.Destroy();
                        }
                        composite.SingleTileRenderers.Clear();
                    }
                    foreach (var map in _allMapEntities)
                        map.ClearMap();
                    _allMapEntities.Clear();
                    continue;
                }

                //connect composites
                var connectionSuccessful = ConnectComposites();
                if (!connectionSuccessful)
                {
                    foreach (var composite in _allComposites)
                    {
                        foreach (var tileRenderer in composite.SingleTileRenderers)
                        {
                            tileRenderer.Entity.Destroy();
                        }
                        composite.SingleTileRenderers.Clear();
                    }
                    foreach (var map in _allMapEntities)
                        map.ClearMap();
                    _allMapEntities.Clear();
                    continue;
                }

                break;
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
                        //get potential maps
                        var possibleMaps = roomEntity.GetPossibleMaps();

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
                        var possibleMaps = roomEntity.GetPossibleMaps();

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
                                var pairsList = GetValidDoorwayPairs(prevNodeDoorways, newNodeDoorways);

                                //sort by distance to starting room
                                if (i > 0)
                                {
                                    pairsList = pairsList.OrderBy(p => Vector2.Distance(p.Item1.Entity.Position, processedRooms.First().Position)).ToList();
                                }

                                //try to find a valid pair
                                while (pairsList.Count > 0)
                                {
                                    //pick a pair based on where we are in the loop
                                    Tuple<DungeonDoorway, DungeonDoorway> pair = null;
                                    if (i == 0)
                                        pair = pairsList.RandomItem();
                                    else if (i < loop.RoomEntities.Count / 2)
                                        pair = pairsList.Last();
                                    else if (i >= loop.RoomEntities.Count / 2)
                                        pair = pairsList.First();
                                    else
                                        pair = pairsList.RandomItem();

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
                    loop.AdjustForPathfinding(25);

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

                        var graph = loop.GetPathfindingGraph();

                        //try to find a path between the doorways
                        if (ConnectDoorways(pair.Item1, pair.Item2, graph, processedRooms, out var path))
                        {
                            //found a valid path, set doorways as open
                            pair.Item1.SetOpen(true);
                            pair.Item2.SetOpen(true);

                            //open tileset
                            using (var stream = TitleContainer.OpenStream(Nez.Content.Tiled.Tilesets.Forge_tileset))
                            {
                                var xDocTileset = XDocument.Load(stream);

                                string tsxDir = Path.GetDirectoryName(Nez.Content.Tiled.Tilesets.Forge_tileset);
                                var tileset = new TmxTileset().LoadTmxTileset(null, xDocTileset.Element("tileset"), 0, tsxDir);
                                tileset.TmxDirectory = tsxDir;

                                var pathPoints = path.Values.SelectMany(v => v).Distinct().ToList();
                                var tileRenderers = PaintFloorTiles(pathPoints, tileset, endEntity);

                                loop.SingleTileRenderers.AddRange(tileRenderers);
                            }
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
                            .Where(p => p.Item1.Direction != p.Item2.Direction)
                            .OrderByDescending(p => p.Item1.IsDirectMatch(p.Item2)).ToList();

                        while (pairsList.Count > 0)
                        {
                            //pick random pair, preference for perfect pairs
                            var pair = pairsList.Any(p => p.Item1.IsDirectMatch(p.Item2))
                                ? pairsList.Where(p => p.Item1.IsDirectMatch(p.Item2)).ToList().RandomItem()
                                : pairsList.RandomItem();

                            //determine direction to try placing the composite based on the chosen doorway pair
                            var dir = Vector2.Zero;
                            switch (pair.Item1.Direction)
                            {
                                case "Top":
                                    dir.Y = -1;
                                    break;
                                case "Bottom":
                                    dir.Y = 1;
                                    break;
                                case "Left":
                                    dir.X = -1;
                                    break;
                                case "Right":
                                    dir.X = 1;
                                    break;
                            }
                            switch (pair.Item2.Direction)
                            {
                                case "Top":
                                    dir.Y = 1;
                                    break;
                                case "Bottom":
                                    dir.Y = -1;
                                    break;
                                case "Left":
                                    dir.X = 1;
                                    break;
                                case "Right":
                                    dir.X = -1;
                                    break;
                            }

                            //try to position the composite between 8 and 25 tiles away
                            var distance = 8;
                            bool success = false;
                            while (distance < 250)
                            {
                                //how far away to move from the first doorway
                                var worldDistance = distance * dir * 16;

                                //move child composite
                                var idealDoorwayOriginPos = pair.Item1.PathfindingOrigin + worldDistance;
                                var movementAmount = idealDoorwayOriginPos - pair.Item2.PathfindingOrigin;
                                childEntity.ParentComposite.MoveRooms(movementAmount, false);

                                //check for overlap
                                if (roomsToCheck.Any(p => childEntity.ParentComposite.RoomEntities.Any(r => r.OverlapsRoom(p))))
                                {
                                    //increment distance and try again
                                    distance += 1;
                                    continue;
                                }
                                else
                                {
                                    //get pathfinding graph
                                    var roomsForGraph = roomsToCheck.Concat(childEntity.ParentComposite.RoomEntities).ToList();
                                    var graph = CreateDungeonGraph(roomsForGraph);

                                    //try to find a path between the doorways
                                    if (ConnectDoorways(pair.Item1, pair.Item2, graph, roomsForGraph, out var path))
                                    {
                                        //found a valid path, set doorways as open
                                        pair.Item1.SetOpen(true);
                                        pair.Item2.SetOpen(true);

                                        //open tileset
                                        using (var stream = TitleContainer.OpenStream(Nez.Content.Tiled.Tilesets.Forge_tileset))
                                        {
                                            var xDocTileset = XDocument.Load(stream);

                                            string tsxDir = Path.GetDirectoryName(Nez.Content.Tiled.Tilesets.Forge_tileset);
                                            var tileset = new TmxTileset().LoadTmxTileset(null, xDocTileset.Element("tileset"), 0, tsxDir);
                                            tileset.TmxDirectory = tsxDir;

                                            var pathPoints = path.Values.SelectMany(v => v).Distinct().ToList();
                                            var tileRenderers = PaintFloorTiles(pathPoints, tileset, childEntity);
                                            room.ParentComposite.SingleTileRenderers.AddRange(tileRenderers);

                                            success = true;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        distance += 1;
                                        continue;
                                    }
                                }
                            }

                            //no position was valid, this pair is invalid. remove from list and try again
                            if (!success)
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

        AstarGridGraph CreateDungeonGraph(List<DungeonRoomEntity> roomsToCheck)
        {
            //prepare entire dungeon for pathfinding by moving bounds above 0, 0
            var top = _allComposites.Select(c => c.Bounds.Top).Min();
            var left = _allComposites.Select(c => c.Bounds.Left).Min();
            var topLeft = new Vector2(left, top);
            var desiredPos = Vector2.Zero + (new Vector2(1, 1) * 16 * 25);
            var amountToMove = desiredPos - topLeft;
            foreach (var composite in _allComposites)
            {
                composite.MoveRooms(amountToMove, false);
            }

            var bottom = _allComposites.Select(c => c.Bounds.Bottom).Max();
            var right = _allComposites.Select(c => c.Bounds.Right).Max();

            var graph = new AstarGridGraph((int)right / 16, (int)bottom / 16);

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

        bool ConnectDoorways(DungeonDoorway startDoor, DungeonDoorway endDoor, AstarGridGraph graph, List<DungeonRoomEntity> roomsToCheck, out Dictionary<Vector2, List<Vector2>> largerPath)
        {
            largerPath = new Dictionary<Vector2, List<Vector2>>();

            var startDoorwayGridPos = (startDoor.PathfindingOrigin / 16).ToPoint();
            var endDoorwayGridPos = (endDoor.PathfindingOrigin / 16).ToPoint();

            switch (startDoor.Direction)
            {
                case "Top":
                    startDoorwayGridPos.Y -= 2;
                    break;
                case "Bottom":
                    startDoorwayGridPos.Y += 2;
                    break;
                case "Left":
                    startDoorwayGridPos.X -= 2;
                    break;
                case "Right":
                    startDoorwayGridPos.X += 2;
                    break;
            }
            switch (endDoor.Direction)
            {
                case "Top":
                    endDoorwayGridPos.Y -= 2;
                    break;
                case "Bottom":
                    endDoorwayGridPos.Y += 2;
                    break;
                case "Left":
                    endDoorwayGridPos.X -= 2;
                    break;
                case "Right":
                    endDoorwayGridPos.X += 2;
                    break;
            }

            graph.Walls.Remove(startDoorwayGridPos);
            graph.Walls.Remove(endDoorwayGridPos);

            var isPathValid = false;
            while (!isPathValid)
            {
                isPathValid = true;

                largerPath.Clear();

                //try finding a path
                var path = graph.Search(startDoorwayGridPos, endDoorwayGridPos);

                //if no path found, connection failed
                if (path == null)
                    return false;

                //get path in world space
                var adjustedPath = path.Select(p =>
                {
                    return (new Vector2(p.X, p.Y) * 16);
                }).ToList();

                largerPath = IncreaseCorridorWidth(adjustedPath);

                //check that all tiles in larger path are valid
                foreach (var pathSet in largerPath.Where(p => p.Key != startDoorwayGridPos.ToVector2() * 16 && p.Key != endDoorwayGridPos.ToVector2() * 16))
                {
                    if (pathSet.Value.Any(p =>
                    {
                        if (roomsToCheck.Any(r => r.OverlapsRoom(p, false)))
                            return true;
                        //if (DirectionHelper.CardinalDirections.Any(d =>
                        //{
                        //    var posInDirection = p + (d * 16);
                        //    return roomsToCheck.Any(r => r.OverlapsRoom(posInDirection, false));
                        //}))
                        //    return true;
                        return false;
                    }))
                    {
                        var posToAddToWalls = (pathSet.Key / 16).ToPoint();
                        if (!graph.Walls.Contains(posToAddToWalls))
                            graph.Walls.Add(posToAddToWalls);
                        isPathValid = false;
                        break;
                    }
                }
            }

            return true;
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

        List<SingleTileRenderer> PaintFloorTiles(List<Vector2> positions, TmxTileset tileset, DungeonRoomEntity parentRoom)
        {
            var renderers = new List<SingleTileRenderer>();
            var tile = tileset.TileRegions[202];
            //var tile = map.TileLayers.First().Tiles.First();
            foreach (var pos in positions)
            {
                var ent = Scene.CreateEntity("tile");
                ent.SetPosition(pos);
                var tileRenderer = ent.AddComponent(new SingleTileRenderer(tileset.Image.Texture, tile));
                tileRenderer.RenderLayer = RenderLayers.Back;
                renderers.Add(tileRenderer);
            }

            return renderers;
        }

        void GenerateWalls(List<Vector2> floorPositions, TmxTileset tileset)
        {
            List<Vector2> wallPositions = new List<Vector2>();

            var tile = tileset.TileRegions[152];
            foreach (var pos in floorPositions)
            {
                foreach (var dir in new[] { new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1) })
                {
                    var neighborPos = pos + (dir * tileset.TileWidth);
                    if (!floorPositions.Contains(neighborPos))
                        wallPositions.Add(neighborPos);
                }
            }

            foreach (var wallPos in wallPositions)
            {
                var ent = Scene.CreateEntity("wall", wallPos);
                ent.AddComponent(new SingleTileRenderer(tileset.Image.Texture, tile));
            }
        }

        Dictionary<Vector2, List<Vector2>> IncreaseCorridorWidth(List<Vector2> positions)
        {
            Dictionary<Vector2, List<Vector2>> posDictionary = new Dictionary<Vector2, List<Vector2>>();
            List<Vector2> visitedPositions = new List<Vector2>();
            for (int i = 1; i < positions.Count + 1; i++)
            {
                posDictionary[positions[i - 1]] = new List<Vector2>();
                for (int x = -1; x < 2; x++)
                {
                    for (int y = -1; y < 2; y++)
                    {
                        var pos = new Vector2(x * 16, y * 16) + positions[i - 1];
                        posDictionary[positions[i - 1]].Add(pos);
                    }
                }
            }

            return posDictionary;
        }

        #endregion
    }
}
