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
using Threadlock.StaticData;

namespace Threadlock.Entities.Characters.Enemies.Spitter
{
    public class Spitter : Enemy<Spitter>
    {
        //consts
        const float _minDistance = 64;
        const float _preferredDistance = 96;
        const float _maxDistance = 192;
        const float _moveSpeed = 50f;
        const float _slowMoveSpeed = 25f;
        const float _cooldown = 3f;

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
        float _cooldownTimer = _cooldown;

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
                .Selector(AbortTypes.Self)
                    .ConditionalDecorator(s => !EntityHelper.HasLineOfSight(this, s.TargetEntity, false) || !s.CanAttack(), false)
                        .Sequence()
                            .Action(s => s.Move())
                            .ParallelSelector()
                                .WaitAction(1f)
                                .Action(s => s.Idle())
                            .EndComposite()
                            .Action(s => s.ExecuteAction(_spitAttack))
                            .Action(s => s.ResetTimer())
                        .EndComposite()
                .EndComposite()
                .Build();

            tree.UpdatePeriod = 0;
            return tree;
        }

        bool CanAttack()
        {
            return _cooldownTimer <= 0f;
        }

        TaskStatus ResetTimer()
        {
            _cooldownTimer = _cooldown;
            return TaskStatus.Success;
        }

        TaskStatus Move()
        {
            if (CanAttack())
                return TaskStatus.Success;

            //get distance to player
            var distanceToPlayer = EntityHelper.DistanceToEntity(this, TargetEntity);

            //invalid firing situations
            if (distanceToPlayer < _minDistance)
                return MoveAway(TargetEntity, _moveSpeed, _minDistance);
            if (distanceToPlayer > _maxDistance || !EntityHelper.HasLineOfSight(this, TargetEntity, false))
                return MoveToTarget(TargetEntity, _moveSpeed);

            //decrement timer
            _cooldownTimer -= Time.DeltaTime;

            //valid firing situations
            if (distanceToPlayer > _preferredDistance)
                return MoveToTarget(TargetEntity, _slowMoveSpeed);

            return Idle();
        }
    }
}
