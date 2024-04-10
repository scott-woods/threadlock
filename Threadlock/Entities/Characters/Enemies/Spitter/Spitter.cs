using Microsoft.Xna.Framework;
using Nez;
using Nez.AI.BehaviorTrees;
using Nez.Sprites;
using Nez.Textures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Threadlock.Components;
using Threadlock.Helpers;
using Threadlock.Models;
using Threadlock.StaticData;

namespace Threadlock.Entities.Characters.Enemies.Spitter
{
    public class Spitter : Enemy<Spitter>
    {
        //consts
        const float _minDistance = 64;
        const float _fastMoveSpeed = 80f;
        const float _attackRange = 128f;
        const float _attackCooldown = 2.5f;
        const float _pursuitDuration = 3f;

        //actions
        SpitAttack _spitAttack;

        //misc
        bool _isOnCooldown = false;
        bool _isPursued = false;
        ITimer _cooldownTimer;
        ITimer _pursuitTimer;

        #region LIFECYCLE

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            if (TryGetComponent<Hurtbox>(out var hurtbox))
                hurtbox.Emitter.AddObserver(HurtboxEventTypes.Hit, OnHurtboxHit);
        }

        public override void OnRemovedFromScene()
        {
            base.OnRemovedFromScene();

            if (TryGetComponent<Hurtbox>(out var hurtbox))
                hurtbox.Emitter.RemoveObserver(HurtboxEventTypes.Hit, OnHurtboxHit);

            _cooldownTimer?.Stop();
            _cooldownTimer = null;

            _pursuitTimer?.Stop();
            _pursuitTimer = null;
        }

        #endregion

        #region ENEMY OVERRIDES

        public override int MaxHealth => 8;

        public override float BaseSpeed => 50f;

        public override Vector2 AnimatorOffset => new Vector2(2, -6);

        public override Vector2 HurtboxSize => new Vector2(11, 20);

        public override Vector2 ColliderSize => new Vector2(6, 6);

        public override Vector2 ColliderOffset => new Vector2(-4, 8);

        public override void Setup()
        {
            SetupBasicEnemy(this);
            AddAnimations();

            _spitAttack = AddComponent(new SpitAttack(this));
        }

        public override BehaviorTree<Spitter> CreateSubTree()
        {
            var tree = BehaviorTreeBuilder<Spitter>.Begin(this)
                .Selector()

                    .Sequence(AbortTypes.LowerPriority) //attack sequence
                        .Conditional(x => x.CanFire())
                        .Action(x => x.ExecuteAction(_spitAttack))
                        .Action(x => x.BeginCooldown())
                    .EndComposite()

                    .Selector(AbortTypes.Self) //if can't attack, choose what to do
                        //if pursued, run away
                        .ConditionalDecorator(x => x.IsPursued())
                        .Sequence()
                            .Action(x => x.MoveAway(TargetEntity, _fastMoveSpeed, _minDistance))
                        .EndComposite()

                        //if no LoS or out of range, move towards target
                        .ConditionalDecorator(x => !EntityHelper.HasLineOfSight(x, TargetEntity) || !x.IsInRange())
                        .Sequence()
                            .Action(x => x.MoveToTarget(TargetEntity, BaseSpeed))
                        .EndComposite()

                        //otherwise idle (tracking target
                        .Sequence()
                            .Action(x => x.Idle(true))
                        .EndComposite()
                    .EndComposite()

                .EndComposite()
            .Build();

            tree.UpdatePeriod = 0;
            return tree;
        }

        #endregion

        void AddAnimations()
        {
            if (TryGetComponent<SpriteAnimator>(out var animator))
            {
                var texture = Scene.Content.LoadTexture(Content.Textures.Characters.Spitter.Spitter_sheet);
                var sprites = Sprite.SpritesFromAtlas(texture, 77, 39);

                animator.AddAnimation("Idle", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 0, 6, 9));
                animator.AddAnimation("Run", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 1, 7, 9));
                animator.AddAnimation("Attack", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 2, 8, 9));
                animator.AddAnimation("Hit", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 3, 3, 9));
                animator.AddAnimation("Die", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 4, 9, 9));
            }
        }

        bool CanFire()
        {
            return !_isOnCooldown && EntityHelper.HasLineOfSight(this, TargetEntity) && !IsPursued() && IsInRange();
        }

        bool IsPursued()
        {
            return _isPursued;
        }

        bool IsInRange()
        {
            return EntityHelper.DistanceToEntity(this, TargetEntity) <= _attackRange;
        }

        #region TASKS

        TaskStatus BeginCooldown()
        {
            _isOnCooldown = true;
            _cooldownTimer = Game1.Schedule(_attackCooldown, timer =>
            {
                _isOnCooldown = false;
            });

            return TaskStatus.Success;
        }

        #endregion

        #region OBSERVERS

        void OnHurtboxHit(HurtboxHit hit)
        {
            _isPursued = true;
            _pursuitTimer?.Stop();
            _pursuitTimer = Game1.Schedule(_pursuitDuration, timer =>
            {
                _isPursued = false;
            });
        }

        #endregion
    }
}
