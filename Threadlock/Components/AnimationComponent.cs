using Nez;
using Nez.Sprites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Helpers;
using Threadlock.StaticData;

namespace Threadlock.Components
{
    public class AnimationComponent : Component, IUpdatable
    {
        SpriteAnimator _animator;

        int _currentFrame;
        AnimationConfig _currentAnimation;

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            _animator = Entity.GetComponent<SpriteAnimator>();
        }

        public void Update()
        {
            if (_animator.IsRunning)
            {
                if (_currentAnimation == null || _currentAnimation.Name != _animator.CurrentAnimationName)
                {
                    DataLoader.AnimationData.TryGetValue(_animator.CurrentAnimationName, out _currentAnimation);
                    _currentFrame = -1;
                }

                if (_currentFrame != _animator.CurrentFrame)
                {
                    //update frame
                    _currentFrame = _animator.CurrentFrame;

                    //handle frame data
                    if (_currentAnimation.FrameData.TryGetValue(_animator.CurrentFrame, out var frameData))
                    {
                        //handle sounds
                        if (frameData.Sounds != null && frameData.Sounds.Count > 0)
                        {
                            if (frameData.PickRandomSound)
                                Game1.AudioManager.PlaySound($"Content/Audio/Sounds/{frameData.Sounds.RandomItem()}.wav");
                            else
                            {
                                foreach (var sound in frameData.Sounds)
                                    Game1.AudioManager.PlaySound($"Content/Audio/Sounds/{sound}.wav");
                            }
                        }
                    }
                }
            }
        }

        public void PlayAnimation(string animationName)
        {
            if (string.IsNullOrWhiteSpace(animationName))
                return;

            //get config
            _currentAnimation = AnimatedSpriteHelper.GetDirectionalAnimation(animationName, _animator.Entity);

            //no config found, return
            if (_currentAnimation == null)
                return;

            _currentFrame = -1;

            //ensure animation exists on animator
            if (!_animator.Animations.ContainsKey(_currentAnimation.Name))
                return;

            //if animation is already playing, return
            if (_animator.IsAnimationActive(_currentAnimation.Name))
                return;

            //handle flip
            var renderers = _animator.Entity.GetComponents<SpriteRenderer>();
            foreach (var renderer in renderers)
                renderer.FlipX = _currentAnimation.FlipX;

            //play the animation
            _animator.Play(_currentAnimation.Name, _currentAnimation.Loop ?? false ? SpriteAnimator.LoopMode.Loop : SpriteAnimator.LoopMode.Once);
        }
    }
}
