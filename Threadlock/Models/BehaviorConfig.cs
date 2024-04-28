using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Models
{
    public class BehaviorConfig
    {
        public string BehaviorTreeName;
        public float ActionCooldown;
        public bool RunsWhenPursued;
        public float PursuitTime;
    }
}
