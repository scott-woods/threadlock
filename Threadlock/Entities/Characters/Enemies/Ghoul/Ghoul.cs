using Microsoft.Xna.Framework;
using Nez;
using Nez.AI.BehaviorTrees;
using Nez.Sprites;
using Nez.Textures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Helpers;
using Threadlock.StaticData;

namespace Threadlock.Entities.Characters.Enemies.Ghoul
{
    public class Ghoul : Enemy<Ghoul>
    {
        //actions
        GhoulAttack _ghoulAttack;

        #region ENEMY OVERRIDES

        public override int MaxHealth => 7;

        public override float BaseSpeed => 165f;

        public override Vector2 HurtboxSize => new Vector2(10, 17);

        public override Vector2 ColliderSize => new Vector2(9, 6);

        public override Vector2 ColliderOffset => new Vector2(0, 9);

        public override void Setup()
        {
            SetupBasicEnemy(this);
            AddAnimations();

            _ghoulAttack = AddComponent(new GhoulAttack(this));
        }

        public override BehaviorTree<Ghoul> CreateSubTree()
        {
            var tree = BehaviorTreeBuilder<Ghoul>.Begin(this)
                .Sequence()
                    .Selector()
                        .Sequence(AbortTypes.LowerPriority)
                            .Conditional(c => c.IsInAttackRange())
                            .Action(c => c.ExecuteAction(_ghoulAttack))
                            .ParallelSelector()
                                .Action(c => c.Idle())
                                .Action(c => c.TrackTarget(TargetEntity))
                                .WaitAction(.5f)
                            .EndComposite()
                        .EndComposite()
                        .Sequence(AbortTypes.LowerPriority)
                            .Action(c => c.MoveToTarget(TargetEntity, BaseSpeed))
                        .EndComposite()
                    .EndComposite()
                .EndComposite()
                .Build();

            tree.UpdatePeriod = 0f;

            return tree;
        }

        #endregion

        void AddAnimations()
        {
            if (TryGetComponent<SpriteAnimator>(out var animator))
            {
                var texture = Scene.Content.LoadTexture(Content.Textures.Characters.Ghoul.Ghoul_sprites);
                var sprites = Sprite.SpritesFromAtlas(texture, 62, 33);
                animator.AddAnimation("Idle", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 0, 4, 11));
                animator.AddAnimation("Run", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 1, 9, 11));
                animator.AddAnimation("Attack", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 2, 7, 11));
                animator.AddAnimation("Hit", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 3, 2, 11));
                animator.AddAnimation("Die", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 4, 8, 11));
                animator.AddAnimation("Spawn", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 5, 11, 11));
            }
        }

        bool IsInAttackRange()
        {
            var target = TargetEntity;
            var distance = EntityHelper.DirectionToEntity(this, target, false);
            if (Math.Abs(distance.X) <= 16 && Math.Abs(distance.Y) <= 8)
                return true;
            return false;
        }
    }
}
