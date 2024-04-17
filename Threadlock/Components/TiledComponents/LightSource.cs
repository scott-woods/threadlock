using Microsoft.Xna.Framework;
using Nez.DeferredLighting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Helpers;
using Threadlock.StaticData;

namespace Threadlock.Components.TiledComponents
{
    public class LightSource : TiledComponent
    {
        PointLight _pointLight;

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            TiledHelper.ParseLightingProperties(TmxObject.Properties, out var lightColor, out var lightRadius, out var lightIntensity);

            _pointLight = Entity.AddComponent(new PointLight(lightColor));
            _pointLight.SetRadius(lightRadius);
            _pointLight.SetIntensity(lightIntensity);
            _pointLight.SetRenderLayer(RenderLayers.Light);
        }
    }
}
