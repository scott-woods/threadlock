using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using Nez.Textures;
using Threadlock.StaticData;

namespace Threadlock.Entities
{
    public class CursorAttach<T> : Entity
    {
        public T Data;

        Sprite _sprite;

        //components
        SpriteRenderer _renderer;

        //other
        MouseCursor _cursor;

        public CursorAttach(Sprite sprite, T data)
        {
            Data = data;
            _sprite = sprite;
        }

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            _cursor = Scene.FindEntity("mouse-cursor") as MouseCursor;
            if (_cursor == null)
                Destroy();

            _renderer = AddComponent(new SpriteRenderer(_sprite));
            _renderer.SetRenderLayer(RenderLayers.ScreenSpaceRenderLayer);
        }

        public override void Update()
        {
            base.Update();

            Position = (_cursor.Position / Game1.ResolutionManager.UIScale.ToVector2()) + new Vector2(_renderer.Width / 2, (_renderer.Height / 2) * -1);
        }
    }
}
