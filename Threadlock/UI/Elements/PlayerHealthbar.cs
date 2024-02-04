using Nez.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.UI.Elements
{
    public class PlayerHealthbar : ProgressBar
    {
        const string _styleName = "playerHealthBar";

        public PlayerHealthbar(Skin skin, string styleName = null) : base(skin, styleName)
        {
        }

        public PlayerHealthbar(float min, float max, float stepSize, bool vertical, ProgressBarStyle style) : base(min, max, stepSize, vertical, style)
        {
        }

        public PlayerHealthbar(float min, float max, float stepSize, bool vertical, Skin skin, string styleName = null) : base(min, max, stepSize, vertical, skin, styleName)
        {
        }
    }
}
