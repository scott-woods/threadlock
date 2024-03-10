using Microsoft.Xna.Framework;
using Nez;
using Nez.AI.Pathfinding;
using Nez.DeferredLighting;
using Nez.Tiled;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components.TiledComponents;
using Threadlock.Helpers;
using Threadlock.Models;
using Threadlock.StaticData;

namespace Threadlock.SceneComponents
{
    public class BSPDungenerator : SceneComponent
    {
        /// <summary>
        /// size of the entire dungeon in tiles
        /// </summary>
        Vector2 _dungeonArea = new Vector2(160, 160);
        Vector2 _tileSize = new Vector2(16, 16);
        const int _maxLeafSize = 45;

        List<DungeonLeaf> _leafs = new List<DungeonLeaf>();

        public void Generate()
        {
            var root = new DungeonLeaf(0, 0, 0, (int)_dungeonArea.X, (int)_dungeonArea.Y);
            _leafs.Add(root);

            //recursively loop through and split leafs until we can't continue
            bool hasSplit = true;
            while (hasSplit)
            {
                hasSplit = false;
                
                //loop through leafs
                foreach (var leaf in _leafs.ToList())
                {
                    //if leaf doesn't have children yet
                    if (leaf.LeftChild == null && leaf.RightChild == null)
                    {
                        //if leaf is too big, or random chance
                        if (leaf.Size.X > _maxLeafSize || leaf.Size.Y > _maxLeafSize || Nez.Random.Chance(.75f))
                        {
                            //try to split the leaf
                            if (leaf.Split())
                            {
                                //if split was successful, add new leafs to list and continue while loop
                                _leafs.Add(leaf.LeftChild);
                                _leafs.Add(leaf.RightChild);
                                hasSplit = true;
                            }
                        }
                    }
                }
            }

            bool spawnRoomPlaced = false;
            var spawnMap = Maps.ForgeSpawn;
            //create rooms in each leaf that is at the bottom of the tree
            foreach (var leaf in _leafs.Where(l => l.LeftChild == null && l.RightChild == null && l.Room == null))
            {
                Map map;

                if (!spawnRoomPlaced && spawnMap.TmxMap.Width <= leaf.Size.X + 10 && spawnMap.TmxMap.Height <= leaf.Size.Y + 10)
                {
                    map = spawnMap;
                    spawnRoomPlaced = true;
                }
                else
                {
                    //get all possible maps that will fit in this partition
                    var possibleMaps = Maps.ForgeMaps.Where((m) =>
                    {
                        //determine if map will fit in leaf
                        var fits = m.TmxMap.Width <= leaf.Size.X + 10 && m.TmxMap.Height <= leaf.Size.Y + 10;

                        //if doesn't fit, unload
                        if (!fits)
                            Game1.Scene.Content.UnloadAsset<TmxMap>(m.Name);

                        return fits;
                    }).ToList();

                    //if no possible maps, don't do anything i guess
                    if (possibleMaps.Count == 0)
                        return;

                    //pick a random map
                    map = possibleMaps.RandomItem();
                }

                var minX = 5;
                var maxX = (int)leaf.Size.X - map.TmxMap.Width - 5;
                int adjustedMinX = (minX + 4) / 5;
                int adjustedMaxX = Math.Max((int)maxX / 5, adjustedMinX);

                var minY = 5;
                var maxY = (int)leaf.Size.Y - map.TmxMap.Height - 5;
                int adjustedMinY = (minY + 4) / 5;
                int adjustedMaxY = Math.Max((int)maxY / 5, adjustedMinY);

                var posX = Nez.Random.Range(adjustedMinX, adjustedMaxX) * 5;
                var posY = Nez.Random.Range(adjustedMinY, adjustedMaxY) * 5;

                var pos = new Vector2(posX + leaf.Position.X, posY + leaf.Position.Y);

                leaf.Room = new DungeonRoom(map, pos, leaf);
            }

            //loop through each leaf that has a room in it and instantiate the room entity
            foreach (var room in _leafs.Where(l => l.Room != null).Select(l => l.Room).ToList())
            {
                //create map entity
                var mapEntity = new Entity($"map-room-${room.Position.X}-${room.Position.Y}");
                mapEntity.AddComponent(room);
                var mapPosition = new Vector2(room.Position.X * _tileSize.X, room.Position.Y * _tileSize.Y);
                mapEntity.SetPosition(mapPosition);
                Scene.AddEntity(mapEntity);

                //load map
                var tmxMap = room.Map.TmxMap;

                //create main map renderer
                var mapRenderer = mapEntity.AddComponent(new TiledMapRenderer(tmxMap, "Walls"));
                mapRenderer.SetLayersToRender(new[] { "Back", "Walls" });
                mapRenderer.RenderLayer = RenderLayers.Back;
                Flags.SetFlagExclusive(ref mapRenderer.PhysicsLayer, (int)PhysicsLayers.Environment);

                //create above map renderer
                var tiledMapDetailsRenderer = mapEntity.AddComponent(new TiledMapRenderer(tmxMap));
                var layersToRender = new List<string>();
                if (tmxMap.Layers.Contains("Front"))
                    layersToRender.Add("Front");
                if (tmxMap.Layers.Contains("AboveFront"))
                    layersToRender.Add("AboveFront");
                tiledMapDetailsRenderer.SetLayersToRender(layersToRender.ToArray());
                tiledMapDetailsRenderer.RenderLayer = RenderLayers.Front;
                tiledMapDetailsRenderer.Material = Material.StencilWrite();
                //tiledMapDetailsRenderer.Material.Effect = Scene.Content.LoadNezEffect<SpriteAlphaTestEffect>();

                TiledHelper.CreateEntitiesForTiledObjects(mapRenderer);
            }

            //create pathfinding graph. anything that is part of a map is a wall in terms of hallway pathfinding
            var graph = new AstarGridGraph((int)_dungeonArea.X, (int)_dungeonArea.Y);
            var mapRenderers = Scene.FindComponentsOfType<TiledMapRenderer>();
            foreach (var renderer in mapRenderers)
            {
                for (var y = 0; y < renderer.TiledMap.Height; y++)
                {
                    for (var x = 0; x < renderer.TiledMap.Width; x++)
                    {
                        graph.Walls.Add(new Point((int)(renderer.Entity.Position.X / 16) + x, (int)(renderer.Entity.Position.Y / 16) + y));
                    }
                }
            }

            //loop through leaf generations, starting from the youngest (highest value)
            for (int i = _leafs.Select(l => l.Generation).Max(); i >= 0; i--)
            {
                //get leafs in this generation
                var leafsInGeneration = _leafs.Where(l => l.Generation == i).ToList();

                //loop through each leaf in this generation
                foreach (var leaf in leafsInGeneration)
                {
                    //don't do anything if either child is null
                    if (leaf.LeftChild == null || leaf.RightChild == null)
                        continue;

                    //get potential rooms for both sides of leaf
                    List<DungeonRoom> leftChildRooms = new List<DungeonRoom>();
                    leaf.LeftChild.GetRooms(ref leftChildRooms);
                    List<DungeonRoom> rightChildRooms = new List<DungeonRoom>();
                    leaf.RightChild.GetRooms(ref rightChildRooms);

                    //get list of potential doorways
                    var leftChildDoorways = leftChildRooms.SelectMany(r => r.Doorways).Where(d => d.HasConnection == false).ToList();
                    var rightChildDoorways = rightChildRooms.SelectMany(r => r.Doorways).Where(d => d.HasConnection == false).ToList();

                    //get the positions of the closest two exits
                    var minDistance = float.MaxValue;
                    DungeonDoorway selectedDoorway1 = null;
                    DungeonDoorway selectedDoorway2 = null;
                    Vector2 room1ExitPosition = new Vector2();
                    Vector2 room2ExitPosition = new Vector2();
                    foreach (var doorway1 in leftChildDoorways)
                    {
                        foreach (var doorway2 in rightChildDoorways)
                        {
                            var pos1 = doorway1.PathfindingOrigin / 16;
                            var pos2 = doorway2.PathfindingOrigin / 16;
                            var dist = Vector2.Distance(pos1, pos2);
                            if (dist < minDistance)
                            {
                                minDistance = dist;
                                room1ExitPosition = pos1;
                                room2ExitPosition = pos2;
                                selectedDoorway1 = doorway1;
                                selectedDoorway2 = doorway2;
                            }
                        }
                    }

                    //set the selected doorways to open
                    selectedDoorway1.SetOpen(true);
                    selectedDoorway2.SetOpen(true);

                    //add padding so hallway can't turn immediately
                    Vector2 room1ExitPositionPadded = new Vector2(room1ExitPosition.X, room1ExitPosition.Y);
                    switch (selectedDoorway1.Direction)
                    {
                        case "Top":
                            room1ExitPositionPadded.Y = selectedDoorway1.DungeonRoom.DungeonLeaf.Position.Y;
                            //room1ExitPositionPadded.Y -= 2;
                            break;
                        case "Bottom":
                            room1ExitPositionPadded.Y = selectedDoorway1.DungeonRoom.DungeonLeaf.Position.Y + selectedDoorway1.DungeonRoom.DungeonLeaf.Size.Y;
                            //room1ExitPositionPadded.Y += 5;
                            break;
                        case "Left":
                            room1ExitPositionPadded.X = selectedDoorway1.DungeonRoom.DungeonLeaf.Position.X;
                            //room1ExitPositionPadded.X -= 3;
                            break;
                        case "Right":
                            room1ExitPositionPadded.X = selectedDoorway1.DungeonRoom.DungeonLeaf.Position.X + selectedDoorway1.DungeonRoom.DungeonLeaf.Size.X;
                            //room1ExitPositionPadded.X += 3;
                            break;
                    }
                    Vector2 room2ExitPositionPadded = new Vector2(room2ExitPosition.X, room2ExitPosition.Y);
                    switch (selectedDoorway2.Direction)
                    {
                        case "Top":
                            room2ExitPositionPadded.Y = selectedDoorway2.DungeonRoom.DungeonLeaf.Position.Y;
                            //room2ExitPositionPadded.Y -= 2;
                            break;
                        case "Bottom":
                            room2ExitPositionPadded.Y = selectedDoorway2.DungeonRoom.DungeonLeaf.Position.Y + selectedDoorway2.DungeonRoom.DungeonLeaf.Size.Y;
                            //room2ExitPositionPadded.Y += 5;
                            break;
                        case "Left":
                            room2ExitPositionPadded.X = selectedDoorway2.DungeonRoom.DungeonLeaf.Position.X;
                            //room2ExitPositionPadded.X -= 3;
                            break;
                        case "Right":
                            room2ExitPositionPadded.X = selectedDoorway2.DungeonRoom.DungeonLeaf.Position.X + selectedDoorway2.DungeonRoom.DungeonLeaf.Size.X;
                            //room2ExitPositionPadded.X += 3;
                            break;
                    }

                    //remove walls from the exit positions
                    graph.Walls.Remove(room1ExitPosition.ToPoint());
                    graph.Walls.Remove(room2ExitPosition.ToPoint());

                    //assemble final path
                    List<Point> finalPath = new List<Point>();
                    var path1 = graph.Search(room1ExitPosition.ToPoint(), room1ExitPositionPadded.ToPoint());
                    if (path1 != null)
                        finalPath.AddRange(path1);
                    var path2 = graph.Search(room1ExitPositionPadded.ToPoint(), room2ExitPositionPadded.ToPoint());
                    if (path2 != null)
                        finalPath.AddRange(path2);
                    var path3 = graph.Search(room2ExitPositionPadded.ToPoint(), room2ExitPosition.ToPoint());
                    if (path3 != null)
                        finalPath.AddRange(path3);

                    //remove duplicate points
                    finalPath = finalPath.Distinct().ToList();

                    //init list of hallways
                    List<HallwayModel> hallwayModels = new List<HallwayModel>();

                    //loop through path
                    for (int j = 1; j < finalPath.Count - 1; j++)
                    {
                        //get previous, current, and next points on path
                        var previousPoint = finalPath[j - 1];
                        var point = finalPath[j];
                        var nextPoint = finalPath[j + 1];

                        //get normalized incoming and outgoing directions
                        Vector2 incomingDirection = new Vector2(point.X - previousPoint.X, point.Y - previousPoint.Y);
                        Vector2 outgoingDirection = new Vector2(nextPoint.X - point.X, nextPoint.Y - point.Y);
                        incomingDirection.Normalize();
                        outgoingDirection.Normalize();

                        //horizontal
                        if (incomingDirection.X == outgoingDirection.X && incomingDirection.Y == 0 && outgoingDirection.Y == 0)
                        {
                            var hallway = new HallwayModel(HallwayMaps.ForgeHorizontal, point, false);
                            hallwayModels.Add(hallway);
                        }
                        //vertical
                        if (incomingDirection.Y == outgoingDirection.Y && incomingDirection.X == 0 && outgoingDirection.X == 0)
                        {
                            var hallway = new HallwayModel(HallwayMaps.ForgeVertical, point, false);
                            hallwayModels.Add(hallway);
                        }
                        //bottom right corner
                        if ((incomingDirection.Y == 1 && outgoingDirection.X == -1) || (incomingDirection.X == 1 && outgoingDirection.Y == -1))
                        {
                            var hallway = new HallwayModel(HallwayMaps.ForgeBottomRight, point);
                            hallwayModels.Add(hallway);

                            var tmxMap = hallway.Map.TmxMap;
                            if (tmxMap.Properties.TryGetValue("PathfindingOffset", out var offsetString))
                            {
                                var offsetValues = offsetString.Split(' ');

                                var prevDirectionValue = 0;
                                var nextDirectionValue = 0;
                                if (incomingDirection.X == 1)
                                {
                                    prevDirectionValue = Convert.ToInt32(offsetValues[0]);
                                    nextDirectionValue = Convert.ToInt32(offsetValues[1]);
                                }

                                if (incomingDirection.Y == 1)
                                {
                                    prevDirectionValue = Convert.ToInt32(offsetValues[1]);
                                    nextDirectionValue = Convert.ToInt32(offsetValues[0]);

                                }

                                var startIndex = Math.Max(hallwayModels.Count - 1 - prevDirectionValue, 0);
                                List<HallwayModel> hallwaysToRemove = new List<HallwayModel>();
                                for (int k = hallwayModels.Count - 2; k >= startIndex; k--)
                                {
                                    var hallwayToRemove = hallwayModels[k];
                                    if (hallwayToRemove.IsCorner)
                                        break;
                                    else
                                        hallwaysToRemove.Add(hallwayToRemove);
                                }

                                foreach (var hallwayToRemove in hallwaysToRemove)
                                {
                                    hallwayModels.Remove(hallwayToRemove);
                                }
                                
                                //var countToRemove = hallwayModels.Count - 1 - startIndex;
                                //hallwayModels.RemoveRange(startIndex, countToRemove);

                                j += nextDirectionValue;
                            }
                        }
                        //bottom left corner
                        else if ((incomingDirection.Y == 1 && outgoingDirection.X == 1) || (incomingDirection.X == -1 && outgoingDirection.Y == -1))
                        {
                            var hallway = new HallwayModel(HallwayMaps.ForgeBottomLeft, point);
                            hallwayModels.Add(hallway);

                            var tmxMap = hallway.Map.TmxMap;
                            if (tmxMap.Properties.TryGetValue("PathfindingOffset", out var offsetString))
                            {
                                var offsetValues = offsetString.Split(' ');

                                var prevDirectionValue = 0;
                                var nextDirectionValue = 0;
                                if (incomingDirection.X == -1)
                                {
                                    prevDirectionValue = Convert.ToInt32(offsetValues[0]);
                                    nextDirectionValue = Convert.ToInt32(offsetValues[1]);
                                }

                                if (incomingDirection.Y == 1)
                                {
                                    prevDirectionValue = Convert.ToInt32(offsetValues[1]);
                                    nextDirectionValue = Convert.ToInt32(offsetValues[0]);

                                }

                                var startIndex = Math.Max(hallwayModels.Count - 1 - prevDirectionValue, 0);
                                List<HallwayModel> hallwaysToRemove = new List<HallwayModel>();
                                for (int k = hallwayModels.Count - 2; k >= startIndex; k--)
                                {
                                    var hallwayToRemove = hallwayModels[k];
                                    if (hallwayToRemove.IsCorner)
                                        break;
                                    else
                                        hallwaysToRemove.Add(hallwayToRemove);
                                }

                                foreach (var hallwayToRemove in hallwaysToRemove)
                                {
                                    hallwayModels.Remove(hallwayToRemove);
                                }

                                //var countToRemove = hallwayModels.Count - 1 - startIndex;
                                //hallwayModels.RemoveRange(startIndex, countToRemove);

                                j += nextDirectionValue;
                            }
                        }
                        //top left corner
                        else if ((incomingDirection.Y == -1 && outgoingDirection.X == 1) || (incomingDirection.X == -1 && outgoingDirection.Y == 1))
                        {
                            var hallway = new HallwayModel(HallwayMaps.ForgeTopLeft, point);
                            hallwayModels.Add(hallway);

                            var tmxMap = hallway.Map.TmxMap;
                            if (tmxMap.Properties.TryGetValue("PathfindingOffset", out var offsetString))
                            {
                                var offsetValues = offsetString.Split(' ');

                                var prevDirectionValue = 0;
                                var nextDirectionValue = 0;
                                if (incomingDirection.X == -1)
                                {
                                    prevDirectionValue = Convert.ToInt32(offsetValues[0]);
                                    nextDirectionValue = Convert.ToInt32(offsetValues[1]) - 3;
                                }

                                if (incomingDirection.Y == -1)
                                {
                                    prevDirectionValue = Convert.ToInt32(offsetValues[1]) - 3;
                                    nextDirectionValue = Convert.ToInt32(offsetValues[0]);
                                }

                                var startIndex = Math.Max(hallwayModels.Count - 1 - prevDirectionValue, 0);
                                List<HallwayModel> hallwaysToRemove = new List<HallwayModel>();
                                for (int k = hallwayModels.Count - 2; k >= startIndex; k--)
                                {
                                    var hallwayToRemove = hallwayModels[k];
                                    if (hallwayToRemove.IsCorner)
                                        break;
                                    else
                                        hallwaysToRemove.Add(hallwayToRemove);
                                }

                                foreach (var hallwayToRemove in hallwaysToRemove)
                                {
                                    hallwayModels.Remove(hallwayToRemove);
                                }

                                //var countToRemove = hallwayModels.Count - 1 - startIndex;
                                //hallwayModels.RemoveRange(startIndex, countToRemove);

                                j += nextDirectionValue;
                            }
                        }
                        //top right corner
                        else if ((incomingDirection.Y == -1 && outgoingDirection.X == -1) || (incomingDirection.X == 1) && (outgoingDirection.Y == 1))
                        {
                            var hallway = new HallwayModel(HallwayMaps.ForgeTopRight, point);
                            hallwayModels.Add(hallway);

                            var tmxMap = hallway.Map.TmxMap;
                            if (tmxMap.Properties.TryGetValue("PathfindingOffset", out var offsetString))
                            {
                                var offsetValues = offsetString.Split(' ');

                                var prevDirectionValue = 0;
                                var nextDirectionValue = 0;
                                if (incomingDirection.X == 1)
                                {
                                    prevDirectionValue = Convert.ToInt32(offsetValues[0]);
                                    nextDirectionValue = Convert.ToInt32(offsetValues[1]) - 3;
                                }

                                if (incomingDirection.Y == -1)
                                {
                                    prevDirectionValue = Convert.ToInt32(offsetValues[1]) - 3;
                                    nextDirectionValue = Convert.ToInt32(offsetValues[0]);
                                }

                                var startIndex = Math.Max(hallwayModels.Count - 1 - prevDirectionValue, 0);
                                List<HallwayModel> hallwaysToRemove = new List<HallwayModel>();
                                for (int k = hallwayModels.Count - 2; k >= startIndex; k--)
                                {
                                    var hallwayToRemove = hallwayModels[k];
                                    if (hallwayToRemove.IsCorner)
                                        break;
                                    else
                                        hallwaysToRemove.Add(hallwayToRemove);
                                }

                                foreach (var hallwayToRemove in hallwaysToRemove)
                                {
                                    hallwayModels.Remove(hallwayToRemove);
                                }

                                //var countToRemove = hallwayModels.Count - 1 - startIndex;
                                //hallwayModels.RemoveRange(startIndex, countToRemove);

                                j += nextDirectionValue;
                            }
                        }
                    }

                    foreach (var point in finalPath)
                    {
                        var ent = Scene.CreateEntity("pathfinding-test");
                        ent.SetPosition(point.X * 16, point.Y * 16);
                        var prototypeSpriteRenderer = ent.AddComponent(new PrototypeSpriteRenderer(4, 4));
                        prototypeSpriteRenderer.RenderLayer = int.MinValue;
                    }

                    //loop through selected hallways and generate their maps
                    foreach (var hallway in hallwayModels)
                    {
                        //load map
                        var tmxMap = hallway.Map.TmxMap;

                        //determine position
                        var hallwayPos = new Vector2(hallway.PathPoint.X, hallway.PathPoint.Y);
                        if (tmxMap.Properties.TryGetValue("PathfindingOffset", out var offsetString))
                        {
                            var offsetValues = offsetString.Split(' ');
                            hallwayPos.X -= Convert.ToInt32(offsetValues[0]);
                            hallwayPos.Y -= Convert.ToInt32(offsetValues[1]);
                        }

                        //create hallway entity and set position
                        var ent = Scene.CreateEntity("hallway");
                        ent.SetPosition(hallwayPos.X * 16, hallwayPos.Y * 16);

                        //create main map renderer
                        var mapRenderer = ent.AddComponent(new TiledMapRenderer(tmxMap, "Walls"));
                        mapRenderer.SetLayersToRender(new[] { "Back", "Walls" });
                        mapRenderer.RenderLayer = 10;
                        Flags.SetFlagExclusive(ref mapRenderer.PhysicsLayer, (int)PhysicsLayers.Environment);

                        //create above map renderer
                        var tiledMapDetailsRenderer = ent.AddComponent(new TiledMapRenderer(tmxMap));
                        var layersToRender = new List<string>();
                        if (tmxMap.Layers.Contains("Front"))
                            layersToRender.Add("Front");
                        if (tmxMap.Layers.Contains("AboveFront"))
                            layersToRender.Add("AboveFront");
                        tiledMapDetailsRenderer.SetLayersToRender(layersToRender.ToArray());
                        tiledMapDetailsRenderer.RenderLayer = RenderLayers.Front;
                        tiledMapDetailsRenderer.Material = Material.StencilWrite();
                        //tiledMapDetailsRenderer.Material.Effect = Scene.Content.LoadNezEffect<SpriteAlphaTestEffect>();

                        TiledHelper.CreateEntitiesForTiledObjects(mapRenderer);
                    }
                }
            }

            //loop through leafs that have left and right children that both have rooms, and connect those rooms
            //foreach (var leaf in _leafs.Where(l => l.LeftChild != null && l.RightChild != null && l.LeftChild.Room != null && l.RightChild.Room != null))
            //{
            //    var room1 = leaf.LeftChild.Room;
            //    var room2 = leaf.RightChild.Room;

            //    //get the positions of the closest two exits
            //    var minDistance = float.MaxValue;
            //    DungeonDoorway selectedDoorway1 = null;
            //    DungeonDoorway selectedDoorway2 = null;
            //    Vector2 room1ExitPosition = new Vector2();
            //    Vector2 room2ExitPosition = new Vector2();
            //    foreach (var doorway1 in room1.Doorways)
            //    {
            //        foreach (var doorway2 in room2.Doorways)
            //        {
            //            var pos1 = (doorway1.Entity.Position / 16) + doorway1.PathfindingOffset;
            //            var pos2 = (doorway2.Entity.Position / 16) + doorway2.PathfindingOffset;
            //            var dist = Vector2.Distance(pos1, pos2);
            //            if (dist < minDistance)
            //            {
            //                minDistance = dist;
            //                room1ExitPosition = pos1;
            //                room2ExitPosition = pos2;
            //                selectedDoorway1 = doorway1;
            //                selectedDoorway2 = doorway2;
            //            }
            //        }
            //    }

            //    //set the selected doorways to open
            //    selectedDoorway1.SetOpen(true);
            //    selectedDoorway2.SetOpen(true);

            //    //add padding so hallway can't turn immediately
            //    Vector2 room1ExitPositionPadded = new Vector2(room1ExitPosition.X, room1ExitPosition.Y);
            //    switch (selectedDoorway1.Direction)
            //    {
            //        case "Top":
            //            room1ExitPositionPadded.Y -= 2;
            //            break;
            //        case "Bottom":
            //            room1ExitPositionPadded.Y += 5;
            //            break;
            //        case "Left":
            //            room1ExitPositionPadded.X -= 3;
            //            break;
            //        case "Right":
            //            room1ExitPositionPadded.X += 3;
            //            break;
            //    }
            //    Vector2 room2ExitPositionPadded = new Vector2(room2ExitPosition.X, room2ExitPosition.Y);
            //    switch (selectedDoorway2.Direction)
            //    {
            //        case "Top":
            //            room2ExitPositionPadded.Y -= 2;
            //            break;
            //        case "Bottom":
            //            room2ExitPositionPadded.Y += 5;
            //            break;
            //        case "Left":
            //            room2ExitPositionPadded.X -= 3;
            //            break;
            //        case "Right":
            //            room2ExitPositionPadded.X += 3;
            //            break;
            //    }

            //    //remove walls from the exit positions
            //    graph.Walls.Remove(room1ExitPosition.ToPoint());
            //    graph.Walls.Remove(room2ExitPosition.ToPoint());

            //    //assemble final path
            //    List<Point> finalPath = new List<Point>();
            //    var path1 = graph.Search(room1ExitPosition.ToPoint(), room1ExitPositionPadded.ToPoint());
            //    if (path1 != null)
            //        finalPath.AddRange(path1);
            //    var path2 = graph.Search(room1ExitPositionPadded.ToPoint(), room2ExitPositionPadded.ToPoint());
            //    if (path2 != null)
            //        finalPath.AddRange(path2);
            //    var path3 = graph.Search(room2ExitPositionPadded.ToPoint(), room2ExitPosition.ToPoint());
            //    if (path3 != null)
            //        finalPath.AddRange(path3);

            //    //remove duplicate points
            //    finalPath = finalPath.Distinct().ToList();

            //    //init list of hallways
            //    List<HallwayModel> hallwayModels = new List<HallwayModel>();

            //    //loop through path
            //    for (int i = 1; i < finalPath.Count - 1; i++)
            //    {
            //        //get previous, current, and next points on path
            //        var previousPoint = finalPath[i - 1];
            //        var point = finalPath[i];
            //        var nextPoint = finalPath[i + 1];

            //        //get normalized incoming and outgoing directions
            //        Vector2 incomingDirection = new Vector2(point.X - previousPoint.X, point.Y - previousPoint.Y);
            //        Vector2 outgoingDirection = new Vector2(nextPoint.X - point.X, nextPoint.Y - point.Y);
            //        incomingDirection.Normalize();
            //        outgoingDirection.Normalize();

            //        //horizontal
            //        if (incomingDirection.X == outgoingDirection.X && incomingDirection.Y == 0 && outgoingDirection.Y == 0)
            //        {
            //            var hallway = new HallwayModel(HallwayMaps.ForgeHorizontal, point, false);
            //            hallwayModels.Add(hallway);
            //        }
            //        //vertical
            //        if (incomingDirection.Y == outgoingDirection.Y && incomingDirection.X == 0 && outgoingDirection.X == 0)
            //        {
            //            var hallway = new HallwayModel(HallwayMaps.ForgeVertical, point, false);
            //            hallwayModels.Add(hallway);
            //        }
            //        //bottom right corner
            //        if ((incomingDirection.Y == 1 && outgoingDirection.X == -1) || (incomingDirection.X == 1 && outgoingDirection.Y == -1))
            //        {
            //            var hallway = new HallwayModel(HallwayMaps.ForgeBottomRight, point);
            //            hallwayModels.Add(hallway);

            //            var tmxMap = hallway.Map.TmxMap;
            //            if (tmxMap.Properties.TryGetValue("PathfindingOffset", out var offsetString))
            //            {
            //                var offsetValues = offsetString.Split(' ');

            //                var prevDirectionValue = 0;
            //                var nextDirectionValue = 0;
            //                if (incomingDirection.X == 1)
            //                {
            //                    prevDirectionValue = Convert.ToInt32(offsetValues[0]);
            //                    nextDirectionValue = Convert.ToInt32(offsetValues[1]);
            //                }

            //                if (incomingDirection.Y == 1)
            //                {
            //                    prevDirectionValue = Convert.ToInt32(offsetValues[1]);
            //                    nextDirectionValue = Convert.ToInt32(offsetValues[0]);
                                
            //                }

            //                var startIndex = Math.Max(hallwayModels.Count - 1 - prevDirectionValue, 0);
            //                var countToRemove = hallwayModels.Count - 1 - startIndex;
            //                hallwayModels.RemoveRange(startIndex, countToRemove);
            //                i += nextDirectionValue;
            //            }
            //        }
            //        //bottom left corner
            //        else if ((incomingDirection.Y == 1 && outgoingDirection.X == 1) || (incomingDirection.X == -1 && outgoingDirection.Y == -1))
            //        {
            //            var hallway = new HallwayModel(HallwayMaps.ForgeBottomLeft, point);
            //            hallwayModels.Add(hallway);

            //            var tmxMap = hallway.Map.TmxMap;
            //            if (tmxMap.Properties.TryGetValue("PathfindingOffset", out var offsetString))
            //            {
            //                var offsetValues = offsetString.Split(' ');

            //                var prevDirectionValue = 0;
            //                var nextDirectionValue = 0;
            //                if (incomingDirection.X == -1)
            //                {
            //                    prevDirectionValue = Convert.ToInt32(offsetValues[0]);
            //                    nextDirectionValue = Convert.ToInt32(offsetValues[1]);
            //                }

            //                if (incomingDirection.Y == 1)
            //                {
            //                    prevDirectionValue = Convert.ToInt32(offsetValues[1]);
            //                    nextDirectionValue = Convert.ToInt32(offsetValues[0]);

            //                }

            //                var startIndex = Math.Max(hallwayModels.Count - 1 - prevDirectionValue, 0);
            //                var countToRemove = hallwayModels.Count - 1 - startIndex;
            //                hallwayModels.RemoveRange(startIndex, countToRemove);
            //                i += nextDirectionValue;
            //            }
            //        }
            //        //top left corner
            //        else if ((incomingDirection.Y == -1 && outgoingDirection.X == 1) || (incomingDirection.X == -1 && outgoingDirection.Y == 1))
            //        {
            //            var hallway = new HallwayModel(HallwayMaps.ForgeTopLeft, point);
            //            hallwayModels.Add(hallway);

            //            var tmxMap = hallway.Map.TmxMap;
            //            if (tmxMap.Properties.TryGetValue("PathfindingOffset", out var offsetString))
            //            {
            //                var offsetValues = offsetString.Split(' ');

            //                var prevDirectionValue = 0;
            //                var nextDirectionValue = 0;
            //                if (incomingDirection.X == -1)
            //                {
            //                    prevDirectionValue = Convert.ToInt32(offsetValues[0]);
            //                    nextDirectionValue = Convert.ToInt32(offsetValues[1]) - 3;
            //                }

            //                if (incomingDirection.Y == -1)
            //                {
            //                    prevDirectionValue = Convert.ToInt32(offsetValues[1]) - 3;
            //                    nextDirectionValue = Convert.ToInt32(offsetValues[0]);
            //                }

            //                var startIndex = Math.Max(hallwayModels.Count - 1 - prevDirectionValue, 0);
            //                var countToRemove = hallwayModels.Count - 1 - startIndex;
            //                hallwayModels.RemoveRange(startIndex, countToRemove);
            //                i += nextDirectionValue;
            //            }
            //        }
            //        //top right corner
            //        else if ((incomingDirection.Y == -1 && outgoingDirection.X == -1) || (incomingDirection.X == 1) && (outgoingDirection.Y == 1))
            //        {
            //            var hallway = new HallwayModel(HallwayMaps.ForgeTopRight, point);
            //            hallwayModels.Add(hallway);

            //            var tmxMap = hallway.Map.TmxMap;
            //            if (tmxMap.Properties.TryGetValue("PathfindingOffset", out var offsetString))
            //            {
            //                var offsetValues = offsetString.Split(' ');

            //                var prevDirectionValue = 0;
            //                var nextDirectionValue = 0;
            //                if (incomingDirection.X == 1)
            //                {
            //                    prevDirectionValue = Convert.ToInt32(offsetValues[0]);
            //                    nextDirectionValue = Convert.ToInt32(offsetValues[1]) - 3;
            //                }

            //                if (incomingDirection.Y == -1)
            //                {
            //                    prevDirectionValue = Convert.ToInt32(offsetValues[1]) - 3;
            //                    nextDirectionValue = Convert.ToInt32(offsetValues[0]);
            //                }

            //                var startIndex = Math.Max(hallwayModels.Count - 1 - prevDirectionValue, 0);
            //                var countToRemove = hallwayModels.Count - 1 - startIndex;
            //                hallwayModels.RemoveRange(startIndex, countToRemove);
            //                i += nextDirectionValue;
            //            }
            //        }
            //    }

            //    //foreach (var point in finalPath)
            //    //{
            //    //    var ent = Scene.CreateEntity("pathfinding-test");
            //    //    ent.SetPosition(point.X * 16, point.Y * 16);
            //    //    var prototypeSpriteRenderer = ent.AddComponent(new PrototypeSpriteRenderer(4, 4));
            //    //    prototypeSpriteRenderer.RenderLayer = int.MinValue;
            //    //}

            //    //loop through selected hallways and generate their maps
            //    foreach (var hallway in hallwayModels)
            //    {
            //        //load map
            //        var tmxMap = hallway.Map.TmxMap;

            //        //determine position
            //        var hallwayPos = new Vector2(hallway.PathPoint.X, hallway.PathPoint.Y);
            //        if (tmxMap.Properties.TryGetValue("PathfindingOffset", out var offsetString))
            //        {
            //            var offsetValues = offsetString.Split(' ');
            //            hallwayPos.X -= Convert.ToInt32(offsetValues[0]);
            //            hallwayPos.Y -= Convert.ToInt32(offsetValues[1]);
            //        }

            //        //create hallway entity and set position
            //        var ent = Scene.CreateEntity("hallway");
            //        ent.SetPosition(hallwayPos.X * 16, hallwayPos.Y * 16);

            //        //create main map renderer
            //        var mapRenderer = ent.AddComponent(new TiledMapRenderer(tmxMap, "Walls"));
            //        mapRenderer.SetLayersToRender(new[] { "Back", "Walls" });
            //        mapRenderer.RenderLayer = 10;
            //        ent.AddComponent(new GridGraphManager(mapRenderer));
            //        ent.AddComponent(new TiledObjectHandler(mapRenderer));
            //        Flags.SetFlagExclusive(ref mapRenderer.PhysicsLayer, (int)PhysicsLayers.Environment);

            //        //create above map renderer
            //        var tiledMapDetailsRenderer = ent.AddComponent(new TiledMapRenderer(tmxMap));
            //        var layersToRender = new List<string>();
            //        if (tmxMap.Layers.Contains("Front"))
            //            layersToRender.Add("Front");
            //        if (tmxMap.Layers.Contains("AboveFront"))
            //            layersToRender.Add("AboveFront");
            //        tiledMapDetailsRenderer.SetLayersToRender(layersToRender.ToArray());
            //        tiledMapDetailsRenderer.RenderLayer = (int)RenderLayers.AboveDetails;
            //        tiledMapDetailsRenderer.Material = Material.StencilWrite();
            //        //tiledMapDetailsRenderer.Material.Effect = Scene.Content.LoadNezEffect<SpriteAlphaTestEffect>();
            //    }
            //}
        }
    }
}
