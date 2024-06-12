using Microsoft.Xna.Framework;
using Nez;
using Nez.AI.FSM;
using Nez.DeferredLighting;
using Nez.Persistence;
using Nez.Sprites;
using Nez.Textures;
using Nez.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Threadlock.Components;
using Threadlock.Entities.Characters.Player.BasicWeapons;
using Threadlock.Entities.Characters.Player.States;
using Threadlock.GlobalManagers;
using Threadlock.Helpers;
using Threadlock.Models;
using Threadlock.SaveData;
using Threadlock.StaticData;

namespace Threadlock.Entities.Characters.Player
{
    public class Player : Entity
    {
        const float _checkRadius = 20f;

        const string _idleAnimation = "Player_Idle";

        public static Player Instance { get; private set; }

        public float MoveSpeed = 135f;
        public Vector2 DefaultSpriteOffset = new Vector2(13, -2);

        //state machine
        public StateMachine<Player> StateMachine { get; set; }
        PlayerState _initialState = new Idle();

        public event Action<BasicWeapon> OnWeaponChanged;

        //components
        Mover _mover;
        SpriteAnimator _animator;
        SpriteTrail _spriteTrail;
        VelocityComponent _velocityComponent;
        SpriteFlipper _spriteFlipper;
        BasicWeapon _basicWeapon;
        Dash _dash;
        BoxCollider _collider;
        Shadow _shadow;
        public BoxCollider Collider { get => _collider; }
        Hurtbox _hurtbox;
        KnockbackComponent _knockbackComponent;
        StatusComponent _statusComponent;
        HealthComponent _healthComponent;
        DeathComponent _deathComponent;
        OriginComponent _originComponent;
        ApComponent _apComponent;
        ActionManager _actionManager;

        public Player() : base("Player")
        {
            Instance = this;

            SetTag(EntityTags.EnemyTarget);

            //add components
            _mover = AddComponent(new Mover());
            AddComponent(new ProjectileMover());
            _animator = AddComponent(new SpriteAnimator());
            _animator.SetLocalOffset(DefaultSpriteOffset);
            _animator.SetRenderLayer(RenderLayers.YSort);
            _velocityComponent = AddComponent(new VelocityComponent());
            _spriteFlipper = AddComponent(new SpriteFlipper());
            _basicWeapon = AddComponent(new LuteHammer());
            _dash = AddComponent(new Dash(1));
            _spriteTrail = AddComponent(new SpriteTrail());
            _spriteTrail.DisableSpriteTrail();

            //collider
            _collider = AddComponent(new BoxCollider(-4, 4, 8, 5));
            Flags.SetFlagExclusive(ref _collider.PhysicsLayer, PhysicsLayers.PlayerCollider);
            _collider.CollidesWithLayers = 0;
            Flags.SetFlag(ref _collider.CollidesWithLayers, PhysicsLayers.Environment);
            Flags.SetFlag(ref _collider.CollidesWithLayers, PhysicsLayers.ProjectilePassableWall);
            Flags.SetFlag(ref _collider.CollidesWithLayers, PhysicsLayers.PromptTrigger);
            Flags.SetFlag(ref _collider.CollidesWithLayers, PhysicsLayers.AreaTrigger);

            //hurtbox
            var hurtboxCollider = AddComponent(new BoxCollider(9, 16));
            hurtboxCollider.IsTrigger = true;
            Flags.SetFlagExclusive(ref hurtboxCollider.PhysicsLayer, PhysicsLayers.PlayerHurtbox);
            hurtboxCollider.CollidesWithLayers = 0;
            Flags.SetFlag(ref hurtboxCollider.CollidesWithLayers, PhysicsLayers.PromptTrigger);
            Flags.SetFlag(ref hurtboxCollider.CollidesWithLayers, PhysicsLayers.EnemyHitbox);
            //Flags.SetFlagExclusive(ref hurtboxCollider.CollidesWithLayers, PhysicsLayers.EnemyHitbox);
            _hurtbox = AddComponent(new Hurtbox(hurtboxCollider, 2f, Nez.Content.Audio.Sounds._64_Get_hit_03));

            _knockbackComponent = AddComponent(new KnockbackComponent(_velocityComponent, "Player_Hit", 110, .5f));

            _statusComponent = AddComponent(new StatusComponent(StatusPriority.Normal));

            _healthComponent = AddComponent(new HealthComponent(10, 10));

            _deathComponent = AddComponent(new DeathComponent("Player_Death", Nez.Content.Audio.Sounds._69_Die_02, false));

            _originComponent = AddComponent(new OriginComponent(_collider));

            _apComponent = AddComponent(new ApComponent(5));

            //actions
            _actionManager = AddComponent(new ActionManager());

            //add animations
            AddAnimations();

            //init state machine
            StateMachine = new StateMachine<Player>(this, _initialState);
            var assembly = Assembly.GetExecutingAssembly();
            var stateTypes = assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(PlayerState)) && !t.IsAbstract && t != typeof(Idle));
            foreach (var type in stateTypes)
                StateMachine.AddState((PlayerState)Activator.CreateInstance(type));

            Game1.SceneManager.Emitter.AddObserver(SceneManagerEvents.SceneChangeStarted, OnSceneChangeStarted);
            Game1.Emitter.AddObserver(CoreEvents.SceneChanged, OnSceneChanged);
            _deathComponent.Emitter.AddObserver(DeathEventTypes.Finished, OnDeath);

            //_shadow = AddComponent(new Shadow(_animator));

            var pointLight = AddComponent(new PointLight(Color.White));
            pointLight.SetRenderLayer(RenderLayers.Light);
            pointLight.SetIntensity(.5f);
            pointLight.SetRadius(100f);
        }

        #region LIFECYCLE

        public override void OnRemovedFromScene()
        {
            base.OnRemovedFromScene();

            if (IsDestroyed)
            {
                Game1.SceneManager.Emitter.RemoveObserver(SceneManagerEvents.SceneChangeStarted, OnSceneChangeStarted);
                Game1.Emitter.RemoveObserver(CoreEvents.SceneChanged, OnSceneChanged);
                _deathComponent.Emitter.RemoveObserver(DeathEventTypes.Finished, OnDeath);
            }
        }

        public override void Update()
        {
            base.Update();

            if (Game1.GameStateManager.GameState != GameState.Paused)
                StateMachine.Update(Time.DeltaTime);
        }

        #endregion

        public void Run()
        {
            var dir = Controls.Instance.DirectionalInput.Value;
            dir.Normalize();

            AnimatedSpriteHelper.PlayAnimation(ref _animator, "Player_Run");

            _velocityComponent.Move(dir, MoveSpeed);
        }

        public void Idle()
        {
            AnimatedSpriteHelper.PlayAnimation(ref _animator, "Player_Idle");

            //_velocityComponent.Direction = Vector2.Zero;
        }

        public void IdleInFacingDirection()
        {
            var dir = GetFacingDirection();

            string dirString;
            if (dir.Y < 0 && Math.Abs(dir.X) < .1f)
                dirString = "Up";
            else if (dir.Y > 0 && Math.Abs(dir.X) < .1f)
                dirString = "Down";
            else
                dirString = "Right";

            AnimatedSpriteHelper.PlayAnimation(ref _animator, $"Player_Idle_{dirString}");

            _velocityComponent.LastNonZeroDirection = dir;
        }

        public BasicWeapon EquipNewWeapon<T>() where T : BasicWeapon, new()
        {
            var weapon = new T();
            return EquipNewWeapon(weapon);
        }

        public BasicWeapon EquipNewWeapon<T>(T weapon) where T : BasicWeapon
        {
            if (_basicWeapon != null)
            {
                _basicWeapon.OnUnequipped();
                RemoveComponent(_basicWeapon);
            }

            _basicWeapon = AddComponent(weapon);

            OnWeaponChanged?.Invoke(_basicWeapon);

            return _basicWeapon;
        }

        public BasicWeapon GetCurrentWeapon()
        {
            return _basicWeapon;
        }

        void OnDeath(Entity entity)
        {
            Game1.GameStateManager.HandlePlayerDeath();
        }

        void OnSceneChangeStarted()
        {
            DetachFromScene();
            SetEnabled(false);
        }

        void OnSceneChanged()
        {
            AttachToScene(Game1.Scene);
        }

        void AddAnimations()
        {
            AnimatedSpriteHelper.LoadAnimationsGlobal(ref _animator, "Player_Idle", "Player_Walk", "Player_Run", "Player_Hit", "Player_Death");
            var json = File.ReadAllText("Content/Textures/Characters/Player/PlayerMainConfig.json");
            var export = Json.FromJson<AsepriteExport>(json);
            var texture = Game1.Content.LoadTexture($"Content/Textures/Characters/Player/{export.Meta.Image}");

            foreach (var tag in export.Meta.FrameTags)
            {
                var sprites = new List<Sprite>();
                var currentFrame = tag.From - 1;
                while (currentFrame < tag.To)
                {
                    var frameData = export.Frames[currentFrame];
                    var sprite = new Sprite(texture, frameData.Frame.ToRectangle());
                    sprites.Add(sprite);
                    currentFrame++;
                }

                var fps = 10;
                if (tag.Name.StartsWith("Roll"))
                    fps = 20;
                if (tag.Name.StartsWith("Thrust"))
                    fps = 15;
                if (tag.Name.StartsWith("Slash"))
                    fps = 21;
                _animator.AddAnimation(tag.Name, sprites.ToArray(), fps);
            }

            //var textureWithSword = Core.Content.LoadTexture(Content.Textures.Characters.Player.Sci_fi_player_with_sword);
            //var noSwordTexture = Core.Content.LoadTexture(Content.Textures.Characters.Player.Sci_fi_player_no_sword);

            //var spritesWithSword = Sprite.SpritesFromAtlas(textureWithSword, 64, 65);
            //var noSwordSprites = Sprite.SpritesFromAtlas(noSwordTexture, 64, 65);

            //var totalCols = 13;
            //var slashFps = 15;
            //var thrustFps = 15;
            //var rollFps = 20;

            ////down
            //_animator.AddAnimation($"IdleDown", AnimatedSpriteHelper.GetSpriteArrayByRow(spritesWithSword, 0, 12, totalCols));
            //_animator.AddAnimation($"IdleDownNoSword", AnimatedSpriteHelper.GetSpriteArrayByRow(noSwordSprites, 0, 12, totalCols));
            //_animator.AddAnimation($"WalkDown", AnimatedSpriteHelper.GetSpriteArrayByRow(spritesWithSword, 1, 8, totalCols));
            //_animator.AddAnimation($"WalkDownNoSword", AnimatedSpriteHelper.GetSpriteArrayByRow(noSwordSprites, 1, 8, totalCols));
            //_animator.AddAnimation($"RunDown", AnimatedSpriteHelper.GetSpriteArrayByRow(spritesWithSword, 2, 8, totalCols));
            //_animator.AddAnimation($"RunDownNoSword", AnimatedSpriteHelper.GetSpriteArrayByRow(noSwordSprites, 2, 8, totalCols));
            //_animator.AddAnimation($"ThrustDown", AnimatedSpriteHelper.GetSpriteArrayByRow(spritesWithSword, 3, 4, totalCols), thrustFps);
            //_animator.AddAnimation($"SlashDown", AnimatedSpriteHelper.GetSpriteArrayFromRange(spritesWithSword, 54, 58), slashFps);
            //_animator.AddAnimation($"RollDown", AnimatedSpriteHelper.GetSpriteArrayByRow(spritesWithSword, 5, 8, totalCols), rollFps);
            //_animator.AddAnimation($"RollDownNoSword", AnimatedSpriteHelper.GetSpriteArrayByRow(noSwordSprites, 3, 8, totalCols), rollFps);

            ////side
            //_animator.AddAnimation($"Idle", AnimatedSpriteHelper.GetSpriteArrayByRow(spritesWithSword, 6, 12, totalCols));
            //_animator.AddAnimation($"IdleNoSword", AnimatedSpriteHelper.GetSpriteArrayByRow(noSwordSprites, 4, 12, totalCols));
            //_animator.AddAnimation($"Walk", AnimatedSpriteHelper.GetSpriteArrayByRow(spritesWithSword, 7, 8, totalCols));
            //_animator.AddAnimation($"WalkNoSword", AnimatedSpriteHelper.GetSpriteArrayByRow(noSwordSprites, 5, 8, totalCols));
            //_animator.AddAnimation($"Run", AnimatedSpriteHelper.GetSpriteArrayByRow(spritesWithSword, 8, 8, totalCols));
            //_animator.AddAnimation($"RunNoSword", AnimatedSpriteHelper.GetSpriteArrayByRow(noSwordSprites, 6, 8, totalCols));
            //_animator.AddAnimation($"Roll", AnimatedSpriteHelper.GetSpriteArrayByRow(spritesWithSword, 9, 6, totalCols), rollFps);
            //_animator.AddAnimation($"RollNoSword", AnimatedSpriteHelper.GetSpriteArrayByRow(noSwordSprites, 7, 6, totalCols), rollFps);
            //_animator.AddAnimation($"Thrust", AnimatedSpriteHelper.GetSpriteArrayByRow(spritesWithSword, 10, 4, totalCols), thrustFps);
            //_animator.AddAnimation($"Slash", AnimatedSpriteHelper.GetSpriteArrayByRow(spritesWithSword, 11, 5, totalCols), slashFps);

            ////up
            //_animator.AddAnimation($"IdleUp", AnimatedSpriteHelper.GetSpriteArrayByRow(spritesWithSword, 12, 12, totalCols));
            //_animator.AddAnimation($"IdleUpNoSword", AnimatedSpriteHelper.GetSpriteArrayByRow(noSwordSprites, 8, 12, totalCols));
            //_animator.AddAnimation($"WalkUp", AnimatedSpriteHelper.GetSpriteArrayByRow(spritesWithSword, 13, 8, totalCols));
            //_animator.AddAnimation($"WalkUpNoSword", AnimatedSpriteHelper.GetSpriteArrayByRow(noSwordSprites, 9, 8, totalCols));
            //_animator.AddAnimation($"RunUp", AnimatedSpriteHelper.GetSpriteArrayByRow(spritesWithSword, 14, 8, totalCols));
            //_animator.AddAnimation($"RunUpNoSword", AnimatedSpriteHelper.GetSpriteArrayByRow(noSwordSprites, 10, 8, totalCols));
            //_animator.AddAnimation($"ThrustUp", AnimatedSpriteHelper.GetSpriteArrayByRow(spritesWithSword, 15, 4, totalCols), thrustFps);
            //_animator.AddAnimation($"SlashUp", AnimatedSpriteHelper.GetSpriteArrayFromRange(spritesWithSword, 210, 214), slashFps);
            //_animator.AddAnimation($"RollUp", AnimatedSpriteHelper.GetSpriteArrayByRow(spritesWithSword, 17, 8, totalCols), rollFps);
            //_animator.AddAnimation($"RollUpNoSword", AnimatedSpriteHelper.GetSpriteArrayByRow(noSwordSprites, 11, 8, totalCols), rollFps);

            ////death
            //_animator.AddAnimation($"Die", AnimatedSpriteHelper.GetSpriteArrayByRow(spritesWithSword, 18, 13, totalCols));
            //_animator.AddAnimation($"DieNoSword", AnimatedSpriteHelper.GetSpriteArrayByRow(noSwordSprites, 12, 13, totalCols));
        }

        
        /// <summary>
        /// get normalized direction the player is facing
        /// </summary>
        /// <returns></returns>
        public Vector2 GetFacingDirection()
        {
            var dir = Scene.Camera.MouseToWorldPoint() - Position;
            dir.Normalize();
            return dir;
        }

        public void PrepareForRespawn()
        {
            _healthComponent.Health = _healthComponent.MaxHealth;
            _apComponent.ActionPoints = 0;
            _statusComponent.Reset();
            _hurtbox.SetEnabled(true);
            //_shadow.SetEnabled(true);
        }

        public bool TryRaycast(int mask, out RaycastHit raycastHit)
        {
            var basePos = _originComponent.Origin;

            var dir = _velocityComponent.Direction != Vector2.Zero ? _velocityComponent.Direction : _velocityComponent.LastNonZeroDirection;

            var checkEnd = basePos + (dir * _checkRadius);

            raycastHit = Physics.Linecast(basePos, checkEnd, mask);
            return raycastHit.Collider != null;
        }
    }
}
