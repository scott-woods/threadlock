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
        //actions
        ChainBotMelee _chainBotMelee;

        #region ENEMY OVERRIDES

        public override int MaxHealth => 10;

        public override float BaseSpeed => 110f;

        public override Vector2 HurtboxSize => new Vector2(8, 18);

        public override Vector2 ColliderSize => new Vector2(10, 5);

        public override Vector2 ColliderOffset => new Vector2(-1, 7);

        public override void Setup()
        {
            SetupBasicEnemy(this);
            AddAnimations();

            _chainBotMelee = AddComponent(new ChainBotMelee(this));
        }

        public override BehaviorTree<ChainBot> CreateSubTree()
        {
            var tree = BehaviorTreeBuilder<ChainBot>.Begin(this)
                .Sequence() //combat sequence
                    .Selector() //move or attack selector
                        .Sequence(AbortTypes.LowerPriority)
                            .Conditional(c => c.IsInAttackRange())
                            .Action(c => c.ExecuteAction(_chainBotMelee))
                            .ParallelSelector()
                                .Action(c => c.Idle())
                                .Action(c => c.TrackTarget(TargetEntity))
                                .WaitAction(2f)
                            .EndComposite()
                        .EndComposite()
                        .Sequence(AbortTypes.LowerPriority)
                            .Action(c => c.MoveToTarget(TargetEntity, BaseSpeed))
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
            if (xDist <= 32 && yDist <= 8)
            {
                return true;
            }
            return false;
        }

        void AddAnimations()
        {
            if (TryGetComponent<SpriteAnimator>(out var animator))
            {
                //Idle
                var idleTexture = Scene.Content.LoadTexture(Content.Textures.Characters.ChainBot.Idle);
                var idleSprites = Sprite.SpritesFromAtlas(idleTexture, 126, 39);
                animator.AddAnimation("IdleLeft", AnimatedSpriteHelper.GetSpriteArray(idleSprites, new List<int> { 1, 3, 5, 7, 9 }));
                animator.AddAnimation("Idle", AnimatedSpriteHelper.GetSpriteArray(idleSprites, new List<int> { 0, 2, 4, 6, 8 }));

                //Run
                var runTexture = Scene.Content.LoadTexture(Content.Textures.Characters.ChainBot.Run);
                var runSprites = Sprite.SpritesFromAtlas(runTexture, 126, 39);
                var leftSprites = runSprites.Where((sprite, index) => index % 2 != 0);
                var rightSprites = runSprites.Where((sprite, index) => index % 2 == 0);
                animator.AddAnimation("RunLeft", leftSprites.ToArray());
                animator.AddAnimation("Run", rightSprites.ToArray());

                //Attack
                var attackTexture = Scene.Content.LoadTexture(Content.Textures.Characters.ChainBot.Attack);
                var attackSprites = Sprite.SpritesFromAtlas(attackTexture, 126, 39);
                animator.AddAnimation("AttackLeft", attackSprites.Where((sprite, index) => index % 2 != 0).ToArray());
                animator.AddAnimation("AttackRight", attackSprites.Where((sprite, index) => index % 2 == 0).ToArray());

                //transition to charge
                var transitionTexture = Scene.Content.LoadTexture(Content.Textures.Characters.ChainBot.Transitiontocharge);
                var transitionSprites = Sprite.SpritesFromAtlas(transitionTexture, 126, 39);
                animator.AddAnimation("TransitionLeft", transitionSprites.Where((sprite, index) => index % 2 != 0).ToArray());
                animator.AddAnimation("TransitionRight", transitionSprites.Where((sprite, index) => index % 2 == 0).ToArray());

                //charge
                var chargeTexture = Scene.Content.LoadTexture(Content.Textures.Characters.ChainBot.Charge);
                var chargeSprites = Sprite.SpritesFromAtlas(chargeTexture, 126, 39);
                animator.AddAnimation("ChargeLeft", chargeSprites.Where((sprite, index) => index % 2 != 0).ToArray());
                animator.AddAnimation("ChargeRight", chargeSprites.Where((sprite, index) => index % 2 == 0).ToArray());

                //hit
                var hitTexture = Scene.Content.LoadTexture(Content.Textures.Characters.ChainBot.Hit);
                var hitSprites = Sprite.SpritesFromAtlas(hitTexture, 126, 39);
                animator.AddAnimation("HurtLeft", AnimatedSpriteHelper.GetSpriteArray(hitSprites, new List<int>() { 1, 3 }));
                animator.AddAnimation("Hit", AnimatedSpriteHelper.GetSpriteArray(hitSprites, new List<int>() { 0, 2 }));

                //die
                var dieTexture = Scene.Content.LoadTexture(Content.Textures.Characters.ChainBot.Death);
                var dieSprites = Sprite.SpritesFromAtlas(dieTexture, 126, 39);
                animator.AddAnimation("Die", AnimatedSpriteHelper.GetSpriteArrayFromRange(dieSprites, 0, 4));
            }
        }
    }
}
