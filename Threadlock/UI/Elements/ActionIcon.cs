using Nez.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.UI.Elements
{
    public class ActionIcon : Image
    {
        Skin _skin;
        string _iconId;

        public ActionIcon(Skin skin, string iconId)
        {
            _skin = skin;
            _iconId = iconId;

            SetScale(2f);

            SetDrawable(_skin.GetDrawable($"Style 4 Icon {iconId}"));
        }
    }
}
