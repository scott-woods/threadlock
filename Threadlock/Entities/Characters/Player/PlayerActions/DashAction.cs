using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using Nez.Textures;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Components.Hitboxes;
using Threadlock.Helpers;
using Threadlock.SaveData;
using Threadlock.StaticData;

namespace Threadlock.Entities.Characters.Player.PlayerActions
{
    [PlayerActionInfo("Dash", 2, "Slice along a straight line, damaging any enemies in your way.", "005")]
    public class DashAction : PlayerAction
    {
        //constants
        const int _range = 48;
        const int _damage = 4;

        //other components
        SpriteAnimator _animator;
        VelocityComponent _velocityComponent;

        //added components
        PrototypeSpriteRenderer _target;
        BoxHitbox _hitbox;

        Entity _hitboxEntity;

        //coroutines
        ICoroutine _executionCoroutine;

        AnimationWaiter _animationWaiter;

        Vector2 _direction;

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

            if (Entity.TryGetComponent<SpriteAnimator>(out var animator))
            {
                _animator = animator;

                _animationWaiter = new AnimationWaiter(_animator);

                var texture = Game1.Content.LoadTexture(Content.Textures.Characters.Player.Sci_fi_player_with_sword);
                var sprites = Sprite.SpritesFromAtlas(texture, 64, 65);
                _animator.AddAnimation("ChargeDash", AnimatedSpriteHelper.GetSpriteArray(sprites, new List<int> { 145 }));
                _animator.AddAnimation("ChargeDashDown", AnimatedSpriteHelper.GetSpriteArray(sprites, new List<int> { 57 }));
                _animator.AddAnimation("ChargeDashUp", AnimatedSpriteHelper.GetSpriteArray(sprites, new List<int> { 212 }));
            }

            if (Entity.TryGetComponent<VelocityComponent>(out var velocityComponent))
                _velocityComponent = velocityComponent;
        }

        public override void OnRemovedFromEntity()
        {
            base.OnRemovedFromEntity();

            if (Entity.TryGetComponent<SpriteAnimator>(out var animator))
            {
                animator.Animations.Remove("ChargeDash");
                animator.Animations.Remove("ChargeDashDown");
                animator.Animations.Remove("ChargeDashUp");
            }
        }

        #endregion

        #region PLAYER ACTION

        public override IEnumerator PreparationCoroutine()
        {
            //handle direction once before enabling
            HandleDirection();

            //show target
            _target.SetEnabled(true);

            while (!Controls.Instance.Confirm.IsPressed)
            {
                HandleDirection();
                yield return null;
            }

            _target.SetEnabled(false);
        }

        public override IEnumerator ExecutionCoroutine()
        {
            //get animation by angle
            var angle = MathHelper.ToDegrees(Mathf.AngleBetweenVectors(Entity.Position, Entity.Position + _target.LocalOffset));
            angle = (angle + 360) % 360;
            var animation = "Thrust";
            if (angle >= 45 && angle < 135) animation = "ThrustDown";
            else if (angle >= 225 && angle < 315) animation = "ThrustUp";

            //play animation
            Game1.StartCoroutine(_animationWaiter.WaitForAnimation(animation));

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
            while (_animator.IsAnimationActive(animation) && _animator.AnimationState == SpriteAnimator.State.Running)
                yield return null;
            //Log.Debug("ExecuteDash finished");
        }

        public override void Reset()
        {
            base.Reset();

            _executionCoroutine?.Stop();
            _executionCoroutine = null;

            _target.SetEnabled(false);
            _hitbox.SetEnabled(false);
            _hitboxEntity?.Destroy();
            _hitboxEntity = null;
        }

        #endregion

        void HandleDirection()
        {
            //get direction player is aiming
            var dir = Player.Instance.GetFacingDirection();

            //update velocity component to face proper direction
            if (Entity.TryGetComponent<VelocityComponent>(out var playerVc))
                playerVc.LastNonZeroDirection = dir;

            //play charge anim by direction
            var animName = $"ChargeDash{DirectionHelper.GetDirectionStringByVector(dir)}";
            if (_animator.Animations.ContainsKey(animName) && !_animator.IsAnimationActive(animName))
                _animator.Play(animName);

            var basePos = Entity.Position;
            if (Entity.TryGetComponent<OriginComponent>(out var oc))
                basePos = oc.Origin;

            var mapRenderer = EntityHelper.GetCurrentMap(Entity);
            var desiredPos = basePos + (dir * _range);
            var raycast = Physics.Linecast(basePos, desiredPos, 1 << PhysicsLayers.Environment);
            if (raycast.Collider != null)
            {
                var posNearWall = raycast.Point + (dir * -1 * 8);
                if (Vector2.Distance(basePos, raycast.Point) > Vector2.Distance(posNearWall, raycast.Point))
                {
                    if (TiledHelper.ValidatePosition(Entity.Scene, posNearWall))
                        desiredPos = posNearWall;
                    else
                        desiredPos = basePos;
                }
                else
                    desiredPos = basePos;
            }

            //animation
            var animation = $"ChargeDash{DirectionHelper.GetDirectionStringByVector(dir)}";
            if (!_animator.IsAnimationActive(animation))
                _animator.Play(animation);

            //update the target's position
            _target.SetLocalOffset(desiredPos - basePos);

            //update direction
            _direction = dir;
        }
    }
}
