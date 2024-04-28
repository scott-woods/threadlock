using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Models
{
    public class SpriteSheetConfig
    {
        public string FileName;
        public int CellWidth;
        public int CellHeight;
        public List<AnimationConfig> Animations;
    }
}
