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
            if (File.Exists("Content/Data/DungeonFlows2.json"))
            {
                var json = File.ReadAllText("Content/Data/DungeonFlows2.json");
                flow = Json.FromJson<DungeonFlow>(json);
            }
            else return;

            //get composites
            var dungeonGraph = new DungeonGraph();
            dungeonGraph.ProcessGraph(flow.Nodes);
            var loops = dungeonGraph.Loops;
            var trees = dungeonGraph.Trees;

            foreach (var tree in trees)
            {
                //create root node
                var rootNode = tree[0];
                var rootMap = GetValidMapByNode(rootNode);

                //create root map entity
                var rootMapEntity = Scene.CreateEntity("map");
                var rootMapRenderer = rootMapEntity.AddComponent(new TiledMapRenderer(rootMap, "Walls"));
                rootMapRenderer.SetLayersToRender(new[] { "Back", "Walls" }.Where(l => rootMap.Layers.Contains(l)).ToArray());
                rootMapRenderer.RenderLayer = RenderLayers.Back;
                Flags.SetFlagExclusive(ref rootMapRenderer.PhysicsLayer, PhysicsLayers.Environment);
                rootNode.Entity = rootMapEntity;
                rootNode.Map = rootMap;
                TiledHelper.CreateEntitiesForTiledObjects(rootMapRenderer);
                rootNode.Doorways = Scene.FindComponentsOfType<DungeonDoorway>().Where(d => d.MapEntity == rootMapEntity).ToList();

                var rootMapFrontRenderer = rootMapEntity.AddComponent(new TiledMapRenderer(rootMap));
                rootMapFrontRenderer.SetLayersToRender(new[] { "Front", "AboveFront" }.Where(l => rootMap.Layers.Contains(l)).ToArray());
                rootMapFrontRenderer.RenderLayer = RenderLayers.Front;

                //loop through remaining nodes
                for (int i = 1; i < tree.Count; i++)
                {
                    //get current and previous nodes in tree
                    var node = tree[i];
                    var prevNode = tree[i - 1];

                    //get a valid map for this new node
                    var map = GetValidMapByNode(node);

                    //create map entity
                    var mapEntity = Scene.CreateEntity("map");
                    var mapRenderer = mapEntity.AddComponent(new TiledMapRenderer(map, "Walls"));
                    mapRenderer.SetLayersToRender(new[] { "Back", "Walls" }.Where(l => map.Layers.Contains(l)).ToArray());
                    mapRenderer.RenderLayer = RenderLayers.Back;
                    Flags.SetFlagExclusive(ref mapRenderer.PhysicsLayer, PhysicsLayers.Environment);
                    node.Entity = mapEntity;
                    node.Map = map;
                    TiledHelper.CreateEntitiesForTiledObjects(mapRenderer);

                    var frontRenderer = mapEntity.AddComponent(new TiledMapRenderer(map));
                    frontRenderer.SetLayersToRender(new[] { "Front", "AboveFront" }.Where(l => map.Layers.Contains(l)).ToArray());
                    frontRenderer.RenderLayer = RenderLayers.Front;

                    var prevNodeDoorways = prevNode.Doorways;
                    var newNodeDoorways = Scene.FindComponentsOfType<DungeonDoorway>().Where(d => d.MapEntity == mapEntity).ToList();
                    node.Doorways = newNodeDoorways;

                    var pairs = from d1 in prevNodeDoorways.Where(d => !d.HasConnection)
                                from d2 in newNodeDoorways.Where(d => !d.HasConnection)
                                where d1.IsDirectMatch(d2)
                                select new { PrevDoorway = d1, NextDoorway = d2 };

                    var pairsList = pairs.ToList();

                    var pair = pairsList.RandomItem();

                    var selectedPrevNodeDoorway = pair.PrevDoorway;
                    var selectedNextNodeDoorway = pair.NextDoorway;

                    selectedPrevNodeDoorway.SetOpen(true);
                    selectedNextNodeDoorway.SetOpen(true);

                    //world position of the doorway in previous room
                    var truePrevNodeDoorwayPos = prevNode.Entity.Position + new Vector2(selectedPrevNodeDoorway.TmxObject.X, selectedPrevNodeDoorway.TmxObject.Y);
                    var trueNextNodeDoorwayPos = truePrevNodeDoorwayPos;

                    //determine world pos that new doorway should be based on direction
                    var vDiff = selectedNextNodeDoorway.TmxObject.Height;
                    var hDiff = selectedNextNodeDoorway.TmxObject.Width;
                    switch (selectedPrevNodeDoorway.Direction)
                    {
                        case "Top":
                            trueNextNodeDoorwayPos.Y -= vDiff;
                            break;
                        case "Bottom":
                            trueNextNodeDoorwayPos.Y += vDiff;
                            break;
                        case "Left":
                            trueNextNodeDoorwayPos.X -= hDiff;
                            break;
                        case "Right":
                            trueNextNodeDoorwayPos.X += hDiff;
                            break;
                    }

                    //determine ideal entity position for the new room based on the lined up doorway
                    var entityPos = trueNextNodeDoorwayPos - new Vector2(selectedNextNodeDoorway.TmxObject.X, selectedNextNodeDoorway.TmxObject.Y);

                    mapEntity.SetPosition(entityPos);
                }
            }
        }

        TmxMap GetValidMapByNode(DungeonNode node)
        {
            return Scene.Content.LoadTiledMap(Nez.Content.Tiled.Tilemaps.Forge.Forge_simple_3);
        }
    }
}
