using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components.Hitboxes;
using Threadlock.StaticData;

namespace Threadlock.Entities.Characters.Player.PlayerActions
{
    [PlayerActionInfo("Dash", 2, "Slice along a straight line, damaging any enemies in your way.", "005")]
    public class DashAction : PlayerAction
    {
        //constants
        const int _range = 48;
        const int _damage = 4;

        //states
        bool _isAnimationFinished = false;

        //other components
        SpriteAnimator _animator;

        //added components
        PrototypeSpriteRenderer _target;
        BoxHitbox _hitbox;

        Entity _hitboxEntity;

        //coroutines
        ICoroutine _executionCoroutine;

        #region LIFECYCLE

        public override void Initialize()
        {
            base.Initialize();

            _target = Entity.AddComponent(new PrototypeSpriteRenderer(4, 4));
            _target.SetEnabled(false);

            _hitbox = new BoxHitbox(_damage, 16, 8);
            Flags.SetFlagExclusive(ref _hitbox.PhysicsLayer, PhysicsLayers.PlayerHitbox);
            Flags.SetFlagExclusive(ref _hitbox.CollidesWithLayers, PhysicsLayers.EnemyHurtbox);
            _hitbox.SetEnabled(false);
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            _animator = Entity.GetComponent<SpriteAnimator>();
        }

        public override void OnDisabled()
        {
            base.OnDisabled();

            _target.SetEnabled(false);
            _hitbox.SetEnabled(false);
        }

        public override void Update()
        {
            base.Update();

            if (State == PlayerActionState.Preparing)
            {
                if (Controls.Instance.Confirm.IsPressed)
                {
                    HandlePreparationFinished();
                    return;
                }

                HandleDirection();
            }
        }

        #endregion

        public override void Prepare()
        {
            base.Prepare();

            //handle direction once before enabling
            HandleDirection();

            _target.SetEnabled(true);
        }

        public override void Execute()
        {
            base.Execute();

            //start coroutine
            _executionCoroutine = Game1.StartCoroutine(ExecuteCoroutine());
        }

        IEnumerator ExecuteCoroutine()
        {
            //get animation by angle
            var angle = MathHelper.ToDegrees(Mathf.AngleBetweenVectors(Entity.Position, Entity.Position + _target.LocalOffset));
            angle = (angle + 360) % 360;
            var animation = "Thrust";
            if (angle >= 45 && angle < 135) animation = "ThrustDown";
            else if (angle >= 225 && angle < 315) animation = "ThrustUp";

            //play animation
            _animator.Play(animation, SpriteAnimator.LoopMode.Once);
            _animator.OnAnimationCompletedEvent += OnAnimationFinished;
            _isAnimationFinished = false;

            //play sound
            Game1.AudioManager.PlaySound(Content.Audio.Sounds._20_Slash_02);

            //enable hitbox
            _hitboxEntity = Entity.Scene.CreateEntity("dash-hitbox", Entity.Position);
            _hitboxEntity.AddComponent(_hitbox);
            _hitboxEntity.SetRotationDegrees(angle);
            _hitbox.SetEnabled(true);

            //determine total amount of time needed to move to target 
            var secondsPerFrame = 1 / (_animator.CurrentAnimation.FrameRates[0] * _animator.Speed);
            var movementFrames = 1;
            var totalMovementTime = movementFrames * secondsPerFrame;
            var movementTimeRemaining = movementFrames * secondsPerFrame;

            //move entity
            //Log.Debug($"ExecuteDash: Waiting for {movementTimeRemaining}");
            var initialPosition = Entity.Position;
            var finalPosition = Entity.Position + _target.LocalOffset;
            while (movementTimeRemaining > 0)
            {
                //lerp towards target position using progress towards total movement time
                movementTimeRemaining -= Time.DeltaTime;
                var progress = (totalMovementTime - movementTimeRemaining) / totalMovementTime;
                var lerpPosition = Vector2.Lerp(initialPosition, finalPosition, progress);
                Entity.Position = lerpPosition;
                _hitboxEntity.Position = lerpPosition;

                yield return null;
            }
            //Log.Debug($"ExecuteDash: Finished waiting");

            //Log.Debug($"ExecuteDash: Waiting for _isAttacking to be false");
            while (!_isAnimationFinished)
            {
                yield return null;
            }
            //Log.Debug("ExecuteDash finished");

            _executionCoroutine = null;

            _target.SetEnabled(false);
            _hitbox.SetEnabled(false);
            _hitboxEntity.Destroy();
            _hitboxEntity = null;

            HandleExecutionFinished();
        }

        void HandleDirection()
        {
            var dir = Player.Instance.GetFacingDirection();

            var newFinalPos = Entity.Position;
            var range = _range;
            while (range > 0)
            {
                var testPos = Entity.Position + dir * range;
                newFinalPos = testPos;
                break;
            }

            _target.SetLocalOffset(newFinalPos - Entity.Position);
        }

        public override void Abort()
        {
            base.Abort();

            _executionCoroutine?.Stop();
            _executionCoroutine = null;

            _target.SetEnabled(false);
            _hitbox.SetEnabled(false);
            _hitboxEntity?.Destroy();
            _hitboxEntity = null;
        }

        void OnAnimationFinished(string animationName)
        {
            _animator.SetSprite(_animator.CurrentAnimation.Sprites.Last());
            _isAnimationFinished = true;
        }
    }
}
