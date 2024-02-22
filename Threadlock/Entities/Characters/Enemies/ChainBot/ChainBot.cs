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

namespace Threadlock.Entities.Characters.Enemies.ChainBot
{
    public class ChainBot : Enemy<ChainBot>
    {
        //property overrides
        const float _speed = 75f;

        //components
        SpriteAnimator _animator;
        Mover _mover;
        HealthComponent _healthComponent;
        VelocityComponent _velocityComponent;
        KnockbackComponent _knockbackComponent;
        Hurtbox _hurtbox;
        BoxCollider _collider;
        SpriteFlipper _flipper;
        DeathComponent _deathComponent;
        Pathfinder _pathfinder;
        OriginComponent _originComponent;

        //actions
        ChainBotMelee _chainBotMelee;

        #region LIFECYCLE

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            _animator = AddComponent(new SpriteAnimator());
            _animator.SetRenderLayer(RenderLayers.YSort);
            AddAnimations();

            _mover = AddComponent(new Mover());

            _healthComponent = AddComponent(new HealthComponent(12, 12));

            _velocityComponent = AddComponent(new VelocityComponent(_mover));

            _knockbackComponent = AddComponent(new KnockbackComponent(_velocityComponent, 150, .5f));

            var hurtboxCollider = AddComponent(new BoxCollider(8, 18));
            hurtboxCollider.IsTrigger = true;
            Flags.SetFlagExclusive(ref hurtboxCollider.PhysicsLayer, PhysicsLayers.EnemyHurtbox);
            Flags.SetFlagExclusive(ref hurtboxCollider.CollidesWithLayers, PhysicsLayers.PlayerHitbox);
            _hurtbox = AddComponent(new Hurtbox(hurtboxCollider, 0f, Nez.Content.Audio.Sounds.Chain_bot_damaged));

            _collider = AddComponent(new BoxCollider(10, 5));
            _collider.SetLocalOffset(new Vector2(-1, 7));
            Flags.SetFlagExclusive(ref _collider.PhysicsLayer, PhysicsLayers.EnemyCollider);
            _collider.CollidesWithLayers = 0;
            Flags.SetFlag(ref _collider.CollidesWithLayers, PhysicsLayers.Environment);
            Flags.SetFlag(ref _collider.CollidesWithLayers, PhysicsLayers.EnemyCollider);

            _flipper = AddComponent(new SpriteFlipper());

            _deathComponent = AddComponent(new DeathComponent("Die", Nez.Content.Audio.Sounds.Enemy_death_1));

            _pathfinder = AddComponent(new Pathfinder(_collider));

            _originComponent = AddComponent(new OriginComponent(_collider));

            _chainBotMelee = AddComponent(new ChainBotMelee(this));
        }

        #endregion

        #region ENEMY OVERRIDES

        public override BehaviorTree<ChainBot> CreateSubTree()
        {
            var tree = BehaviorTreeBuilder<ChainBot>.Begin(this)
                .Sequence() //combat sequence
                    .Selector() //move or attack selector
                        .Sequence(AbortTypes.LowerPriority)
                            .Conditional(c => c.IsInAttackRange())
                            .Action(c => c.ExecuteAction(_chainBotMelee))
                        .EndComposite()
                        .Sequence(AbortTypes.LowerPriority)
                            .Action(c => c.MoveToTarget(TargetEntity, _speed))
                        .EndComposite()
                    .EndComposite()
                .EndComposite()
            .Build();

            tree.UpdatePeriod = 0;
            return tree;
        }

        #endregion

        bool IsInAttackRange()
        {
            var targetPos = TargetEntity.Position;
            var xDist = Math.Abs(Position.X - targetPos.X);
            var yDist = Math.Abs(Position.Y - targetPos.Y);
            if (xDist <= 16 && yDist <= 8)
            {
                return true;
            }
            return false;
        }

        void AddAnimations()
        {
            //Idle
            var idleTexture = Scene.Content.LoadTexture(Content.Textures.Characters.ChainBot.Idle);
            var idleSprites = Sprite.SpritesFromAtlas(idleTexture, 126, 39);
            _animator.AddAnimation("IdleLeft", AnimatedSpriteHelper.GetSpriteArray(idleSprites, new List<int> { 1, 3, 5, 7, 9 }));
            _animator.AddAnimation("Idle", AnimatedSpriteHelper.GetSpriteArray(idleSprites, new List<int> { 0, 2, 4, 6, 8 }));

            //Run
            var runTexture = Scene.Content.LoadTexture(Content.Textures.Characters.ChainBot.Run);
            var runSprites = Sprite.SpritesFromAtlas(runTexture, 126, 39);
            var leftSprites = runSprites.Where((sprite, index) => index % 2 != 0);
            var rightSprites = runSprites.Where((sprite, index) => index % 2 == 0);
            _animator.AddAnimation("RunLeft", leftSprites.ToArray());
            _animator.AddAnimation("Run", rightSprites.ToArray());

            //Attack
            var attackTexture = Scene.Content.LoadTexture(Content.Textures.Characters.ChainBot.Attack);
            var attackSprites = Sprite.SpritesFromAtlas(attackTexture, 126, 39);
            _animator.AddAnimation("AttackLeft", attackSprites.Where((sprite, index) => index % 2 != 0).ToArray());
            _animator.AddAnimation("AttackRight", attackSprites.Where((sprite, index) => index % 2 == 0).ToArray());

            //transition to charge
            var transitionTexture = Scene.Content.LoadTexture(Content.Textures.Characters.ChainBot.Transitiontocharge);
            var transitionSprites = Sprite.SpritesFromAtlas(transitionTexture, 126, 39);
            _animator.AddAnimation("TransitionLeft", transitionSprites.Where((sprite, index) => index % 2 != 0).ToArray());
            _animator.AddAnimation("TransitionRight", transitionSprites.Where((sprite, index) => index % 2 == 0).ToArray());

            //charge
            var chargeTexture = Scene.Content.LoadTexture(Content.Textures.Characters.ChainBot.Charge);
            var chargeSprites = Sprite.SpritesFromAtlas(chargeTexture, 126, 39);
            _animator.AddAnimation("ChargeLeft", chargeSprites.Where((sprite, index) => index % 2 != 0).ToArray());
            _animator.AddAnimation("ChargeRight", chargeSprites.Where((sprite, index) => index % 2 == 0).ToArray());

            //hit
            var hitTexture = Scene.Content.LoadTexture(Content.Textures.Characters.ChainBot.Hit);
            var hitSprites = Sprite.SpritesFromAtlas(hitTexture, 126, 39);
            _animator.AddAnimation("HurtLeft", AnimatedSpriteHelper.GetSpriteArray(hitSprites, new List<int>() { 1, 3 }));
            _animator.AddAnimation("Hit", AnimatedSpriteHelper.GetSpriteArray(hitSprites, new List<int>() { 0, 2 }));

            //die
            var dieTexture = Scene.Content.LoadTexture(Content.Textures.Characters.ChainBot.Death);
            var dieSprites = Sprite.SpritesFromAtlas(dieTexture, 126, 39);
            _animator.AddAnimation("Die", AnimatedSpriteHelper.GetSpriteArrayFromRange(dieSprites, 0, 4));
        }
    }
}
