using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Entities.Characters.Player;
using Threadlock.Scenes;

namespace Threadlock.GlobalManagers
{
    public class GameStateManager : GlobalManager
    {
        public void HandlePlayerDeath()
        {
            Game1.AudioManager.StopMusic();
            Game1.SceneManager.ChangeScene(typeof(Hub), "0");
            Game1.SceneManager.Emitter.AddObserver(SceneManagerEvents.ScreenObscured, OnScreenObscured);
        }

        void OnScreenObscured()
        {
            Game1.SceneManager.Emitter.RemoveObserver(SceneManagerEvents.ScreenObscured, OnScreenObscured);

            Player.Instance.PrepareForRespawn();
        }
    }
}
