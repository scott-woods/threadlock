﻿using Nez;
using Nez.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Entities.Characters.Player;

#if DEBUG
namespace Threadlock.DebugTools
{
    public partial class DebugCommands
    {
        [Command("tcl", "Disable player's collision")]
        static void TogglePlayerCollider()
        {
            if (Player.Instance != null)
            {
                Player.Instance.Collider.SetEnabled(!Player.Instance.Collider.Enabled);
            }
        }

        [Command("tgm", "Toggle god mode (free actions, don't take damage)")]
        static void ToggleGodMode()
        {
            DebugSettings.FreeActions = !DebugSettings.FreeActions;
            DebugSettings.PlayerHurtboxEnabled = !DebugSettings.PlayerHurtboxEnabled;
        }

        [Command("tai", "Toggle enemy AI")]
        static void ToggleEnemyAI()
        {
            DebugSettings.EnemyAIEnabled = !DebugSettings.EnemyAIEnabled;
        }

        [Command("goto", "Try to load a different scene")]
        static void GoToScene(string sceneName, string spawnId)
        {
            var sceneType = Type.GetType($"Threadlock.Scenes.{sceneName}");
            if (sceneType != null)
                Game1.SceneManager.ChangeScene(sceneType, spawnId);
        }

        [Command("sethp", "Set player's hp to any amount")]
        static void SetHp(string hp)
        {
            if (Player.Instance != null)
            {
                if (Player.Instance.TryGetComponent<HealthComponent>(out var hc))
                    hc.Health = Convert.ToInt32(hp);
            }
        }
    }
}
#endif