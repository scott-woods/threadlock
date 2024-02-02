using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.StaticData;

namespace Threadlock.Entities.Characters.Player.PlayerActions
{
    public class DashAction : PlayerAction
    {
        //constants
        const int _range = 48;

        //states
        bool _isAnimationFinished = false;

        //other components
        SpriteAnimator _animator;

        //added components
        List<Component> _components = new List<Component>();
        PrototypeSpriteRenderer _target;

        public override void Initialize()
        {
            base.Initialize();

            _target = AddComponent(new PrototypeSpriteRenderer(4, 4));

            SetEnabled(false);
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            _animator = Entity.GetComponent<SpriteAnimator>();
        }

        public override void OnEnabled()
        {
            base.OnEnabled();

            foreach (var component in _components)
                component.SetEnabled(true);
        }

        public override void OnDisabled()
        {
            base.OnDisabled();

            foreach (var component in _components)
                component.SetEnabled(false);
        }

        public override void Prepare(Action prepFinishedCallback)
        {
            base.Prepare(prepFinishedCallback);

            //handle direction once before enabling
            HandleDirection();

            //enable
            SetEnabled(true);
        }

        public override void Execute(Action executionCompletedCallback)
        {
            base.Execute(executionCompletedCallback);

            //start coroutine
            Core.StartCoroutine(ExecuteCoroutine());
        }

        public IEnumerator ExecuteCoroutine()
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

                yield return null;
            }
            //Log.Debug($"ExecuteDash: Finished waiting");

            //Log.Debug($"ExecuteDash: Waiting for _isAttacking to be false");
            while (!_isAnimationFinished)
            {
                yield return null;
            }
            //Log.Debug("ExecuteDash finished");

            HandleExecutionFinished();
        }

        public override void Update()
        {
            base.Update();

            if (State == PlayerActionState.Preparing)
            {
                if (Controls.Instance.Confirm.IsPressed)
                {
                    HandlePrepFinished();
                    return;
                }

                HandleDirection();
            }
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

            SetEnabled(false);
        }

        public override void HandleExecutionFinished()
        {
            SetEnabled(false);

            base.HandleExecutionFinished();
        }

        T AddComponent<T>(T component) where T : Component
        {
            Entity.AddComponent(component);
            _components.Add(component);
            return component;
        }

        void OnAnimationFinished(string animationName)
        {
            _animator.SetSprite(_animator.CurrentAnimation.Sprites.Last());
            _isAnimationFinished = true;
        }
    }
}
