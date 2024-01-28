using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.PostProcessors
{
    public class JitterPostProcessor : PostProcessor
    {
        public JitterPostProcessor(int executionOrder, Effect effect = null) : base(executionOrder, effect)
        {
        }

        public override void OnAddedToScene(Scene scene)
        {
            base.OnAddedToScene(scene);

            Effect = scene.Content.LoadEffect(Nez.Content.CompiledEffects.JitterDestroyer);
        }

        public override void Process(RenderTarget2D source, RenderTarget2D destination)
        {
            Effect.Parameters["textureSize"].SetValue(new Vector2(source.Width, source.Height));
            base.Process(source, destination);
        }
    }
}
