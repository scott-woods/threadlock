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

        [Command("tgm", "Makes actions cost no AP")]
        static void ToggleFreeActions()
        {
            DebugSettings.FreeActions = !DebugSettings.FreeActions;
        }
    }
}
#endif