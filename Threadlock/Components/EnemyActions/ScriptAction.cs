using Microsoft.Xna.Framework;
using Nez.Persistence;
using Nez.Tweens;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Helpers;

namespace Threadlock.Components.EnemyActions
{
    public class ScriptAction
    {
        public string Type;
        public Dictionary<string, object> Params;
        public string Command;
    }

    #region PARAMS MODELS

    public class PlayAnimationParams
    {
        public AnimationConfig Animation;
        public Movement Movement;
    }

    public class MeleeAttackParams
    {
        public HitboxConfig Hitbox;
        public AnimationConfig Animation;
        public Movement Movement;
    }

    public class FireProjectileParams
    {
        public string Name;
        public float Speed;
        public bool DestroyOnWall;
        public HitboxConfig Hitbox;
        public AnimationConfig Animation;
        public int LaunchFrame;
    }

    public class WaitParams
    {
        public float Time;
    }

    #endregion

    #region MODELS

    public class HitboxConfig
    {
        public Vector2 Size;
        public float Radius;
        public List<Vector2> Points;
        public Rectangle Rect;
        public int Damage;
        public Vector2 Offset;
        public Vector2 LocalOffset;
        public List<int> ActiveFrames;
        public bool Rotate = true;
    }

    public class Movement
    {
        public float Speed;
        public EaseType EaseType;
        public float FinalSpeed;
        public MovementDirection MovementDirection;
    }

    public class AnimationConfig
    {
        public string Name;
        public bool Loop;
        public float Duration;
        public int[] Frames;
        public Dictionary<int, List<string>> Sounds;
        public List<VfxConfig> Vfx;
    }

    public class VfxConfig
    {
        public string Name;
        public Vector2 Offset;
        public bool Rotate;
        public int StartFrame;
        public bool Flip;
    }

    public enum MovementDirection
    {
        ToTarget,
        AwayFromTarget
    }

    #endregion
}
