using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Models;

namespace Threadlock.Components.EnemyActions
{
    public class AnimationConfig2
    {
        public string Name;
        public string Path;

        public int CellWidth;
        public int CellHeight;

        public int Row;
        public int Frames;
        public int StartFrame;

        public bool Loop;

        public string ChainTo;

        public Dictionary<int, FrameData> FrameData = new Dictionary<int, FrameData>();
    }
}
