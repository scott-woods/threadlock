using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez;
using Nez.Sprites;
using Nez.Systems;
using Nez.Textures;
using Nez.Tweens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Components.Hitboxes;
using Threadlock.Helpers;
using Threadlock.SaveData;
using Threadlock.StaticData;

namespace Threadlock.Entities.Characters.Player.BasicWeapons
{
    public class Sword : BasicWeapon, IUpdatable
    {
        //constants
        const float _initialMoveSpeed = 170f;
        const float _finalMoveSpeed = 0f;
        const float _finisherDelay = .08f;
        const int _damage = 1;
        const float _normalPushForce = 1f;
        const float _finisherPushForce = 1.5f;
        const int _movementFrames = 3; //the number of frames that we are moving for
        const float _hitboxOffset = 12f;

        public override bool CanMove => false;

        public float Speed = 1f;
        float _originalAnimatorSpeed;

        int _comboCounter = 0;
        bool _continueCombo = false;
        float _elapsedTime = 0f;
        bool _isAttacking = false;
        bool _shouldPerformFinisher = false;
        Vector2 _direction;

        List<int> _hitboxActiveFrames = new List<int>();
        List<int> _postAttackFrames = new List<int>();
        int _preFinisherFrame;

        //passed components
        SpriteAnimator _animator;
        VelocityComponent _velocityComponent;

        //added components
        CircleHitbox _hitbox;

        //list of colliders hit during attack
        List<Collider> _hitColliders = new List<Collider>();

        public override void Initialize()
        {
            base.Initialize();

            _hitbox = Entity.AddComponent(new CircleHitbox(_damage, 12));
            Flags.SetFlagExclusive(ref _hitbox.PhysicsLayer, PhysicsLayers.PlayerHitbox);
            Flags.SetFlagExclusive(ref _hitbox.CollidesWithLayers, PhysicsLayers.EnemyHurtbox);
            _hitbox.SetEnabled(false);
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            _animator = Entity.GetComponent<SpriteAnimator>();
            _velocityComponent = Entity.GetComponent<VelocityComponent>();
        }

        public void Update()
        {
            if (_isAttacking)
            {
                //if it's been at least one frame, check for combos
                if (_animator.CurrentFrame > 0)
                {
                    //check if we can move to the next attack
                    if (_comboCounter < 3)
                    {
                        //if left click
                        if (Controls.Instance.Melee.IsPressed)
                        {
                            //get direction
                            var dir = Player.Instance.GetFacingDirection();
                            if (dir != Vector2.Zero)
                            {
                                dir.Normalize();
                                _direction = dir;
                            }

                            if (_comboCounter == 2)
                            {
                                //queue up finisher
                                _shouldPerformFinisher = true;
                            }
                            else
                            {
                                //queue up next in combo
                                _continueCombo = true;
                            }
                        }

                        //if next in combo is queued but not on finisher
                        if (_continueCombo && _postAttackFrames.Contains(_animator.CurrentFrame))
                        {
                            _continueCombo = false;
                            _isAttacking = false;
                            _elapsedTime = 0;

                            PerformAttack(_direction);
                            return;
                        }

                        //if finisher is queued and we are on pre finisher frame get ready
                        if (_shouldPerformFinisher && _animator.CurrentFrame >= _preFinisherFrame)
                        {
                            _shouldPerformFinisher = false;
                            _isAttacking = false;
                            _elapsedTime = 0;

                            _animator.Pause();
                            _animator.SetSprite(_animator.CurrentAnimation.Sprites[_preFinisherFrame]);

                            Core.Schedule(_finisherDelay / Speed, timer =>
                            {
                                PerformAttack(_direction);
                            });

                            return;
                        }
                    }
                }

                //increment time
                _elapsedTime += Time.DeltaTime;

                //get animation duration
                //var animationDuration = _animator.CurrentAnimation.Sprites.Count() / _animator.CurrentAnimation.FrameRate;
                if (_animator.CurrentAnimation == null)
                    return;
                var animationDuration = _movementFrames / (_animator.CurrentAnimation.FrameRates[0] * _animator.Speed);

                //if elapsed time is less than duration, we are still attacking
                if (_elapsedTime < animationDuration)
                {
                    //get lerp factor
                    float lerpFactor = _elapsedTime / animationDuration;

                    //determine move speed based on which combo we are on
                    var initialSpeed = _initialMoveSpeed;
                    if (_comboCounter == 2) initialSpeed *= 1.1f;
                    else if (_comboCounter == 3) initialSpeed *= 2f;

                    float currentSpeed = Lerps.Lerp(initialSpeed, _finalMoveSpeed, lerpFactor);
                    _velocityComponent.Move(_direction, currentSpeed);
                }

                //handle hitbox
                if (_hitboxActiveFrames.Contains(_animator.CurrentFrame))
                {
                    _hitbox.SetEnabled(true);
                }
                else _hitbox.SetEnabled(false);

                //check for hit
                var colliders = Physics.BoxcastBroadphaseExcludingSelf(_hitbox, _hitbox.CollidesWithLayers);
                if (colliders.Count > 0)
                {
                    foreach (var collider in colliders)
                    {
                        if (!_hitColliders.Contains(collider))
                        {
                            Emitter.Emit(BasicWeaponEventTypes.Hit, _damage);
                            _hitColliders.Add(collider);
                        }
                    }
                }
            }
        }

        public override void OnUnequipped()
        {

        }

        /// <summary>
        /// called from player state, starts a new attack
        /// </summary>
        /// <param name="attackCompletedCallback"></param>
        protected override void StartAttack()
        {
            //set animation handler
            _animator.OnAnimationCompletedEvent += OnAnimationFinished;

            //set original animator speed to return to once finished attack
            _originalAnimatorSpeed = _animator.Speed;

            //set animator speed
            _animator.Speed *= Speed;

            //attack in direction of mouse
            var dir = Player.Instance.GetFacingDirection();
            dir.Normalize();
            _direction = dir;

            PerformAttack(dir);
        }

        void PerformAttack(Vector2 dir)
        {
            //increment combo
            _comboCounter++;

            //clear hit colliders
            _hitColliders.Clear();

            //set hitbox position
            var hitboxPosition = Entity.Position + dir * _hitboxOffset;
            _hitbox.SetLocalOffset(hitboxPosition - Entity.Position);
            _hitbox.Direction = dir;
            //_hitbox.SetEnabled(true);

            //update hitbox push force
            if (_comboCounter > 2)
            {
                _hitbox.PushForce = _finisherPushForce;
            }
            else _hitbox.PushForce = _normalPushForce;

            //get angle in degrees
            var angle = MathHelper.ToDegrees(Mathf.AngleBetweenVectors(Entity.Position, hitboxPosition));
            angle = (angle + 360) % 360;

            //animation
            var animation = "";
            if (_comboCounter == 1 || _comboCounter == 3)
            {
                animation = "Thrust";
            }
            else
            {
                animation = "Slash";
            }

            //account for angle
            if (dir.Y < 0 && Math.Abs(dir.X) < .75f)
                animation += "Up";
            else if (dir.Y > 0 && Math.Abs(dir.X) < .75f)
                animation += "Down";

            //if (angle >= 45 && angle < 135) animation += "Down";
            //else if (angle >= 225 && angle < 315) animation += "Up";

            switch (animation)
            {
                case "ThrustDown":
                case "ThrustUp":
                case "Thrust":
                case "Slash":
                    _hitboxActiveFrames = new List<int> { 0 };
                    _postAttackFrames = new List<int> { 2, 3 };
                    _preFinisherFrame = 2;
                    break;
                case "SlashUp":
                case "SlashDown":
                    _hitboxActiveFrames = new List<int> { 0 };
                    _postAttackFrames = new List<int> { 2, 3 };
                    _preFinisherFrame = 2;
                    break;
            }

            //determine and play sound
            switch (_comboCounter)
            {
                case 1:
                    Game1.AudioManager.PlaySound(Content.Audio.Sounds._32_Swoosh_sword_2);
                    break;
                case 2:
                    Game1.AudioManager.PlaySound(Content.Audio.Sounds._33_Swoosh_Sword_3);
                    break;
                case 3:
                    Game1.AudioManager.PlaySound(Content.Audio.Sounds._31_swoosh_sword_1);
                    break;
            }

            Debug.Log($"Playing animation: {animation}");

            //play animation
            _animator.Play(animation, SpriteAnimator.LoopMode.Once);

            //set is attacking to true so update starts moving and reset elapsed time
            _isAttacking = true;
            _elapsedTime = 0f;
        }

        void OnAnimationFinished(string animationName)
        {
            Debug.Log("Finished melee attack animation: " + animationName);

            //remove event handler
            _animator.OnAnimationCompletedEvent -= OnAnimationFinished;

            //hold last frame so it doesn't look weird
            _animator.SetSprite(_animator.CurrentAnimation.Sprites.Last());

            //reset
            Reset();

            CompletionCallback?.Invoke();
        }

        void Reset()
        {
            //animator
            _animator.Speed = _originalAnimatorSpeed != 0 ? _originalAnimatorSpeed : 1f;
            _animator.OnAnimationCompletedEvent -= OnAnimationFinished;

            //fields
            _comboCounter = 0;
            _continueCombo = false;
            _elapsedTime = 0f;
            _isAttacking = false;
            _shouldPerformFinisher = false;

            //hitbox
            _hitbox.SetEnabled(false);

            //clear hit colliders
            _hitColliders.Clear();
        }

        public void CancelAttack()
        {
            Debug.Log("Cancel melee attack");
            Reset();
        }
    }
}
