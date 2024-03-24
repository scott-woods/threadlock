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

        public void ChangeScene(Type targetSceneType, string targetSpawnId = "")
        {
            TargetSpawnId = targetSpawnId;
            
            var transition = new FadeTransition(() => Activator.CreateInstance(targetSceneType) as Scene);
            transition.OnTransitionCompleted += OnTransitionCompleted;
            transition.OnScreenObscured += OnScreenObscured;
            Game1.StartSceneTransition(transition);

            Emitter.Emit(SceneManagerEvents.SceneChangeStarted);
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
