using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Entities.Characters;
using Threadlock.Entities.Characters.Enemies.ChainBot;
using Threadlock.Entities.Characters.Player;
using Threadlock.StaticData;

namespace Threadlock.Scenes
{
    public class InitialScene : BaseScene
    {
        public override void Initialize()
        {
            base.Initialize();

            var mapEntity = CreateEntity("map");
            var map = Content.LoadTiledMap(Nez.Content.Tiled.Tilemaps.Test);
            var mapRenderer = mapEntity.AddComponent(new TiledMapRenderer(map, "Walls"));
            mapRenderer.SetLayersToRender(new[] { "Back", "Walls" }.Where(l => map.Layers.Contains(l)).ToArray());
            mapRenderer.RenderLayer = RenderLayers.DefaultMapLayer;
            Flags.SetFlagExclusive(ref mapRenderer.PhysicsLayer, PhysicsLayers.None);

            var frontMapRenderer = mapEntity.AddComponent(new TiledMapRenderer(map));
            frontMapRenderer.SetLayersToRender(new[] { "Front", "AboveFront" }.Where(l => map.Layers.Contains(l)).ToArray());
            frontMapRenderer.RenderLayer = RenderLayers.Front;

            var player = AddEntity(new Player());
            var playerPos = mapEntity.Position + (new Vector2(map.Width / 2, map.Height / 2) * map.TileWidth);
            player.SetPosition(playerPos);

            var chainBot = AddEntity(new ChainBot());

            var followCam = Camera.AddComponent(new CustomFollowCamera(player));
        }
    }
}
