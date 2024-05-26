using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using Nez.Textures;
using Nez.Tweens;
using Nez.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components.Hitboxes;
using Threadlock.Helpers;
using Threadlock.StaticData;

namespace Threadlock.Components.EnemyActions.Assassin
{
    public class AssassinDashSlice : EnemyAction
    {
        const float _minDistance = 64;
        const string _dashAnimationName = "SliceAttack";
        const string _crossAnimationName = "CrossSlice";
        readonly List<int> _buildUpFrames = new List<int> { 0, 1, 2, 3, 4 };
        readonly List<int> _dashFrames = new List<int> { 5, 6, 7, 8, 9 };
        const float _chargeMoveSpeed = 400f;
        const float _dashMoveSpeed = 1700f;
        const float _vfxOffset = 45;
        const float _dashHitboxRadius = 8;
        const float _dashHitboxOffset = 16;
        const float _crossHitboxRadius = 14;
        const float _crossHitboxOffset = 6;
        const float _delayBeforeCross = .5f;
        readonly List<int> _dashHitboxActiveFrames = new List<int> { 5, 6 };
        readonly List<int> _crossHitboxActiveFrames = new List<int> { 0 };
        const int _dashDamage = 3;
        const int _crossDamge = 3;
        const string _chargeSound = Nez.Content.Audio.Sounds._23_Slash_05;
        const string _dashSound = Nez.Content.Audio.Sounds._22_Slash_04;
        const string _crossSound = Nez.Content.Audio.Sounds._21_Slash_03;

        SpriteAnimator _vfxAnimator;
        CircleHitbox _dashHitbox;
        CircleHitbox _crossHitbox;

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            _dashHitbox = Entity.AddComponent(new CircleHitbox(_dashDamage, _dashHitboxRadius));
            Flags.SetFlagExclusive(ref _dashHitbox.PhysicsLayer, PhysicsLayers.EnemyHitbox);
            Flags.SetFlagExclusive(ref _dashHitbox.CollidesWithLayers, PhysicsLayers.PlayerHurtbox);
            _dashHitbox.SetEnabled(false);

            _crossHitbox = Entity.AddComponent(new CircleHitbox(_crossDamge, _crossHitboxRadius));
            Flags.SetFlagExclusive(ref _crossHitbox.PhysicsLayer, PhysicsLayers.EnemyHitbox);
            Flags.SetFlagExclusive(ref _crossHitbox.CollidesWithLayers, PhysicsLayers.PlayerHurtbox);
            _crossHitbox.SetEnabled(false);
        }

        #region Enemy action implementation

        public override float CooldownTime => 5f;

        public override int Priority => 0;

        public override bool CanExecute()
        {
            var dist = EntityHelper.DistanceToEntity(Enemy, Enemy.TargetEntity);
            if (dist <= _minDistance)
                return true;

            return false;
        }

        protected override IEnumerator ExecutionCoroutine()
        {
            //get direction to target
            var dir = EntityHelper.DirectionToEntity(Enemy, Enemy.TargetEntity);
            var inverseDir = dir * -1;

            //grab velocity component
            var velocityComponent = Entity.GetComponent<VelocityComponent>();
            
            //grab animator
            var animator = Entity.GetComponent<SpriteAnimator>();

            //update hitbox offset
            _dashHitbox.SetLocalOffset(dir * _dashHitboxOffset);

            //play dash animation
            animator.Play(_dashAnimationName, SpriteAnimator.LoopMode.Once);
            var animDuration = AnimatedSpriteHelper.GetAnimationDuration(animator);
            var durPerSprite = animDuration / animator.CurrentAnimation.Sprites.Length;
            var chargeDuration = _buildUpFrames.Count * durPerSprite;
            var dashDuration = _dashFrames.Count * durPerSprite;
            var timer = 0f;
            bool hasPlayedVfx = false;
            bool hasPositionedHitbox = false;
            bool hasPlayedChargeSound = false;
            bool hasPlayedDashSound = false;
            while (animator.CurrentAnimationName == _dashAnimationName && animator.AnimationState != SpriteAnimator.State.Completed)
            {
                //increment timer
                timer += Time.DeltaTime;

                if (_buildUpFrames.Contains(animator.CurrentFrame))
                {
                    if (!hasPlayedChargeSound)
                    {
                        Game1.AudioManager.PlaySound(_chargeSound);
                        hasPlayedChargeSound = true;
                    }

                    var speed = Lerps.Ease(EaseType.QuartOut, _chargeMoveSpeed, 0, timer, chargeDuration);
                    velocityComponent.Move(inverseDir, speed);
                }

                //update hitbox enabled status
                _dashHitbox.SetEnabled(_dashHitboxActiveFrames.Contains(animator.CurrentFrame));

                if (_dashFrames.Contains(animator.CurrentFrame))
                {
                    if (!hasPlayedDashSound)
                    {
                        Game1.AudioManager.PlaySound(_dashSound);
                        hasPlayedDashSound = true;
                    }

                    if (!hasPositionedHitbox)
                    {
                        _dashHitbox.SetLocalOffset(dir * _dashHitboxOffset);

                        hasPositionedHitbox = true;
                    }

                    if (!hasPlayedVfx)
                    {
                        var vfxEntity = Entity.Scene.CreateEntity("dash-slice-vfx", Entity.Position + (dir * _vfxOffset));
                        _vfxAnimator = vfxEntity.AddComponent(new SpriteAnimator());
                        _vfxAnimator.OnAnimationCompletedEvent += OnVfxCompleted;
                        _vfxAnimator.SetRenderLayer(animator.RenderLayer);
                        var texture = Entity.Scene.Content.LoadTexture(Nez.Content.Textures.Characters.Assassin.Assassinvfx);
                        var sprites = Sprite.SpritesFromAtlas(texture, 91, 19);
                        _vfxAnimator.AddAnimation("Play", sprites.ToArray());
                        var radians = (float)Math.Atan2(dir.Y, dir.X);
                        vfxEntity.SetRotation(radians);
                        _vfxAnimator.Play("Play", SpriteAnimator.LoopMode.Once);

                        hasPlayedVfx = true;
                    }

                    var speed = Lerps.Ease(EaseType.ExpoOut, _dashMoveSpeed, 0, timer - chargeDuration, dashDuration);
                    velocityComponent.Move(dir, speed, true);
                }

                yield return null;
            }

            //make sure dash hitbox is disabled
            _dashHitbox.SetEnabled(false);

            //delay before cross slice
            yield return Coroutine.WaitForSeconds(_delayBeforeCross);

            //update cross slice hitbox offset
            _crossHitbox.SetLocalOffset(dir * _crossHitboxOffset);

            //play cross slice animation
            animator.Play(_crossAnimationName, SpriteAnimator.LoopMode.Once);
            Game1.AudioManager.PlaySound(_crossSound);
            while (animator.CurrentAnimationName == _crossAnimationName && animator.AnimationState != SpriteAnimator.State.Completed)
            {
                _crossHitbox.SetEnabled(_crossHitboxActiveFrames.Contains(animator.CurrentFrame));

                yield return null;
            }

            //make sure cross slice hitbox is disabled
            _crossHitbox.SetEnabled(false);
        }

        protected override void Reset()
        {
            _dashHitbox.SetEnabled(false);
            _crossHitbox.SetEnabled(false);
        }

        #endregion

        void OnVfxCompleted(string animationName)
        {
            _vfxAnimator.OnAnimationCompletedEvent -= OnVfxCompleted;

            _vfxAnimator.SetEnabled(false);
            _vfxAnimator.Entity.Destroy();

            _vfxAnimator = null;
        }
    }
}
