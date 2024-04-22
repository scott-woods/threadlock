using Nez;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.UI.Canvases
{
    public abstract class Menu : UICanvas
    {
        public abstract IEnumerator OpenMenu();
    }
}
