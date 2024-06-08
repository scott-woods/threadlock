using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Components.EnemyActions
{
    public class ProjectileConfig2
    {
        public string Name;

        public int Damage;

        public bool AffectsPlayer;
        public bool AffectsEnemies;
        public bool DestroyOnWalls;

        public bool AttachToOwner;

        public int Radius;
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

        public List<string> DestroyAnimations = new List<string>();
    }

    public class StraightProjectileConfig : ProjectileConfig2
    {
        public float Speed;
    }

    public class InstantProjectileConfig : ProjectileConfig2
    {
        public string PreAttackAnimation;
        public string AttackAnimation;
    }
}
