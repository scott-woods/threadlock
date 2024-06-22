using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Entities.Characters.Player.PlayerActions
{
    public class PlayerActionMovementConfig
    {
        public PlayerActionMovementType MovementType;
        public float Speed;
        public float Duration;
        public bool UseAnimationDuration = true;
    }

    public enum PlayerActionMovementType
    {
        InDirection,
        ToTarget,
        Instant
    }
}
