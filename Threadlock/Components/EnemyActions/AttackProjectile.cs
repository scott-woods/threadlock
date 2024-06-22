using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Components.EnemyActions
{
    public class AttackProjectile
    {
        public string ProjectileName;

        /// <summary>
        /// offset in the direction of the target
        /// </summary>
        public float OffsetDistance;

        public bool StartFromTarget;
        public bool PredictTarget;
        public float MinPredictionOffset;
        public float MaxPredictionOffset;

        /// <summary>
        /// offset entity's starting position
        /// </summary>
        public Vector2 EntityOffset;

        public float Delay;
    }
}
