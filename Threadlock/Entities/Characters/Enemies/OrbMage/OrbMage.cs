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
        const float _speed = 25f;
        const float _minDistanceToPlayer = 48f;
        const float _attackRange = 192f;
        const float _preferredDistanceToPlayer = 80f;
        const float _sweepAttackRange = 64f;
        const float _attackPrepTime = 3f;

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
            _animator.AddAnimation("Death", deathSprites.ToArray());
        }

        #endregion

        #region OVERRIDES

        public override BehaviorTree<OrbMage> CreateSubTree()
        {
            var tree = BehaviorTreeBuilder<OrbMage>.Begin(this)
                .Sequence()
                    .Action(o => o.StartAttackTimer())
                    //pre attack
                    .Selector(AbortTypes.Self)
                        //if player too close, move away from player
                        .ConditionalDecorator(o => EntityHelper.DistanceToEntity(o, o.GetTarget()) <= _minDistanceToPlayer, true)
                            .Sequence()
                                .Action(o => o.MoveAway(o.GetTarget(), _speed))
                            .EndComposite()

                        //if not in attack range, move towards player
                        .ConditionalDecorator(o => EntityHelper.DistanceToEntity(o, o.GetTarget()) > _attackRange, true)
                            .Sequence()
                                .Action(o => o.MoveToTarget(o.GetTarget(), _speed))
                            .EndComposite()
                        
                        //if in attack range, increment attack timer
                        .ConditionalDecorator(o => EntityHelper.DistanceToEntity(o, o.GetTarget()) <= _attackRange, true)
                            .Sequence()
                                .Action(o => o.WaitToAttack())
                            .EndComposite()
                    .EndComposite()
                    //select and perform attaack
                    .Selector()
                        .Sequence()
                            .Conditional(o => EntityHelper.DistanceToEntity(this, o.GetTarget()) < _sweepAttackRange)
                            .Action(o => o.ExecuteAction(_orbMageSweepAttack))
                        .EndComposite()
                        .Sequence()
                            .Action(o => o.ExecuteAction(_orbMageAttack))
                        .EndComposite()
                    .EndComposite()
                    .Sequence()
                        .ParallelSelector()
                            .Action(o => o.Idle())
                            .WaitAction(1f)
                        .EndComposite()
                    .EndComposite()
                .EndComposite()
            .Build();

            tree.UpdatePeriod = 0;
            return tree;
        }

        #endregion

        #region TASKS

        TaskStatus StartAttackTimer()
        {
            _attackPrepTimer = 0f;
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
            var distToPlayer = EntityHelper.DistanceToEntity(this, GetTarget());

            //slowly move towards player if not at preferred distance
            if (distToPlayer > _preferredDistanceToPlayer)
            {
                MoveToTarget(GetTarget(), _speed);
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
