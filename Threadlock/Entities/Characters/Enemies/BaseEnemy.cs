using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.StaticData;

namespace Threadlock.Entities.Characters.Enemies
{
    public abstract class BaseEnemy : Entity
    {
        public abstract int MaxHealth { get; }
        public abstract float BaseSpeed { get; }
        public abstract Vector2 HurtboxSize { get; }
        public abstract Vector2 ColliderSize { get; }
        public abstract Vector2 ColliderOffset { get; }

        public virtual Vector2 AnimatorOffset { get => Vector2.Zero; }

        #region STATIC SETUP METHODS

        public static void SetupBasicEnemy(BaseEnemy enemy)
        {
            //RENDERERS
            var animator = enemy.AddComponent(new SpriteAnimator());
            animator.SetLocalOffset(enemy.AnimatorOffset);
            animator.SetRenderLayer(RenderLayers.YSort);

            enemy.AddComponent(new Shadow(animator));

            enemy.AddComponent(new SelectionComponent(animator, 10));

            enemy.AddComponent(new SpriteFlipper());


            //PHYSICS
            var mover = enemy.AddComponent(new Mover());

            var velocityComponent = enemy.AddComponent(new VelocityComponent(mover));

            var collider = enemy.AddComponent(new BoxCollider(enemy.ColliderOffset.X, enemy.ColliderOffset.Y, enemy.ColliderSize.X, enemy.ColliderSize.Y));
            Flags.SetFlagExclusive(ref collider.PhysicsLayer, PhysicsLayers.EnemyCollider);
            collider.CollidesWithLayers = 0;
            Flags.SetFlag(ref collider.CollidesWithLayers, PhysicsLayers.Environment);
            Flags.SetFlag(ref collider.CollidesWithLayers, PhysicsLayers.EnemyCollider);
            Flags.SetFlag(ref collider.CollidesWithLayers, PhysicsLayers.ProjectilePassableWall);

            var hurtboxCollider = enemy.AddComponent(new BoxCollider(enemy.HurtboxSize.X, enemy.HurtboxSize.Y));
            hurtboxCollider.IsTrigger = true;
            Flags.SetFlagExclusive(ref hurtboxCollider.PhysicsLayer, PhysicsLayers.EnemyHurtbox);
            Flags.SetFlagExclusive(ref hurtboxCollider.CollidesWithLayers, PhysicsLayers.PlayerHitbox);
            enemy.AddComponent(new Hurtbox(hurtboxCollider, 0f, Nez.Content.Audio.Sounds.Chain_bot_damaged));

            enemy.AddComponent(new KnockbackComponent(velocityComponent, 150, .5f));

            enemy.AddComponent(new HealthComponent(enemy.MaxHealth, enemy.MaxHealth));


            //OTHER
            enemy.AddComponent(new DeathComponent("Die", Nez.Content.Audio.Sounds.Enemy_death_1));

            enemy.AddComponent(new Pathfinder(collider));

            enemy.AddComponent(new OriginComponent(collider));

            enemy.AddComponent(new LootDropper(LootTables.BasicEnemy));
        }

        #endregion
    }
}
