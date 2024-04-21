using Nez;
using Nez.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Entities.Characters.Player;
using Threadlock.Entities.Characters.Player.States;
using Threadlock.SaveData;
using Threadlock.Scenes;
using Threadlock.UI.Canvases;

namespace Threadlock.GlobalManagers
{
    public class GameStateManager : GlobalManager
    {
        public GameState GameState = GameState.Normal;
        public Emitter<GameStateEvents> Emitter = new Emitter<GameStateEvents>();

        GameState _previousGameState;
        PauseMenu _pauseMenu;

        public void Pause()
        {
            if (Player.Instance.StateMachine.CurrentState.GetType() == typeof(ActionState))
                return;
            
            //update state
            _previousGameState = GameState;
            GameState = GameState.Paused;

            //disable other ui
            //foreach (var canvas in Game1.Scene.FindComponentsOfType<UICanvas>())
            //    canvas.SetEnabled(false);

            //show pause menu
            _pauseMenu = Game1.Scene.CreateEntity("pause-menu")
                .AddComponent(new PauseMenu(Unpause));

            Time.TimeScale = 0;

            //emit
            Emitter.Emit(GameStateEvents.Paused);
        }

        void Unpause()
        {
            //sound
            Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds._100_Unpause_06);

            //update state
            GameState = _previousGameState;
            _previousGameState = GameState.Paused;

            //destroy menu if necessary
            if (!_pauseMenu.Entity.IsDestroyed)
                _pauseMenu.Entity.Destroy();

            _pauseMenu = null;

            //enable other ui
            //foreach (var canvas in Game1.Scene.FindComponentsOfType<UICanvas>())
            //    canvas.SetEnabled(true);

            Time.TimeScale = 1;

            //emit
            Emitter.Emit(GameStateEvents.Unpaused);
        }

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

    public enum GameState
    {
        Normal,
        Paused
    }

    public enum GameStateEvents
    {
        Paused,
        Unpaused
    }
}
