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
using Threadlock.Helpers;

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

        SpriteAnimator _vfxAnimator;

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
            var dir = EntityHelper.DirectionToEntity(Enemy, Enemy.TargetEntity);
            var inverseDir = dir * -1;

            var velocityComponent = Entity.GetComponent<VelocityComponent>();

            var animator = Entity.GetComponent<SpriteAnimator>();
            animator.Play(_dashAnimationName, SpriteAnimator.LoopMode.Once);
            var animDuration = AnimatedSpriteHelper.GetAnimationDuration(animator);
            var durPerSprite = animDuration / animator.CurrentAnimation.Sprites.Length;
            var chargeDuration = _buildUpFrames.Count * durPerSprite;
            var dashDuration = _dashFrames.Count * durPerSprite;

            var timer = 0f;
            bool hasPlayedVfx = false;
            while (animator.CurrentAnimationName == _dashAnimationName && animator.AnimationState != SpriteAnimator.State.Completed)
            {
                timer += Time.DeltaTime;

                if (_buildUpFrames.Contains(animator.CurrentFrame))
                {
                    var speed = Lerps.Ease(EaseType.QuartOut, _chargeMoveSpeed, 0, timer, chargeDuration);
                    velocityComponent.Move(inverseDir, speed);
                }

                if (_dashFrames.Contains(animator.CurrentFrame))
                {
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
                    velocityComponent.Move(dir, speed);
                }

                yield return null;
            }

            animator.Play(_crossAnimationName, SpriteAnimator.LoopMode.Once);
            while (animator.CurrentAnimationName == _crossAnimationName && animator.AnimationState != SpriteAnimator.State.Completed)
            {
                yield return null;
            }
        }

        protected override void Reset()
        {

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
