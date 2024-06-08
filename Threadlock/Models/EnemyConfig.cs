using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Threadlock.Components.EnemyActions;

namespace Threadlock.Models
{
    public class EnemyConfig
    {
        //info
        public string Name;

        //stats
        public int MaxHealth;
        public float BaseSpeed;

        //hurtbox
        public Vector2 HurtboxSize;

        //collider
        public Vector2 ColliderSize;
        public Vector2 ColliderOffset;

        //animator
        public Vector2 AnimatorOffset;
        public List<SpriteSheetConfig> SpriteSheets;

        //behavior
        public BehaviorConfig BehaviorConfig;

        //actions
        public List<string> Actions;
    }
}
