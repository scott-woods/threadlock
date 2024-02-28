using Microsoft.Xna.Framework;
using Nez;
using Nez.Persistence;
using Nez.Tiled;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components.TiledComponents;
using Threadlock.Entities;
using Threadlock.Helpers;
using Threadlock.Models;
using Threadlock.SaveData;
using Threadlock.StaticData;

namespace Threadlock.SceneComponents
{
    public class Dungenerator : SceneComponent
    {
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
            var loops = dungeonGraph.Loops;
            var trees = dungeonGraph.Trees;
            var allComposites = loops.Concat(trees);

            //all map entities in the dungeon
            List<DungeonRoomEntity> allMapEntities = new List<DungeonRoomEntity>();

            //handle tree composites
            foreach (var tree in trees)
            {
                //list of all maps in this tree
                var mapEntities = new List<DungeonRoomEntity>();
                
                DungeonRoomEntity prevMapEntity = null;
                foreach (var node in tree)
                {
                    //get potential maps
                    var possibleMaps = GetPossibleMapsByNode(node);

                    //create map entity
                    var mapEntity = Scene.AddEntity(new DungeonRoomEntity(node.Id));

                    //try maps until a valid one is found
                    while (possibleMaps.Count > 0)
                    {
                        //pick a random map
                        var map = possibleMaps.RandomItem();
                        
                        //create map
                        mapEntity.CreateMap(map);

                        //only validate for previous node if it isn't null (ie this isn't the first in composite)
                        if (prevMapEntity != null)
                        {
                            //get doorways
                            var prevNodeDoorways = prevMapEntity.FindComponentsOnMap<DungeonDoorway>();
                            var newNodeDoorways = mapEntity.FindComponentsOnMap<DungeonDoorway>();

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
                                mapEntity.SetPosition(entityPos);

                                //get rectangle of this map
                                var newMapRect = new RectangleF(mapEntity.Position.X, mapEntity.Position.Y, map.Width * map.TileWidth, map.Height * map.TileHeight);

                                //loop through previously placed maps
                                bool isInvalidPair = false;
                                foreach (var mapEnt in mapEntities.Where(e => e != mapEntity))
                                {
                                    //get rectangle of the previously placed map
                                    var prevMapRenderer = mapEnt.GetComponent<TiledMapRenderer>();
                                    var prevMap = prevMapRenderer.TiledMap;
                                    var prevMapRect = new RectangleF(mapEnt.Position.X, mapEnt.Position.Y, prevMap.Width * prevMap.TileWidth, prevMap.Height * prevMap.TileHeight);

                                    //if there is no overlap, continue
                                    if (!newMapRect.Intersects(prevMapRect))
                                        continue;

                                    //if there is some overlap, check each tile on the new map
                                    foreach (var layer in map.TileLayers)
                                    {
                                        //check each non-null tile
                                        foreach (var tile in layer.Tiles.Where(t => t != null))
                                        {
                                            //get bounds of this tile
                                            var tileBounds = new RectangleF(tile.X * map.TileWidth, tile.Y * map.TileHeight, map.TileWidth, map.TileHeight);
                                            tileBounds.X += mapEntity.Position.X;
                                            tileBounds.Y += mapEntity.Position.Y;

                                            //check if bounds of this tile overlaps any layers in previously placed maps
                                            if (prevMapRenderer.TiledMap.TileLayers.Any(l => l.GetTilesIntersectingBounds(tileBounds).Count > 0))
                                            {
                                                //new map would overlap a previously placed map, this pair is invalid
                                                isInvalidPair = true;
                                                break;
                                            }
                                        }

                                        if (isInvalidPair)
                                            break;
                                    }

                                    if (isInvalidPair)
                                        break;
                                }

                                //invalid pair. remove from list and try again
                                if (isInvalidPair)
                                {
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
                                mapEntity.Destroy();

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
                    mapEntities.Add(mapEntity);

                    //update prev map entity for next loop iteration
                    prevMapEntity = mapEntity;
                }

                //finished with tree. add all map entities from this tree to the total list
                allMapEntities.AddRange(mapEntities);
            }

            //handle loop composites
            foreach (var loop in loops)
            {
                //list of all maps in this loop
                var mapEntities = new List<DungeonRoomEntity>();

                DungeonRoomEntity prevMapEntity = null;
                for (int i = 0; i < loop.Count; i++)
                {
                    var node = loop[i];

                    //get potential maps
                    var possibleMaps = GetPossibleMapsByNode(node);

                    //create map entity
                    var mapEntity = Scene.AddEntity(new DungeonRoomEntity(node.Id));

                    //try maps until a valid one is found
                    while (possibleMaps.Count > 0)
                    {
                        //pick a random map
                        var map = possibleMaps.RandomItem();

                        //create map
                        mapEntity.CreateMap(map);

                        //only validate for previous node if it isn't null (ie this isn't the first in composite)
                        if (prevMapEntity != null)
                        {
                            //get doorways
                            var prevNodeDoorways = prevMapEntity.FindComponentsOnMap<DungeonDoorway>();
                            var newNodeDoorways = mapEntity.FindComponentsOnMap<DungeonDoorway>();

                            //get possible doorway pairs
                            var pairsList = GetValidDoorwayPairs(prevNodeDoorways, newNodeDoorways);

                            //sort by distance to starting room
                            if (i > 0)
                            {
                                pairsList = pairsList.OrderBy(p => Vector2.Distance(p.Item1.Entity.Position, mapEntities.First().Position)).ToList();
                            }

                            bool roomPlaced = false;
                            while (pairsList.Count > 0)
                            {
                                Tuple<DungeonDoorway, DungeonDoorway> pair = null;
                                if (i < loop.Count / 2)
                                    pair = pairsList.Last();
                                else if (i >= loop.Count / 2)
                                    pair = pairsList.First();
                                else
                                    pair = pairsList.RandomItem();

                                //get the ideal position for the new room entity based on the pair
                                var entityPos = GetRoomPositionByDoorwayPair(pair.Item1, pair.Item2);

                                //set the map's position
                                mapEntity.SetPosition(entityPos);

                                //get rectangle of this map
                                var newMapRect = new RectangleF(mapEntity.Position.X, mapEntity.Position.Y, map.Width * map.TileWidth, map.Height * map.TileHeight);

                                //loop through previously placed maps
                                bool isInvalidPair = false;
                                foreach (var mapEnt in mapEntities.Where(e => e != mapEntity))
                                {
                                    //get rectangle of the previously placed map
                                    var prevMapRenderer = mapEnt.GetComponent<TiledMapRenderer>();
                                    var prevMap = prevMapRenderer.TiledMap;
                                    var prevMapRect = new RectangleF(mapEnt.Position.X, mapEnt.Position.Y, prevMap.Width * prevMap.TileWidth, prevMap.Height * prevMap.TileHeight);

                                    //if there is no overlap, continue
                                    if (!newMapRect.Intersects(prevMapRect))
                                        continue;

                                    //if there is some overlap, check each tile on the new map
                                    foreach (var layer in map.TileLayers)
                                    {
                                        //check each non-null tile
                                        foreach (var tile in layer.Tiles.Where(t => t != null))
                                        {
                                            //get bounds of this tile
                                            var tileBounds = new RectangleF(tile.X * map.TileWidth, tile.Y * map.TileHeight, map.TileWidth, map.TileHeight);
                                            tileBounds.X += mapEntity.Position.X;
                                            tileBounds.Y += mapEntity.Position.Y;

                                            //check if bounds of this tile overlaps any layers in previously placed maps
                                            if (prevMapRenderer.TiledMap.TileLayers.Any(l => l.GetTilesIntersectingBounds(tileBounds).Count > 0))
                                            {
                                                //new map would overlap a previously placed map, this pair is invalid
                                                isInvalidPair = true;
                                                break;
                                            }
                                        }

                                        if (isInvalidPair)
                                            break;
                                    }

                                    if (isInvalidPair)
                                        break;
                                }

                                //invalid pair. remove from list and try again
                                if (isInvalidPair)
                                {
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
                                mapEntity.Destroy();

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
                    mapEntities.Add(mapEntity);

                    //update prev map entity for next loop iteration
                    prevMapEntity = mapEntity;
                }

                //handle connecting end of loop to beginning
                var startEntity = mapEntities.First();
                var endEntity = mapEntities.Last();

                //get pairs and order by distance between each other
                var pairs = GetValidDoorwayPairs(endEntity.FindComponentsOnMap<DungeonDoorway>(), startEntity.FindComponentsOnMap<DungeonDoorway>(), false);
                pairs = pairs.OrderBy(p => Vector2.Distance(p.Item1.Entity.Position, p.Item2.Entity.Position)).ToList();

                //open pair
                var finalPair = pairs.First();
                finalPair.Item1.SetOpen(true);
                finalPair.Item2.SetOpen(true);

                //finished with loop. add all map entities from this loop to the total list
                allMapEntities.AddRange(mapEntities);
            }

            //connect composites
            //get nodes that have children that aren't in their own composite
            var compositeParentNodes = flow.Nodes
                .Where(n =>
                {
                    var composite = allComposites.First(t => t.Contains(n));
                    return n.Children.Any(c => !composite.Any(comp => comp.Id == c.ChildNodeId));
                })
                .OrderByDescending(n => n.Children.Count());

            foreach (var parentNode in compositeParentNodes)
            {
                //get the map entity this node is associated with
                var parentMapEntity = allMapEntities.First(m => m.RoomId == parentNode.Id);

                //get composite this node is part of
                var composite = allComposites.First(t => t.Contains(parentNode));

                //get children that aren't in this composite
                var childMapEntities = allMapEntities
                    .Where(m =>
                    {
                        var childNodeIds = parentNode.Children
                            .Where(c => !composite.Any(comp => comp.Id == c.ChildNodeId))
                            .Select(conn => conn.ChildNodeId);
                        return childNodeIds.Contains(m.RoomId);
                    });

                //loop through children that aren't in this composite
                foreach (var childEntity in childMapEntities)
                {
                    //get doorways
                    var parentNodeDoorways = parentMapEntity.FindComponentsOnMap<DungeonDoorway>();
                    var childNodeDoorways = childEntity.FindComponentsOnMap<DungeonDoorway>();

                    //get possible doorway pairs
                    var pairsList = GetValidDoorwayPairs(parentNodeDoorways, childNodeDoorways);

                    //pick random pair
                    var pair = pairsList.RandomItem();

                    //get ideal position for connecting room
                    var pos = GetRoomPositionByDoorwayPair(pair.Item1, pair.Item2);

                    //get distance we need to move each room in this composite
                    var diff = pos - childEntity.Position;

                    //get the composite this child node is part of
                    var childNodeComposite = allComposites.First(t => t.Contains(flow.Nodes.First(n => n.Id == childEntity.RoomId)));

                    //move each map in the child node composite
                    foreach (var childCompNode in childNodeComposite)
                    {
                        var childCompNodeEnt = allMapEntities.First(m => m.RoomId == childCompNode.Id);
                        childCompNodeEnt.Position += diff;
                    }

                    //successfully moved, set doorways as open
                    pair.Item1.SetOpen(true);
                    pair.Item2.SetOpen(true);
                }
            }
        }

        List<TmxMap> GetPossibleMapsByNode(DungeonNode node)
        {
            return new List<TmxMap>
            {
                Scene.Content.LoadTiledMap(Nez.Content.Tiled.Tilemaps.Forge.Forge_simple_3)
            };
        }

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
    }
}
