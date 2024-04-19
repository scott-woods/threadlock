using Nez;
using Nez.Systems;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Scenes;
using Threadlock.Transitions;

namespace Threadlock.GlobalManagers
{
    public class SceneManager : GlobalManager
    {
        public Emitter<SceneManagerEvents> Emitter = new Emitter<SceneManagerEvents>();
        public string TargetSpawnId;

        public void ChangeScene(Type targetSceneType, string targetSpawnId = "", bool stopMusic = true)
        {
            TargetSpawnId = targetSpawnId;
            
            var transition = new CustomFadeTransition(() => Activator.CreateInstance(targetSceneType) as Scene);
            transition.LoadSceneOnBackgroundThread = true;
            transition.FadeInDuration = .01f;
            transition.DelayBeforeFadeInDuration = .01f;
            transition.OnTransitionCompleted += OnTransitionCompleted;
            transition.OnScreenObscured += OnScreenObscured;
            transition.FadeInStarted += OnFadeInStarted;

            Game1.StartCoroutine(Game1.AudioManager.FadeoutMusic(1.5f));
            Game1.StartSceneTransition(transition);

            Emitter.Emit(SceneManagerEvents.SceneChangeStarted);
        }

        void OnScreenObscured()
        {
            Emitter.Emit(SceneManagerEvents.ScreenObscured);
        }

        void OnFadeInStarted()
        {
            Emitter.Emit(SceneManagerEvents.FadeInStarted);
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
        FadeInStarted
    }
}
