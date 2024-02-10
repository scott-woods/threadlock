using Nez;
using Nez.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}
#endif