using Nez.Tweens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Components.EnemyActions
{
    public class MovementConfig
    {
        public MovementDirection Direction;
        public float Speed;
        public float? FinalSpeed;
        public EaseType EaseType;
        public float Duration;
        public bool UseAnimationDuration = true;
    }
}
