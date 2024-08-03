using Microsoft.Xna.Framework;

namespace Threadlock.Actions
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
