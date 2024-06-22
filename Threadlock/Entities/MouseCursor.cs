using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Entities.Characters.Player;
using Threadlock.StaticData;

namespace Threadlock.Entities
{
    public class MouseCursor : Entity
    {
        public static readonly string EntityName = "mouse-cursor";

        SpriteRenderer _spriteRenderer;

        public MouseCursor() : base(EntityName) { }

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            var texture = Scene.Content.LoadTexture(Nez.Content.Textures.UI.Crosshair038);
            _spriteRenderer = AddComponent(new SpriteRenderer(texture));
            _spriteRenderer.SetRenderLayer(RenderLayers.Cursor);

            Scale = new Vector2(.25f, .25f);

            //Scale = Screen.Size / Game1.ResolutionManager.DesignResolution.ToVector2();
        }

        public override void Update()
        {
            base.Update();

            Position = Scene.Camera.MouseToWorldPoint();
        }
    }
}
