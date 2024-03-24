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
        const float _moveSpeed = 50f;
        const float _fastMoveSpeed = 80f;
        const float _attackRange = 128f;
        const float _attackCooldown = 2.5f;
        const float _pursuitDuration = 3f;

        //components
        Mover _mover;
        SpriteAnimator _animator;
        Hurtbox _hurtbox;
        HealthComponent _healthComponent;
        Pathfinder _pathfinder;
        BoxCollider _collider;
        VelocityComponent _velocityComponent;
        KnockbackComponent _knockbackComponent;
        SpriteFlipper _spriteFlipper;
        OriginComponent _originComponent;
        DeathComponent _deathComponent;

        //actions
        SpitAttack _spitAttack;

        //misc
        //float _cooldownTimer = _cooldown;
        bool _isOnCooldown = false;
        bool _isPursued = false;
        ITimer _cooldownTimer;
        ITimer _pursuitTimer;

        #region LIFECYCLE

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            _mover = AddComponent(new Mover());

            _animator = AddComponent(new SpriteAnimator());
            _animator.SetLocalOffset(new Vector2(2, -6));
            _animator.SetRenderLayer(RenderLayers.YSort);
            AddAnimations();

            //hurtbox
            var hurtboxCollider = AddComponent(new BoxCollider(11, 20));
            hurtboxCollider.IsTrigger = true;
            Flags.SetFlagExclusive(ref hurtboxCollider.PhysicsLayer, (int)PhysicsLayers.EnemyHurtbox);
            Flags.SetFlagExclusive(ref hurtboxCollider.CollidesWithLayers, (int)PhysicsLayers.PlayerHitbox);
            _hurtbox = AddComponent(new Hurtbox(hurtboxCollider, 0, Content.Audio.Sounds.Chain_bot_damaged));
            _hurtbox.Emitter.AddObserver(HurtboxEventTypes.Hit, OnHurtboxHit);

            _healthComponent = AddComponent(new HealthComponent(12, 12));

            //collider
            _collider = AddComponent(new BoxCollider(-4, 8, 6, 6));
            Flags.SetFlagExclusive(ref _collider.PhysicsLayer, (int)PhysicsLayers.EnemyCollider);
            _collider.CollidesWithLayers = 0;
            Flags.SetFlag(ref _collider.CollidesWithLayers, (int)PhysicsLayers.Environment);

            _pathfinder = AddComponent(new Pathfinder(_collider));

            _velocityComponent = AddComponent(new VelocityComponent(_mover));

            _knockbackComponent = AddComponent(new KnockbackComponent(_velocityComponent));

            _originComponent = AddComponent(new OriginComponent(_collider));

            _deathComponent = AddComponent(new DeathComponent("Die", Nez.Content.Audio.Sounds.Enemy_death_1));

            _spriteFlipper = AddComponent(new SpriteFlipper());

            //actions
            _spitAttack = AddComponent(new SpitAttack(this));

            BeginCooldown();
        }

        public override void OnRemovedFromScene()
        {
            base.OnRemovedFromScene();

            _hurtbox.Emitter.RemoveObserver(HurtboxEventTypes.Hit, OnHurtboxHit);

            _cooldownTimer?.Stop();
            _cooldownTimer = null;

            _pursuitTimer?.Stop();
            _pursuitTimer = null;
        }

        #endregion

        #region SETUP

        void AddAnimations()
        {
            var texture = Scene.Content.LoadTexture(Content.Textures.Characters.Spitter.Spitter_sheet);
            var sprites = Sprite.SpritesFromAtlas(texture, 77, 39);

            _animator.AddAnimation("Idle", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 0, 6, 9));
            _animator.AddAnimation("Run", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 1, 7, 9));
            _animator.AddAnimation("Attack", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 2, 8, 9));
            _animator.AddAnimation("Hit", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 3, 3, 9));
            _animator.AddAnimation("Die", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 4, 9, 9));
        }

        #endregion

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
                            .Action(x => x.MoveToTarget(TargetEntity, _moveSpeed))
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
