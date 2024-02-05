using Microsoft.Xna.Framework;
using Nez;
using Nez.AI.FSM;
using Nez.Sprites;
using Nez.Textures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Entities.Characters.Player.BasicWeapons;
using Threadlock.Entities.Characters.Player.PlayerActions;
using Threadlock.Entities.Characters.Player.States;
using Threadlock.Helpers;
using Threadlock.SaveData;
using Threadlock.StaticData;

namespace Threadlock.Entities.Characters.Player
{
    public class Player : Entity
    {
        public static Player Instance { get; private set; }

        public float MoveSpeed = 150f;
        public Vector2 SpriteOffset = new Vector2(13, -2);

        //state machine
        public StateMachine<Player> StateMachine { get; set; }

        //components
        Mover _mover;
        SpriteAnimator _animator;
        SpriteTrail _spriteTrail;
        VelocityComponent _velocityComponent;
        SpriteFlipper _spriteFlipper;
        SwordAttack _swordAttack;
        Dash _dash;
        BoxCollider _collider;
        public BoxCollider Collider { get => _collider; }
        Hurtbox _hurtbox;
        KnockbackComponent _knockbackComponent;
        StatusComponent _statusComponent;
        HealthComponent _healthComponent;
        DeathComponent _deathComponent;
        OriginComponent _originComponent;
        ApComponent _apComponent;

        //actions
        public PlayerAction OffensiveAction1;
        public PlayerAction OffensiveAction2;
        public PlayerAction SupportAction;

        public Player()
        {
            Instance = this;
        }

        #region LIFECYCLE

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            //add components
            _mover = AddComponent(new Mover());
            _animator = AddComponent(new SpriteAnimator());
            _animator.SetLocalOffset(SpriteOffset);
            _velocityComponent = AddComponent(new VelocityComponent(_mover));
            _spriteFlipper = AddComponent(new SpriteFlipper());
            _swordAttack = AddComponent(new SwordAttack());
            _dash = AddComponent(new Dash(1));
            _spriteTrail = AddComponent(new SpriteTrail());
            _spriteTrail.DisableSpriteTrail();

            //collider
            _collider = AddComponent(new BoxCollider(-4, 4, 8, 5));
            Flags.SetFlagExclusive(ref _collider.PhysicsLayer, PhysicsLayers.PlayerCollider);
            _collider.CollidesWithLayers = 0;
            Flags.SetFlag(ref _collider.CollidesWithLayers, PhysicsLayers.Environment);

            //hurtbox
            var hurtboxCollider = AddComponent(new BoxCollider(9, 16));
            hurtboxCollider.IsTrigger = true;
            Flags.SetFlagExclusive(ref hurtboxCollider.PhysicsLayer, PhysicsLayers.PlayerHurtbox);
            Flags.SetFlagExclusive(ref hurtboxCollider.CollidesWithLayers, PhysicsLayers.EnemyHitbox);
            _hurtbox = AddComponent(new Hurtbox(hurtboxCollider, 1f, Nez.Content.Audio.Sounds._64_Get_hit_03));

            _knockbackComponent = AddComponent(new KnockbackComponent(_velocityComponent, 110, .5f));

            _statusComponent = AddComponent(new StatusComponent(StatusPriority.Normal));

            _healthComponent = AddComponent(new HealthComponent(10, 10));

            _deathComponent = AddComponent(new DeathComponent("Die", Nez.Content.Audio.Sounds._69_Die_02));

            _originComponent = AddComponent(new OriginComponent(_collider));

            _apComponent = AddComponent(new ApComponent(5));

            //actions
            OffensiveAction1 = AddComponent(Activator.CreateInstance(PlayerData.Instance.OffensiveAction1.ToType()) as PlayerAction);
            SupportAction = AddComponent(Activator.CreateInstance(PlayerData.Instance.SupportAction.ToType()) as PlayerAction);

            //add animations
            AddAnimations();

            //init state machine
            StateMachine = new StateMachine<Player>(this, new Idle());
            StateMachine.AddState(new Move());
            StateMachine.AddState(new BasicAttackState());
            StateMachine.AddState(new PreparingActionState());
            StateMachine.AddState(new ExecutingActionState());
            StateMachine.AddState(new DashState());
            StateMachine.AddState(new StunnedState());
        }

        public override void Update()
        {
            base.Update();

            StateMachine.Update(Time.DeltaTime);
        }

        #endregion

        void AddAnimations()
        {
            var texture = Core.Content.LoadTexture(Content.Textures.Characters.Player.Sci_fi_player_with_sword);
            var noSwordTexture = Core.Content.LoadTexture(Content.Textures.Characters.Player.Sci_fi_player_no_sword);
            var sprites = Sprite.SpritesFromAtlas(texture, 64, 65);
            var noSwordSprites = Sprite.SpritesFromAtlas(noSwordTexture, 64, 65);

            var totalCols = 13;
            var slashFps = 15;
            var thrustFps = 15;
            var rollFps = 20;

            //down
            _animator.AddAnimation($"IdleDown", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 0, 12, totalCols));
            _animator.AddAnimation($"IdleDownNoSword", AnimatedSpriteHelper.GetSpriteArrayByRow(noSwordSprites, 0, 12, totalCols));
            _animator.AddAnimation($"WalkDown", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 1, 8, totalCols));
            _animator.AddAnimation($"WalkDownNoSword", AnimatedSpriteHelper.GetSpriteArrayByRow(noSwordSprites, 1, 8, totalCols));
            _animator.AddAnimation($"RunDown", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 2, 8, totalCols));
            _animator.AddAnimation($"RunDownNoSword", AnimatedSpriteHelper.GetSpriteArrayByRow(noSwordSprites, 2, 8, totalCols));
            _animator.AddAnimation($"ThrustDown", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 3, 4, totalCols), thrustFps);
            _animator.AddAnimation($"SlashDown", AnimatedSpriteHelper.GetSpriteArrayFromRange(sprites, 54, 58), slashFps);
            _animator.AddAnimation($"RollDown", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 5, 8, totalCols), rollFps);
            _animator.AddAnimation($"RollDownNoSword", AnimatedSpriteHelper.GetSpriteArrayByRow(noSwordSprites, 3, 8, totalCols), rollFps);

            //side
            _animator.AddAnimation($"Idle", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 6, 12, totalCols));
            _animator.AddAnimation($"IdleNoSword", AnimatedSpriteHelper.GetSpriteArrayByRow(noSwordSprites, 4, 12, totalCols));
            _animator.AddAnimation($"Walk", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 7, 8, totalCols));
            _animator.AddAnimation($"WalkNoSword", AnimatedSpriteHelper.GetSpriteArrayByRow(noSwordSprites, 5, 8, totalCols));
            _animator.AddAnimation($"Run", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 8, 8, totalCols));
            _animator.AddAnimation($"RunNoSword", AnimatedSpriteHelper.GetSpriteArrayByRow(noSwordSprites, 6, 8, totalCols));
            _animator.AddAnimation($"Roll", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 9, 6, totalCols), rollFps);
            _animator.AddAnimation($"RollNoSword", AnimatedSpriteHelper.GetSpriteArrayByRow(noSwordSprites, 7, 6, totalCols), rollFps);
            _animator.AddAnimation($"Thrust", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 10, 4, totalCols), thrustFps);
            _animator.AddAnimation($"Slash", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 11, 5, totalCols), slashFps);

            //up
            _animator.AddAnimation($"IdleUp", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 12, 12, totalCols));
            _animator.AddAnimation($"IdleUpNoSword", AnimatedSpriteHelper.GetSpriteArrayByRow(noSwordSprites, 8, 12, totalCols));
            _animator.AddAnimation($"WalkUp", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 13, 8, totalCols));
            _animator.AddAnimation($"WalkUpNoSword", AnimatedSpriteHelper.GetSpriteArrayByRow(noSwordSprites, 9, 8, totalCols));
            _animator.AddAnimation($"RunUp", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 14, 8, totalCols));
            _animator.AddAnimation($"RunUpNoSword", AnimatedSpriteHelper.GetSpriteArrayByRow(noSwordSprites, 10, 8, totalCols));
            _animator.AddAnimation($"ThrustUp", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 15, 4, totalCols), thrustFps);
            _animator.AddAnimation($"SlashUp", AnimatedSpriteHelper.GetSpriteArrayFromRange(sprites, 210, 214), slashFps);
            _animator.AddAnimation($"RollUp", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 17, 8, totalCols), rollFps);
            _animator.AddAnimation($"RollUpNoSword", AnimatedSpriteHelper.GetSpriteArrayByRow(noSwordSprites, 11, 8, totalCols), rollFps);

            //death
            _animator.AddAnimation($"Die", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 18, 13, totalCols));
            _animator.AddAnimation($"DieNoSword", AnimatedSpriteHelper.GetSpriteArrayByRow(noSwordSprites, 12, 13, totalCols));
        }

        

        public Vector2 GetFacingDirection()
        {
            var dir = Scene.Camera.MouseToWorldPoint() - Position;
            dir.Normalize();
            return dir;
        }
    }
}
