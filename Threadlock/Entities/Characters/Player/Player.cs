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

        public float MoveSpeed = 135f;
        public Vector2 DefaultSpriteOffset = new Vector2(12, -2);

        //state machine
        public StateMachine<Player> StateMachine { get; set; }
        PlayerState _initialState = new Idle();

        public event Action<BasicWeapon> OnWeaponChanged;

        //components
        Mover _mover;
        SpriteAnimator _animator;
        SpriteTrail _spriteTrail;
        VelocityComponent _velocityComponent;
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
        DirectionComponent _directionComponent;

        public Player() : base("Player")
        {
            SetTag(EntityTags.EnemyTarget);

            //add components
            _mover = AddComponent(new Mover());
            AddComponent(new ProjectileMover());
            _animator = AddComponent(new SpriteAnimator());
            //_animator.SetLocalOffset(DefaultSpriteOffset);
            _animator.SetRenderLayer(RenderLayers.YSort);
            _velocityComponent = AddComponent(new VelocityComponent());
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
            //Flags.SetFlag(ref hurtboxCollider.CollidesWithLayers, PhysicsLayers.EnemyHitbox);
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

            //_shadow = AddComponent(new Shadow(_animator));

            var pointLight = AddComponent(new PointLight(Color.White));
            pointLight.SetRenderLayer(RenderLayers.Light);
            pointLight.SetIntensity(.5f);
            pointLight.SetRadius(100f);

            AddComponent(new WeaponManager());

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

            _directionComponent = AddComponent(new DirectionComponent());

            AddComponent(new InteractableChecker());
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

            _velocityComponent.Move(dir, MoveSpeed, false, true);

            AnimatedSpriteHelper.PlayAnimation(_animator, "Player_Run");
        }

        public void Idle()
        {
            AnimatedSpriteHelper.PlayAnimation(_animator, "Player_Idle");
        }

        #region OBSERVERS

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

        #endregion

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

            var dir = _directionComponent.GetCurrentDirection();

            var checkEnd = basePos + (dir * _checkRadius);

            raycastHit = Physics.Linecast(basePos, checkEnd, mask);
            return raycastHit.Collider != null;
        }
    }
}
