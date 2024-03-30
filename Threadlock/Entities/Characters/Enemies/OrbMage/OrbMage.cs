using Nez;
using Nez.AI.BehaviorTrees;
using Nez.Sprites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Microsoft.Xna.Framework;
using Threadlock.StaticData;
using Nez.Textures;
using TaskStatus = Nez.AI.BehaviorTrees.TaskStatus;
using Threadlock.Helpers;

namespace Threadlock.Entities.Characters.Enemies.OrbMage
{
    public class OrbMage : Enemy<OrbMage>
    {
        //consts
        const float _speed = 45f;
        const float _minDistanceToPlayer = 48f;
        const float _attackRange = 128f;
        const float _preferredDistanceToPlayer = 80f;
        const float _sweepAttackRange = 64f;
        const float _attackPrepTime = 3f;
        const float _attackCooldown = 2.5f;

        //components
        Mover _mover;
        SpriteAnimator _animator;
        Hurtbox _hurtbox;
        HealthComponent _healthComponent;
        Pathfinder _pathfinder;
        BoxCollider _collider;
        VelocityComponent _velocityComponent;
        KnockbackComponent _knockbackComponent;
        OriginComponent _originComponent;
        DeathComponent _deathComponent;
        SpriteFlipper _spriteFlipper;

        //actions
        OrbMageAttack _orbMageAttack;
        OrbMageSweepAttack _orbMageSweepAttack;

        float _attackPrepTimer = 0f;
        ITimer _cooldownTimer;
        bool _isOnCooldown = false;

        #region LIFECYCLE

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            //actions
            _orbMageAttack = AddComponent(new OrbMageAttack(this));
            _orbMageSweepAttack = AddComponent(new OrbMageSweepAttack(this));

            _mover = AddComponent(new Mover());

            _animator = AddComponent(new SpriteAnimator());
            _animator.SetLocalOffset(new Vector2(38, -5));
            _animator.SetRenderLayer(RenderLayers.YSort);
            AddAnimations();

            //hurtbox
            var hurtboxCollider = AddComponent(new BoxCollider(9, 24));
            hurtboxCollider.IsTrigger = true;
            Flags.SetFlagExclusive(ref hurtboxCollider.PhysicsLayer, PhysicsLayers.EnemyHurtbox);
            Flags.SetFlagExclusive(ref hurtboxCollider.CollidesWithLayers, PhysicsLayers.PlayerHitbox);
            _hurtbox = AddComponent(new Hurtbox(hurtboxCollider, 0, Content.Audio.Sounds.Chain_bot_damaged));

            _healthComponent = AddComponent(new HealthComponent(12, 12));

            _velocityComponent = AddComponent(new VelocityComponent(_mover));

            _collider = AddComponent(new BoxCollider(-5, 5, 10, 7));
            Flags.SetFlagExclusive(ref _collider.PhysicsLayer, PhysicsLayers.EnemyCollider);
            _collider.CollidesWithLayers = 0;
            Flags.SetFlag(ref _collider.CollidesWithLayers, PhysicsLayers.Environment);
            Flags.SetFlag(ref _collider.CollidesWithLayers, PhysicsLayers.EnemyCollider);

            _originComponent = AddComponent(new OriginComponent(_collider));

            _pathfinder = AddComponent(new Pathfinder(_collider));

            _spriteFlipper = AddComponent(new SpriteFlipper());

            _deathComponent = AddComponent(new DeathComponent("Die", Nez.Content.Audio.Sounds.Enemy_death_1));

            _knockbackComponent = AddComponent(new KnockbackComponent(_velocityComponent));

            AddComponent(new LootDropper(LootTables.BasicEnemy));

            var shadow = AddComponent(new Shadow(_animator));
        }

        public override void OnRemovedFromScene()
        {
            base.OnRemovedFromScene();

            _cooldownTimer?.Stop();
            _cooldownTimer = null;
        }

        #endregion

        #region SETUP

        void AddAnimations()
        {
            var idleTexture = Scene.Content.LoadTexture(Nez.Content.Textures.Characters.OrbMage.Idle);
            var idleSprites = Sprite.SpritesFromAtlas(idleTexture, 119, 34);
            _animator.AddAnimation("Idle", idleSprites.ToArray());

            var moveTexture = Scene.Content.LoadTexture(Nez.Content.Textures.Characters.OrbMage.Move);
            var moveSprites = Sprite.SpritesFromAtlas(moveTexture, 119, 34);
            _animator.AddAnimation("Run", moveSprites.ToArray());

            var attackTexture = Scene.Content.LoadTexture(Nez.Content.Textures.Characters.OrbMage.Attack);
            var attackSprites = Sprite.SpritesFromAtlas(attackTexture, 119, 34);
            _animator.AddAnimation("Attack", attackSprites.ToArray());

            var sweepAttackTexture = Scene.Content.LoadTexture(Nez.Content.Textures.Characters.OrbMage.Sweepattack);
            var sweepAttackSprites = Sprite.SpritesFromAtlas(sweepAttackTexture, 119, 34);
            _animator.AddAnimation("SweepAttack", sweepAttackSprites.ToArray());

            var hitTexture = Scene.Content.LoadTexture(Nez.Content.Textures.Characters.OrbMage.Hit);
            var hitSprites = Sprite.SpritesFromAtlas(hitTexture, 119, 34);
            _animator.AddAnimation("Hit", hitSprites.ToArray());

            var deathTexture = Scene.Content.LoadTexture(Nez.Content.Textures.Characters.OrbMage.Death);
            var deathSprites = Sprite.SpritesFromAtlas(deathTexture, 119, 34);
            _animator.AddAnimation("Die", deathSprites.ToArray());
        }

        #endregion

        #region OVERRIDES

        public override BehaviorTree<OrbMage> CreateSubTree()
        {
            var tree = BehaviorTreeBuilder<OrbMage>.Begin(this)
                .Selector(AbortTypes.Self)

                    .Sequence()
                        .Conditional(x => !x.IsOnCooldown())
                        .Selector() //attack selector
                            .Sequence() //magic ranged attack
                                .Conditional(x => x.IsInMagicAttackRange())
                                .ParallelSelector()
                                    .Action(x => x.TrackTarget(TargetEntity))
                                    .Action(x => x.ExecuteAction(_orbMageAttack))
                                .EndComposite()
                                .Action(x => x.StartCooldownTimer())
                            .EndComposite()
                            .Sequence() //melee sweep attack
                                .Conditional(x => x.IsInMeleeRange())
                                .ParallelSelector()
                                    .Action(x => x.TrackTarget(TargetEntity))
                                    .Action(x => x.ExecuteAction(_orbMageSweepAttack))
                                .EndComposite()
                                .Action(x => x.StartCooldownTimer())
                            .EndComposite()
                        .EndComposite()
                    .EndComposite()

                    .ConditionalDecorator(x => x.IsOnCooldown() || (!x.IsInMagicAttackRange() && !x.IsInMeleeRange()))
                    .Selector() //move or idle
                        .Sequence(AbortTypes.LowerPriority)
                            .Conditional(x => x.IsPlayerTooClose())
                            .Action(x => x.MoveAway(TargetEntity, _speed, _minDistanceToPlayer))
                        .EndComposite()
                        .Sequence(AbortTypes.LowerPriority) //move towards player if too far away
                            .Conditional(x => x.IsPlayerTooFar())
                            .Action(x => x.MoveToTarget(TargetEntity, _speed))
                        .EndComposite()
                        .ParallelSelector() //idle
                            .Action(x => x.TrackTarget(TargetEntity))
                            .Action(x => x.Idle())
                        .EndComposite()
                    .EndComposite()

                .EndComposite()
            .Build();

            tree.UpdatePeriod = 0;
            return tree;
        }

        #endregion

        bool IsInMagicAttackRange()
        {
            var dist = EntityHelper.DistanceToEntity(this, TargetEntity);
            return dist <= _attackRange && dist > _sweepAttackRange;
        }

        bool IsInMeleeRange()
        {
            return EntityHelper.DistanceToEntity(this, TargetEntity) <= _sweepAttackRange;
        }

        bool IsPlayerTooFar()
        {
            var dist = EntityHelper.DistanceToEntity(this, TargetEntity);
            return dist > _attackRange;
        }

        bool IsPlayerTooClose()
        {
            return EntityHelper.DistanceToEntity(this, TargetEntity) < _minDistanceToPlayer;
        }

        bool IsOnCooldown()
        {
            return _isOnCooldown;
        }

        #region TASKS

        TaskStatus StartAttackTimer()
        {
            _attackPrepTimer = 0f;
            return TaskStatus.Success;
        }

        TaskStatus StartCooldownTimer()
        {
            _isOnCooldown = true;
            _cooldownTimer = Game1.Schedule(_attackCooldown, timer =>
            {
                _isOnCooldown = false;
            });

            return TaskStatus.Success;
        }

        TaskStatus WaitToAttack()
        {
            //increment timer
            _attackPrepTimer += Time.DeltaTime;

            //if timer finished, reset timer and return success
            if (_attackPrepTimer >= _attackPrepTime)
            {
                _attackPrepTimer = 0;
                return TaskStatus.Success;
            }

            //get distance and direction to player
            var distToPlayer = EntityHelper.DistanceToEntity(this, TargetEntity);

            //slowly move towards player if not at preferred distance
            if (distToPlayer > _preferredDistanceToPlayer)
            {
                MoveToTarget(TargetEntity, _speed);
            }
            else
            {
                return Idle();
            }

            return TaskStatus.Running;
        }

        #endregion
    }
}
