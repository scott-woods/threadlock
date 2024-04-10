using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Effects
{
    public class SpriteOutline : Effect
    {
        const float _sizeScale = 8;

        public Color OutlineColor
        {
            get => new Color(_outlineColor);
            set
            {
                var outlineVec = value.ToVector4();
                if (_outlineColor != outlineVec)
                {
                    _outlineColor = outlineVec;
                    _outlineColorParam.SetValue(_outlineColor);
                }
            }
        }

        public Vector2 TextureSize
        {
            get => _textureSize;
            set
            {
                if (_textureSize != value)
                {
                    _textureSize = value;
                    _textureSizeParam.SetValue(_textureSize * _sizeScale);
                }
            }
        }

        Vector2 _textureSize = new Vector2(1024, 1024);
        Vector4 _outlineColor = new Vector4(1, 1, 1, 0);
        EffectParameter _outlineColorParam;
        EffectParameter _textureSizeParam;

        public SpriteOutline() : base(Core.GraphicsDevice, EffectResource.GetFileResourceBytes(Content.CompiledEffects.SpriteOutline))
        {
            _outlineColorParam = Parameters["_outlineColor"];
            _outlineColorParam.SetValue(_outlineColor);

            _textureSizeParam = Parameters["_textureSize"];
            _textureSizeParam.SetValue(_textureSize * _sizeScale);
        }
    }
}
