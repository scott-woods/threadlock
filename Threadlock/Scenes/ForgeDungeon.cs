using Microsoft.Xna.Framework;
using Nez;
using Nez.Tiled;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Threadlock.Components;
using Threadlock.Components.TiledComponents;
using Threadlock.Helpers;
using Threadlock.SceneComponents;
using Threadlock.SceneComponents.Dungenerator;
using Threadlock.StaticData;
using Threadlock.UI.Canvases;

namespace Threadlock.Scenes
{
    public class ForgeDungeon : BaseScene
    {
        Dungenerator _dungenerator;
        PlayerSpawner _playerSpawner;

        public override void Initialize()
        {
            base.Initialize();

            CreateEntity("ui").AddComponent(new CombatUI());

            _dungenerator = AddSceneComponent(new Dungenerator());
            _playerSpawner = AddSceneComponent(new PlayerSpawner());
        }

        public override void OnStart()
        {
            base.OnStart();

            //_generator.Generate();
            _dungenerator.Generate();

            //var map = Content.LoadTiledMap(Nez.Content.Tiled.Tilemaps.Forge.Forge_start);
            //var mapEntity = CreateEntity("map");
            //var mapRenderer = mapEntity.AddComponent(new TiledMapRenderer(map, "Walls"));
            //mapRenderer.SetRenderLayer(RenderLayers.Back);
            //mapRenderer.SetLayersToRender("Back", "Back2", "Walls");
            //Flags.SetFlagExclusive(ref mapRenderer.PhysicsLayer, PhysicsLayers.Environment);
            //TiledHelper.CreateEntitiesForTiledObjects(mapRenderer);

            //var frontRenderer = mapEntity.AddComponent(new TiledMapRenderer(map));
            //frontRenderer.SetLayersToRender("Front");
            //frontRenderer.SetRenderLayer(RenderLayers.Front);

            //List<Vector2> allFloorPositions = new List<Vector2>();

            //List<Vector2> reservedPositions = new List<Vector2>();
            //reservedPositions.AddRange(TiledHelper.GetTilePositionsByLayer(mapEntity, "Back"));

            //foreach (var doorway in FindComponentsOfType<DungeonDoorway>())
            //{
            //    if (doorway.HasConnection)
            //        reservedPositions.AddRange(TiledHelper.GetTilePositionsByLayer(doorway.Entity, "Back"));
            //    allFloorPositions.AddRange(TiledHelper.GetTilePositionsByLayer(doorway.Entity, "Fill"));
            //}

            //using (var stream = TitleContainer.OpenStream(Nez.Content.Tiled.Tilesets.Forge_tileset))
            //{
            //    var xDocTileset = XDocument.Load(stream);

            //    string tsxDir = Path.GetDirectoryName(Nez.Content.Tiled.Tilesets.Forge_tileset);
            //    var tileset = new TmxTileset().LoadTmxTileset(null, xDocTileset.Element("tileset"), 0, tsxDir);
            //    tileset.TmxDirectory = tsxDir;

            //    //var tileRenderers = CorridorPainter.PaintFloorTiles(floorPositions, tileset, endEntity);
            //    CorridorPainter.PaintCorridorTiles(allFloorPositions, reservedPositions, tileset);
            //}

            //var map = DoorwayMaps.ForgeRightOpen.TmxMap;
            //var mapRenderer = CreateEntity("map").AddComponent(new TiledMapRenderer(map, "Walls"));
            //mapRenderer.SetLayersToRender("Back", "Walls");
            //Flags.SetFlagExclusive(ref mapRenderer.PhysicsLayer, PhysicsLayers.Environment);

            var player = _playerSpawner.SpawnPlayer();

            Camera.Entity.AddComponent(new CustomFollowCamera(player));
        }
    }
}
