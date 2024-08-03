using Nez;
using Nez.Console;
using Nez.ImGuiTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Entities.Characters.Player;
using Threadlock.SaveData;

#if DEBUG
namespace Threadlock.DebugTools
{
    public partial class DebugCommands
    {
        [Command("tcl", "Disable player's collision")]
        static void TogglePlayerCollider()
        {
            if (Game1.Scene.FindEntity("Player") is Player player)
                player.Collider.SetEnabled(!player.Collider.Enabled);
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
            {
                Game1.AudioManager.StopMusic();
                Game1.SceneManager.ChangeScene(sceneType, spawnId);
            }
        }

        [Command("sethp", "Set player's hp to any amount")]
        static void SetHp(string hp)
        {
            if (Game1.Scene.FindEntity("Player") is Player player && player.TryGetComponent<HealthComponent>(out var hc))
                hc.Health = Convert.ToInt32(hp);
        }

        [Command("add-dollahs", "Add dollahs")]
        static void AddDollahs(string dollahs)
        {
            PlayerData.Instance.Dollahs += Convert.ToInt32(dollahs);
        }

        [Command("toggle-imgui", "Toggles the Dear ImGui renderer")]
        public static void ToggleImGui()
        {
            // install the service if it isnt already there
            var service = Core.GetGlobalManager<ImGuiManager>();
            if (service == null)
            {
                service = new ImGuiManager();
                Core.RegisterGlobalManager(service);
            }
            else
            {
                service.SetEnabled(!service.Enabled);
            }

            Game1.Instance.IsMouseVisible = !Game1.Instance.IsMouseVisible;
        }
    }
}
#endif