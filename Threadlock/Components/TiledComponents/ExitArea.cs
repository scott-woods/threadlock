using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Components.TiledComponents
{
    public class ExitArea : AreaTrigger
    {
        Type _targetSceneType;
        string _targetSpawn;

        public override void Initialize()
        {
            base.Initialize();

            if (TmxObject.Properties.TryGetValue("TargetScene", out var targetScene))
                _targetSceneType = Type.GetType("Threadlock.Scenes." + targetScene);
            if (TmxObject.Properties.TryGetValue("TargetSpawn", out var targetSpawn))
                _targetSpawn = targetSpawn;
        }

        public override void OnTriggered()
        {
            Game1.SceneManager.ChangeScene(_targetSceneType, _targetSpawn);

            Entity.Destroy();
        }
    }
}
