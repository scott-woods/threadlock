﻿using Microsoft.Xna.Framework;
using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components.TiledComponents;
using Threadlock.Entities.Characters.Player;

namespace Threadlock.SceneComponents
{
    public class PlayerSpawner : SceneComponent
    {
        public Player SpawnPlayer()
        {
            var playerEntity = Scene.AddEntity(new Player());

            var spawnPosition = Vector2.Zero;
            var playerSpawnPoints = Scene.FindComponentsOfType<PlayerSpawnPoint>();
            if (playerSpawnPoints != null && playerSpawnPoints.Count > 0)
            {
                var spawn = playerSpawnPoints.FirstOrDefault(s => s.Id == Game1.SceneManager.TargetSpawnId);
                if (spawn != null)
                {
                    spawnPosition = spawn.Entity.Position;
                }
                else
                    spawnPosition = playerSpawnPoints.First().Entity.Position;
            }

            playerEntity.SetPosition(spawnPosition);

            return playerEntity;
        }
    }
}
