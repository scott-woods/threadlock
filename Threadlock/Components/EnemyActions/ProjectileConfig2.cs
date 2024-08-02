using Microsoft.Xna.Framework;
using Nez.Tweens;
using System.Collections.Generic;
using Threadlock.StaticData;

namespace Threadlock.Components.EnemyActions
{
    public class ProjectileConfig2
    {
        public string Name;

        public int Damage;

        public bool AttachToOwner;

        public int? Radius;
        public Vector2 Size;
        public List<Vector2> Points = new List<Vector2>();

        public bool ShouldRotate;
        public float MaxRotation = 90;

        public float Lifespan;

        public string LaunchAnimation;
        public float LaunchDuration;

        public string Animation;
        public Vector2 AnimationOffset;
        public bool DestroyAfterAnimation;

        public float HitboxActiveDuration;

        /// <summary>
        /// List of animations to choose from for when this projectile is destroyed
        /// </summary>
        public List<string> DestroyAnimations = new List<string>();

        /// <summary>
        /// hit effects when hitting a specific layer
        /// </summary>
        public List<HitEffect> HitEffects = new List<HitEffect>();
        /// <summary>
        /// hit vfx when hitting something
        /// </summary>
        public List<string> HitVfx = new List<string>();

        public List<string> PhysicsLayers = new List<string>();
        public bool AffectsPlayer;
        public bool AffectsEnemies;

        public bool DestroyOnHit;

        public bool DestroyOnWalls;
    }

    public class StraightProjectileConfig : ProjectileConfig2
    {
        public float Speed;
        public float? InitialSpeed;
        public float? TimeToFinalSpeed;
        public EaseType? EaseType;
    }

    public class InstantProjectileConfig : ProjectileConfig2
    {
        public string PreAttackAnimation;
        public string AttackAnimation;
    }

    public class ExplosionProjectileConfig : ProjectileConfig2
    {
        public float ExplosionTime;
        public EaseType EaseType;
        public float InitialRadius;
        public float FinalRadius;
    }
}
