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
        //constants
        float _speed = 165f;

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
        GhoulAttack _ghoulAttack;

        #region LIFECYCLE

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            //Mover
            _mover = AddComponent(new Mover());

            //hurtbox
            var hurtboxCollider = AddComponent(new BoxCollider(10, 17));
            hurtboxCollider.IsTrigger = true;
            Flags.SetFlagExclusive(ref hurtboxCollider.PhysicsLayer, (int)PhysicsLayers.EnemyHurtbox);
            Flags.SetFlagExclusive(ref hurtboxCollider.CollidesWithLayers, (int)PhysicsLayers.PlayerHitbox);
            _hurtbox = AddComponent(new Hurtbox(hurtboxCollider, 0, Content.Audio.Sounds.Chain_bot_damaged));

            //health
            _healthComponent = AddComponent(new HealthComponent(9, 9));

            //velocity
            _velocityComponent = AddComponent(new VelocityComponent(_mover));

            //collider
            _collider = AddComponent(new BoxCollider(9, 6));
            _collider.SetLocalOffset(new Vector2(0, 9));
            Flags.SetFlagExclusive(ref _collider.PhysicsLayer, (int)PhysicsLayers.EnemyCollider);
            _collider.CollidesWithLayers = 0;
            Flags.SetFlag(ref _collider.CollidesWithLayers, (int)PhysicsLayers.Environment);
            Flags.SetFlag(ref _collider.CollidesWithLayers, (int)PhysicsLayers.EnemyCollider);

            //origin
            _originComponent = AddComponent(new OriginComponent(_collider));

            //pathfinding
            _pathfinder = AddComponent(new Pathfinder(_collider));

            //animator
            _animator = AddComponent(new SpriteAnimator());
            _animator.SetRenderLayer(RenderLayers.YSort);
            _animator.Speed = 1.5f;
            AddAnimations();

            //sprite flipper
            _spriteFlipper = AddComponent(new SpriteFlipper());

            //death
            _deathComponent = AddComponent(new DeathComponent("Die", Content.Audio.Sounds.Enemy_death_1));

            //knockback
            _knockbackComponent = AddComponent(new KnockbackComponent(_velocityComponent, 150f));

            //actions
            _ghoulAttack = AddComponent(new GhoulAttack(this));

            AddComponent(new LootDropper(LootTables.BasicEnemy));
        }

        #endregion

        #region SETUP

        void AddAnimations()
        {
            var texture = Scene.Content.LoadTexture(Content.Textures.Characters.Ghoul.Ghoul_sprites);
            var sprites = Sprite.SpritesFromAtlas(texture, 62, 33);
            _animator.AddAnimation("Idle", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 0, 4, 11));
            _animator.AddAnimation("Run", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 1, 9, 11));
            _animator.AddAnimation("Attack", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 2, 7, 11));
            _animator.AddAnimation("Hit", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 3, 2, 11));
            _animator.AddAnimation("Die", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 4, 8, 11));
            _animator.AddAnimation("Spawn", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 5, 11, 11));
        }

        #endregion

        #region ENEMY OVERRIDES

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
                            .Action(c => c.MoveToTarget(TargetEntity, _speed))
                        .EndComposite()
                    .EndComposite()
                .EndComposite()
                .Build();

            tree.UpdatePeriod = 0f;

            return tree;
        }

        #endregion

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
