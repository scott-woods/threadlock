using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using Nez.Systems;
using Nez.Tweens;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.StaticData;

namespace Threadlock.Entities.Characters.Player.PlayerActions
{
    [PlayerActionInfo("Blink", 2, "Instantly teleport to a short distance away.", "135")]
    public class Teleport : PlayerAction
    {
        //consts
        const float _maxRadius = 100f;
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
            _simPlayer = Entity.Scene.AddEntity(new SimPlayer());

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

            return Entity.Scene.Camera.MouseToWorldPoint();
        }

        void UpdateSimPlayerPosition()
        {
            //get desired position
            var desiredPos = GetDesiredPosition();

            //first, distance to mouse must be within radius
            var dist = Vector2.Distance(desiredPos, Entity.Position);
            if (dist >= _maxRadius)
            {
                var dir = desiredPos - Entity.Position;
                dir.Normalize();
                desiredPos = Entity.Position + (dir * _maxRadius);
            }

            _simPlayer.Position = desiredPos;
        }
    }
}
