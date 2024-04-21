using Microsoft.Xna.Framework;
using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Models
{
    public class Bobber
    {
        float _frequency;
        float _amplitude;
        Vector2 _basePosition;

        public Bobber(Vector2 basePosition, float frequency = 2f, float amplitude = 2f)
        {
            _basePosition = basePosition;
            _frequency = frequency;
            _amplitude = amplitude;
        }

        public Vector2 GetNextPosition()
        {
            float bobbingOffset = (float)Math.Sin(Time.TotalTime * _frequency) * _amplitude;
            return new Vector2(_basePosition.X, _basePosition.Y - bobbingOffset);
        }
    }
}
