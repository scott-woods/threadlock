using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using Nez.Tweens;
using System.Collections;
using Threadlock.Components;
using Threadlock.Helpers;
using Threadlock.SaveData;
using Threadlock.StaticData;

namespace Threadlock.Entities.Characters.Player.PlayerActions
{
    [PlayerActionInfo("Blink", 1, "Instantly teleport to a short distance away.", "135")]
    public class Teleport : PlayerAction
    {
        //consts
        const float _maxRadius = 200f;
        const float _speed = 250f;

        //entities
        SimPlayer _simPlayer;

        //misc
        Vector2 _targetPosition;

        //coroutines
        ICoroutine _tweenCoroutine;

        #region PLAYER ACTION OVERRIDES

        public override IEnumerator PreparationCoroutine()
        {
            //create sim player
            _simPlayer = Entity.Scene.AddEntity(new SimPlayer(SimPlayerType.AttachToCursor, "Player_Idle", _targetPosition));

            UpdateSimPlayerPosition();

            while (!Controls.Instance.Confirm.IsPressed)
            {
                UpdateSimPlayerPosition();
                yield return null;
            }

            _targetPosition = _simPlayer.Position;
        }

        public override IEnumerator ExecutionCoroutine()
        {
            _simPlayer?.Destroy();

            //fade player out
            var animator = Player.Instance.GetComponent<SpriteAnimator>();
            animator.SetColor(Color.White);
            var tween = animator.TweenColorTo(Color.Transparent, .15f);
            tween.SetEaseType(EaseType.QuintIn);
            tween.Start();
            _tweenCoroutine = Game1.StartCoroutine(tween.WaitForCompletion());
            yield return _tweenCoroutine;
            _tweenCoroutine = null;

            //play sound
            Game1.AudioManager.PlaySound(Content.Audio.Sounds.Player_teleport);

            //move entity to target position
            Entity.Position = _targetPosition;

            //fade in
            tween = animator.TweenColorTo(Color.White, .1f);
            tween.Start();
            _tweenCoroutine = Game1.StartCoroutine(tween.WaitForCompletion());
            yield return _tweenCoroutine;
            _tweenCoroutine = null;
        }

        public override void Reset()
        {
            base.Reset();

            _tweenCoroutine?.Stop();
            _tweenCoroutine = null;

            _targetPosition = Vector2.Zero;

            var animator = Player.Instance.GetComponent<SpriteAnimator>();
            animator.SetColor(Color.White);

            _simPlayer?.Destroy();
        }

        #endregion      

        Vector2 GetDesiredPosition()
        {
            //if (!Game1.InputStateManager.IsUsingGamepad)
            //{
            //    return Scene.Camera.MouseToWorldPoint();
            //}
            //else
            //{
            //    var movement = Controls.Instance.DirectionalInput.Value;
            //    if (movement != Vector2.Zero)
            //    {
            //        movement.Normalize();
            //        return Position + (movement * Time.DeltaTime * _speed);
            //    }
            //    else return Position;
            //}

            var desiredPos = Entity.Scene.Camera.MouseToWorldPoint();

            var basePos = Entity.Position;
            if (Entity.TryGetComponent<OriginComponent>(out var oc))
                basePos = oc.Origin;
            var dir = desiredPos - basePos;
            dir.Normalize();

            var result = basePos;

            //first, distance to mouse must be within radius
            var dist = Vector2.Distance(desiredPos, basePos);
            if (dist >= _maxRadius)
                desiredPos = basePos + (dir * _maxRadius);

            var mapRenderer = EntityHelper.GetCurrentMap(Entity);

            //make sure position wouldn't put us in a wall
            if (TiledHelper.ValidatePosition(Entity.Scene, desiredPos))
                result = desiredPos;
            else
            {
                var raycast = Physics.Linecast(basePos, desiredPos, 1 << PhysicsLayers.Environment);
                if (raycast.Collider != null)
                {
                    var posNearWall = raycast.Point + (dir * -1 * 8);
                    if (Vector2.Distance(basePos, raycast.Point) > Vector2.Distance(posNearWall, raycast.Point))
                        if (TiledHelper.ValidatePosition(Entity.Scene, posNearWall))
                            result = posNearWall;
                }
            }

            return result;
        }

        void UpdateSimPlayerPosition()
        {
            //get desired position
            var desiredPos = GetDesiredPosition();

            //get direction
            var basePos = Entity.Position;
            if (Entity.TryGetComponent<OriginComponent>(out var oc))
                basePos = oc.Origin;
            var dir = desiredPos - basePos;
            dir.Normalize();

            if (_simPlayer.TryGetComponent<VelocityComponent>(out var velocityComponent))
                velocityComponent.LastNonZeroDirection = dir;

            if (Entity.TryGetComponent<VelocityComponent>(out var playerVc))
                playerVc.LastNonZeroDirection = dir;

            if (Entity.TryGetComponent<SpriteAnimator>(out var animator))
            {
                var animName = $"Idle{DirectionHelper.GetDirectionStringByVector(dir)}";
                if (!animator.Animations.ContainsKey(animName))
                    animName = "IdleDown";
                if (!animator.IsAnimationActive(animName))
                    animator.Play(animName);
            }

            _simPlayer.Position = desiredPos;
        }
    }
}
