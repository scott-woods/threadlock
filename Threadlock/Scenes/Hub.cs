using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez;
using Nez.DeferredLighting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Components.TiledComponents;
using Threadlock.Entities;
using Threadlock.Entities.Characters.Enemies.OrbMage;
using Threadlock.Entities.Characters.Player;
using Threadlock.Entities.Characters.Player.PlayerActions;
using Threadlock.Helpers;
using Threadlock.Models;
using Threadlock.SaveData;
using Threadlock.SceneComponents;
using Threadlock.StaticData;
using Threadlock.UI.Canvases;

namespace Threadlock.Scenes
{
    public class Hub : BaseScene
    {
        public override Color SceneColor => new Color(41, 16, 19, 255);

        public override void OnStart()
        {
            base.OnStart();

            var mapEntity = CreateEntity("map");
            var map = Content.LoadTiledMap(Nez.Content.Tiled.Tilemaps.HubMaps.Hub);
            TiledHelper.SetupMap(mapEntity, map);
            TiledHelper.SetupLightingTiles(mapEntity, map);

            var ui = CreateEntity("ui").AddComponent(new CombatUI());

            var playerSpawner = AddSceneComponent(new PlayerSpawner());
            var player = playerSpawner.SpawnPlayer();

            //var mapBounds = TiledHelper.GetActualBounds(mapEntity);
            //var followCam = Camera.AddComponent(new CustomFollowCamera(player, mapBounds.Location.ToVector2(), (mapBounds.Location + mapBounds.GetSize()).ToVector2()));
            var followCam = Camera.AddComponent(new CustomFollowCamera(player));

            Game1.AudioManager.PlayMusic(Nez.Content.Audio.Music.The_bay);

            var actionPick = AddEntity(new ActionPickup(PlayerActionType.FromType<ChainLightning>()));
            actionPick.SetPosition(player.Position + new Vector2(-25, -100));
            var actionPick2 = AddEntity(new ActionPickup(PlayerActionType.FromType<Grip>()));
            actionPick2.SetPosition(actionPick.Position + new Vector2(50, 0));
        }
    }
}
