﻿using Nez;
using Nez.Sprites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.StaticData;

namespace Threadlock.Entities
{
    public class MouseCursor : Entity
    {
        SpriteRenderer _spriteRenderer;

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            var texture = Scene.Content.LoadTexture(Nez.Content.Textures.UI.Crosshair038);
            _spriteRenderer = AddComponent(new SpriteRenderer(texture));
            _spriteRenderer.SetRenderLayer(RenderLayers.Cursor);

            Scale = Screen.Size / Game1.ResolutionManager.DesignResolution.ToVector2();
        }

        public override void Update()
        {
            base.Update();

            Position = Input.RawMousePosition.ToVector2();
        }
    }
}
