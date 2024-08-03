using Microsoft.Xna.Framework;
using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Components.TiledComponents;
using Threadlock.Entities.Characters.Player;

namespace Threadlock.SceneComponents
{
    public class PlayerSpawner : SceneComponent
    {
        public Player SpawnPlayer()
        {
            var player = Scene.FindEntity("Player") as Player;
            
            if (player == null)
            {
                player = Scene.AddEntity(new Player());
            }

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

            player.SetEnabled(true);

            player.SetPosition(spawnPosition);

            return player;
        }
    }
}
