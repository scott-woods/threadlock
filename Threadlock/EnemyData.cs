using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Entities.Characters.Enemies;
using Threadlock.Models;

namespace Threadlock
{
    public class EnemyData
    {
        public string Name;
        public int MaxHealth;
        public float BaseSpeed;
        public Vector2 HurtboxSize;
        public Vector2 ColliderSize;
        public Vector2 ColliderOffset;
        public Vector2 AnimatorOffset;
        public string IdleAnimation;
        public string MoveAnimation;
        public string HitAnimation;
        public string DeathAnimation;
        public string SpawnAnimation;
        public BehaviorConfig BehaviorConfig;
        public List<string> Actions;

        public Enemy CreateEnemy()
        {
            return new Enemy(this);
        }
    }
}
