using Microsoft.Xna.Framework;
using Nez.DeferredLighting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.StaticData;

namespace Threadlock.Components.TiledComponents
{
    public class LightSource : TiledComponent
    {
        PointLight _pointLight;

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            _pointLight = Entity.AddComponent(new PointLight(Color.White));
            _pointLight.SetRadius(250f);
            _pointLight.SetIntensity(1f);
            _pointLight.SetRenderLayer(RenderLayers.Light);
        }
    }
}
