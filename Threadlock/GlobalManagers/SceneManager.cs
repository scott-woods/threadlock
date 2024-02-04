using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.GlobalManagers
{
    public class SceneManager : GlobalManager
    {
        public string TargetSpawnId;

        public void ChangeScene(Type targetSceneType, string targetSpawnId = "")
        {
            TargetSpawnId = targetSpawnId;

            var transition = new FadeTransition(() => Activator.CreateInstance(targetSceneType) as Scene);
            Game1.StartSceneTransition(transition);
        }
    }
}
