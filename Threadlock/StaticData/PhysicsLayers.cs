using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.StaticData
{
    public static class PhysicsLayers
    {
        public const int None = 0;
        public const int Environment = 1;
        public const int PlayerHitbox = 2;
        public const int EnemyHurtbox = 3;
        public const int PlayerCollider = 4;
        public const int EnemyCollider = 5;
        public const int EnemyHitbox = 6;
        public const int PlayerHurtbox = 7;
    }
}
