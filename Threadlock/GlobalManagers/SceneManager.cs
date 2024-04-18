using Nez;
using Nez.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.GlobalManagers
{
    public class SceneManager : GlobalManager
    {
        public Emitter<SceneManagerEvents> Emitter = new Emitter<SceneManagerEvents>();
        public string TargetSpawnId;

        public void ChangeScene(Type targetSceneType, string targetSpawnId = "", bool stopMusic = true)
        {
            TargetSpawnId = targetSpawnId;
            
            var transition = new FadeTransition(() => LoadScene(targetSceneType).Result);
            transition.LoadSceneOnBackgroundThread = true;
            transition.FadeInDuration = .01f;
            transition.DelayBeforeFadeInDuration = .01f;
            transition.OnTransitionCompleted += OnTransitionCompleted;
            transition.OnScreenObscured += OnScreenObscured;

            Game1.StartCoroutine(Game1.AudioManager.FadeoutMusic(transition.FadeOutDuration));
            Game1.StartSceneTransition(transition);

            Emitter.Emit(SceneManagerEvents.SceneChangeStarted);
        }

        async Task<Scene> LoadScene(Type targetSceneType)
        {
            Scene scene = null;

            await Task.Run(() =>
            {
                scene = Activator.CreateInstance(targetSceneType) as Scene;
                Task.Delay(10000).Wait();
            });

            return scene;
        }

        void OnScreenObscured()
        {
            Emitter.Emit(SceneManagerEvents.ScreenObscured);
        }

        void OnTransitionCompleted()
        {
            Emitter.Emit(SceneManagerEvents.SceneChangeFinished);
        }
    }

    public enum SceneManagerEvents
    {
        SceneChangeStarted,
        ScreenObscured,
        SceneChangeFinished,
    }
}
