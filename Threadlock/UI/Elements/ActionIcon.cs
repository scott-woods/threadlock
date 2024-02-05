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
        int _apCost;

        public ActionIcon(Skin skin, string iconId, int apCost)
        {
            _skin = skin;
            _iconId = iconId;
            _apCost = apCost;

            SetScale(2f);

            SetDrawable(_skin.GetDrawable($"Style 4 Icon {iconId}"));
        }

        public void UpdateDisplay(int actionPoints)
        {
            if (actionPoints >= _apCost)
            {
                SetDrawable(_skin.GetDrawable($"Style 3 Icon {_iconId}"));
            }
            else
            {
                SetDrawable(_skin.GetDrawable($"Style 4 Icon {_iconId}"));
            }
        }
    }
}
