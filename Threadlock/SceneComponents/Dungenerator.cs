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
using System.Xml.Linq;
using Threadlock.Components;
using Threadlock.Components.TiledComponents;
using Threadlock.Entities;
using Threadlock.Helpers;
using Threadlock.Models;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using RectangleF = Nez.RectangleF;

namespace Threadlock.SceneComponents
{
    public class Dungenerator : SceneComponent
    {
        List<DungeonRoomEntity> _allMapEntities = new List<DungeonRoomEntity>();
        List<DungeonComposite> _allComposites = new List<DungeonComposite>();

        public void Generate()
        {
            //read flow file
            DungeonFlow flow = new DungeonFlow();
            if (File.Exists("Content/Data/DungeonFlows4.json"))
            {
                var json = File.ReadAllText("Content/Data/DungeonFlows4.json");
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

            //handle tree composites
            HandleTrees(_allComposites.Where(c => c.CompositeType == DungeonCompositeType.Tree).ToList());

            //handle loop composites
            HandleLoops(_allComposites.Where(c => c.CompositeType == DungeonCompositeType.Loop).ToList());

            //foreach (var comp in _allComposites)
            //{
            //    var compositeMapEntities = _allMapEntities.Where(e => comp.Any(c => c.Id == e.RoomId)).ToList();
            //}

            //var graph = CreatePathfindingGraph();

            //connect composites
            ConnectComposites();
        }

        #region HANDLE LOOPS/TREES

        void HandleTrees(List<DungeonComposite> trees)
        {
            foreach (var tree in trees)
            {
                //rooms that have been processed
                var processedRooms = new List<DungeonRoomEntity>();

                DungeonRoomEntity prevMapEntity = null;
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

                            bool roomPlaced = false;
                            while (pairsList.Count > 0)
                            {
                                //pick a random doorway pair
                                var pair = pairsList.RandomItem();

                                //get the ideal position for the new room entity based on the pair
                                var entityPos = GetRoomPositionByDoorwayPair(pair.Item1, pair.Item2);

                                //set the map's position
                                roomEntity.SetPosition(entityPos);

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

                                roomPlaced = true;
                                break;
                            }

                            //room not placed for any pair. map is invalid.
                            if (!roomPlaced)
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

                    //add entity to list
                    processedRooms.Add(roomEntity);

                    //update prev map entity for next loop iteration
                    prevMapEntity = roomEntity;
                }

                //finished with tree. add all map entities from this tree to the total list
                _allMapEntities.AddRange(processedRooms);
            }
        }

        void HandleLoops(List<DungeonComposite> loops)
        {
            foreach (var loop in loops)
            {
                //processed rooms
                var processedRooms = new List<DungeonRoomEntity>();

                //create rooms/maps
                DungeonRoomEntity prevMapEntity = null;
                for (int i = 0; i < loop.RoomEntities.Count; i++)
                {
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

                        //only validate for previous node if it isn't null (ie this isn't the first in composite)
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

                            bool roomPlaced = false;
                            while (pairsList.Count > 0)
                            {
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
                                var entityPos = GetRoomPositionByDoorwayPair(pair.Item1, pair.Item2);

                                //set the map's position
                                roomEntity.SetPosition(entityPos);

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

                                roomPlaced = true;
                                break;
                            }

                            //room not placed for any pair. map is invalid.
                            if (!roomPlaced)
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

                    //add entity to list
                    processedRooms.Add(roomEntity);

                    //update prev map entity for next loop iteration
                    prevMapEntity = roomEntity;
                }

                //create pathfinding graph
                //var graph = new AstarGridGraph((int)compositeBounds.Width, (int)compositeBounds.Height);
                var graph = loop.GetPathfindingGraph();

                //handle connecting end of loop to beginning
                var startEntity = processedRooms.First();
                var endEntity = processedRooms.Last();

                //get pairs and order by distance between each other
                var pairs = GetValidDoorwayPairs(endEntity.FindComponentsOnMap<DungeonDoorway>(), startEntity.FindComponentsOnMap<DungeonDoorway>(), false);
                pairs = pairs.OrderBy(p => Vector2.Distance(p.Item1.Entity.Position, p.Item2.Entity.Position)).ToList();

                //open pair
                var finalPair = pairs.First();
                finalPair.Item1.SetOpen(true);
                finalPair.Item2.SetOpen(true);

                var endDoorwayPos = new Vector2(finalPair.Item1.Entity.Position.X / 16, finalPair.Item1.Entity.Position.Y / 16).ToPoint();
                var startDoorwayPos = new Vector2(finalPair.Item2.Entity.Position.X / 16, finalPair.Item2.Entity.Position.Y / 16).ToPoint();
                endDoorwayPos += finalPair.Item1.PathfindingOffset.ToPoint();
                startDoorwayPos += finalPair.Item2.PathfindingOffset.ToPoint();
                switch (finalPair.Item1.Direction)
                {
                    case "Top":
                        endDoorwayPos.Y -= 2;
                        break;
                    case "Bottom":
                        endDoorwayPos.Y += 2;
                        break;
                    case "Left":
                        endDoorwayPos.X -= 2;
                        break;
                    case "Right":
                        endDoorwayPos.X += 2;
                        break;
                }
                switch (finalPair.Item2.Direction)
                {
                    case "Top":
                        startDoorwayPos.Y -= 2;
                        break;
                    case "Bottom":
                        startDoorwayPos.Y += 2;
                        break;
                    case "Left":
                        startDoorwayPos.X -= 2;
                        break;
                    case "Right":
                        startDoorwayPos.X += 2;
                        break;
                }

                var offsetPos = loop.Bounds.Location / 16;
                var endDoorwayGraphPos = endDoorwayPos - offsetPos.ToPoint();
                var startDoorwayGraphPos = startDoorwayPos - offsetPos.ToPoint();

                graph.Walls.Remove(endDoorwayGraphPos);
                graph.Walls.Remove(startDoorwayGraphPos);

                var isPathValid = false;
                while (!isPathValid)
                {
                    var path = graph.Search(endDoorwayGraphPos, startDoorwayGraphPos);

                    if (path == null)
                        break;

                    var adjustedPath = path.Select(p =>
                    {
                        return (new Vector2(p.X, p.Y) * 16) + loop.Bounds.Location;
                    }).ToList();

                    var largerPath = IncreaseCorridorWidth(adjustedPath);

                    //check all renderers. if any tile in the path would overlap, this path is invalid. add a wall there.
                    var renderers = processedRooms.SelectMany(e => e.GetComponents<TiledMapRenderer>());
                    bool arePointsRemoved = false;
                    foreach (var renderer in renderers.Where(r => r.CollisionLayer != null))
                    {
                        foreach (var pathPair in largerPath)
                        {
                            if (pathPair.Value.Any(p => renderer.GetTileAtWorldPosition(p) != null || DirectionHelper.CardinalDirections.Any(d => renderer.GetTileAtWorldPosition(p + (d * 16)) != null)))
                            {
                                var positionToAddToWalls = pathPair.Key - loop.Bounds.Location;
                                positionToAddToWalls /= 16;
                                if (!graph.Walls.Contains(positionToAddToWalls.ToPoint()))
                                    graph.Walls.Add(positionToAddToWalls.ToPoint());
                                arePointsRemoved = true;
                                break;
                            }
                        }
                    }

                    if (arePointsRemoved)
                        continue;
                    else
                        isPathValid = true;

                    using (var stream = TitleContainer.OpenStream(Nez.Content.Tiled.Tilesets.Forge_tileset))
                    {
                        var xDocTileset = XDocument.Load(stream);

                        string tsxDir = Path.GetDirectoryName(Nez.Content.Tiled.Tilesets.Forge_tileset);
                        var tileset = new TmxTileset().LoadTmxTileset(null, xDocTileset.Element("tileset"), 0, tsxDir);
                        tileset.TmxDirectory = tsxDir;

                        var tile = tileset.TileRegions[202];
                        foreach (var centerPos in largerPath)
                        {
                            var allPositions = new List<Vector2>();
                            allPositions.Add(centerPos.Key);
                            allPositions.AddRange(centerPos.Value);
                            allPositions = allPositions.Distinct().ToList();

                            foreach (var pos in centerPos.Value)
                            {
                                var ent = Scene.CreateEntity("tile", pos);
                                ent.SetParent(startEntity);
                                ent.AddComponent(new SingleTileRenderer(tileset.Image.Texture, tile));
                            }
                        }

                        //PaintFloorTiles(adjustedPath, tileset);
                        //GenerateWalls(adjustedPath, tileset);
                    }
                }

                //finished with loop. add all map entities from this loop to the total list
                _allMapEntities.AddRange(processedRooms);
            }
        }

        #endregion

        #region COMPOSITES

        void ConnectComposites()
        {
            foreach (var room in _allMapEntities
                .Where(r => r.ChildrenOutsideComposite != null && r.ChildrenOutsideComposite.Count > 0)
                .OrderByDescending(r => r.ChildrenOutsideComposite.Count))
            {
                //loop through children that aren't in this composite
                foreach (var childEntity in room.ChildrenOutsideComposite)
                {
                    //get doorways
                    var parentNodeDoorways = room.FindComponentsOnMap<DungeonDoorway>();
                    var childNodeDoorways = childEntity.FindComponentsOnMap<DungeonDoorway>();

                    //get possible doorway pairs
                    var pairsList = GetValidDoorwayPairs(parentNodeDoorways, childNodeDoorways);

                    //pick random pair
                    var pair = pairsList.RandomItem();

                    //get ideal position for connecting room
                    var pos = GetRoomPositionByDoorwayPair(pair.Item1, pair.Item2);

                    //get distance we need to move each room in this composite
                    var diff = pos - childEntity.Position;

                    childEntity.ParentComposite.MoveRooms(diff);

                    //successfully moved, set doorways as open
                    pair.Item1.SetOpen(true);
                    pair.Item2.SetOpen(true);
                }
            }
        }

        #endregion

        #region HELPERS

        Vector2 GetRoomPositionByDoorwayPair(DungeonDoorway previousDoorway, DungeonDoorway nextDoorway)
        {
            //world position of the doorway in previous room
            var prevDoorwayWorldPos = previousDoorway.MapEntity.Position + new Vector2(previousDoorway.TmxObject.X, previousDoorway.TmxObject.Y);
            var idealNextDoorwayWorldPos = prevDoorwayWorldPos;

            //determine world pos that new doorway should be based on direction
            var vDiff = nextDoorway.TmxObject.Height;
            var hDiff = nextDoorway.TmxObject.Width;
            switch (previousDoorway.Direction)
            {
                case "Top":
                    idealNextDoorwayWorldPos.Y -= vDiff;
                    break;
                case "Bottom":
                    idealNextDoorwayWorldPos.Y += vDiff;
                    break;
                case "Left":
                    idealNextDoorwayWorldPos.X -= hDiff;
                    break;
                case "Right":
                    idealNextDoorwayWorldPos.X += hDiff;
                    break;
            }

            //determine ideal entity position for the new room based on the lined up doorway
            return idealNextDoorwayWorldPos - new Vector2(nextDoorway.TmxObject.X, nextDoorway.TmxObject.Y);
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

        void PaintFloorTiles(List<Vector2> positions, TmxTileset tileset)
        {
            var tile = tileset.TileRegions[202];
            //var tile = map.TileLayers.First().Tiles.First();
            foreach (var pos in positions)
            {
                var ent = Scene.CreateEntity("tile", pos);
                ent.AddComponent(new SingleTileRenderer(tileset.Image.Texture, tile));
            }
        }

        void PaintFloorTiles(List<Vector2> positions, Texture2D tilesetTexture)
        {
            foreach (var pos in positions)
            {
                var ent = Scene.CreateEntity("tile", pos);
                ent.AddComponent(new SingleTileRenderer(tilesetTexture, new Rectangle(32, 128, 16, 16)));
            }
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
                        //if (!visitedPositions.Contains(pos))
                        //{
                        //    visitedPositions.Add(pos);
                        //    posDictionary[positions[i - 1]].Add(pos);
                        //}
                    }
                }
            }

            return posDictionary;
        }

        #endregion
    }
}
