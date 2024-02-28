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
            if (File.Exists("Content/Data/DungeonFlows3.json"))
            {
                var json = File.ReadAllText("Content/Data/DungeonFlows3.json");
                flow = Json.FromJson<DungeonFlow>(json);
            }
            else return;

            //get composites
            var dungeonGraph = new DungeonGraph();
            dungeonGraph.ProcessGraph(flow.Nodes);
            var loops = dungeonGraph.Loops;
            var trees = dungeonGraph.Trees;

            //all map entities in the dungeon
            List<DungeonRoomEntity> allMapEntities = new List<DungeonRoomEntity>();

            //loop through tree composites
            foreach (var tree in trees)
            {
                //list of all maps in this tree
                var mapEntities = new List<DungeonRoomEntity>();

                DungeonRoomEntity prevMapEntity = null;
                foreach (var node in tree)
                {
                    //get potential maps
                    var possibleMaps = GetPossibleMapsByNode(node);

                    while (possibleMaps.Count > 0)
                    {
                        //pick a random map
                        var map = possibleMaps.RandomItem();

                        //create map entity
                        var mapEntity = Scene.AddEntity(new DungeonRoomEntity(node.Id));
                        mapEntity.CreateMap(map);
                        mapEntities.Add(mapEntity);

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

                            //map validation successful
                            break;
                        }

                        //update prev map entity for next loop iteration
                        prevMapEntity = mapEntity;
                    }
                }

                //finished with tree. add all map entities from this tree to the total list
                allMapEntities.AddRange(mapEntities);
            }

            //connect composites
            foreach (var tree in trees)
            {
                foreach (var node in tree)
                {
                    var mapEntity = allMapEntities.Find(e => e.Id == node.Id);

                    foreach (var child in node.Children)
                    {
                        if (!tree.Any(n => n.Id == child.ChildNodeId))
                        {
                            //make connection between composites
                            var connectingMapEntity = allMapEntities.Find(e => e.Id == child.ChildNodeId);

                            var connectingMapDoorways = Scene.FindComponentsOfType<DungeonDoorway>()
                                .Where(d => d.MapEntity == connectingMapEntity && !d.HasConnection);
                            var currentMapDoorways = Scene.FindComponentsOfType<DungeonDoorway>()
                                .Where(d => d.MapEntity == mapEntity && !d.HasConnection);

                            var pairs = from d1 in currentMapDoorways.Where(d => !d.HasConnection)
                                        from d2 in connectingMapDoorways.Where(d => !d.HasConnection)
                                        where d1.IsDirectMatch(d2)
                                        select new { PrevDoorway = d1, NextDoorway = d2 };

                            var pairsList = pairs.ToList();

                            var pair = pairsList.RandomItem();

                            var pos = GetRoomPositionByDoorwayPair(pair.PrevDoorway, pair.NextDoorway);

                            var diff = pos - connectingMapEntity.Position;

                            //move all maps in the other composite
                            foreach (var tree2 in trees)
                            {
                                if (tree2.Any(t => t.Id == child.ChildNodeId))
                                {
                                    foreach (var tree2child in tree2)
                                    {
                                        var ent = allMapEntities.First(e => e.RoomId == tree2child.Id);
                                        ent.Position += diff;
                                    }    
                                }
                            }
                        }
                    }
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
