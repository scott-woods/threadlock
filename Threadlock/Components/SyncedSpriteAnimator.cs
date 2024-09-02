using Nez;
using Nez.Sprites;
using Nez.Textures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.SceneComponents;
using Threadlock.StaticData;

namespace Threadlock.Components
{
    public class SyncedSpriteAnimator : SpriteRenderer, IUpdatable
    {
        public Dictionary<string, SpriteAnimation> Animations { get { return _animations; } }

        readonly Dictionary<string, SpriteAnimation> _animations = new Dictionary<string, SpriteAnimation>();

        /// <summary>
		/// the current animation
		/// </summary>
		public SpriteAnimation CurrentAnimation { get; private set; }

        /// <summary>
        /// the name of the current animation
        /// </summary>
        public string CurrentAnimationName { get; private set; }

        SyncedAnimationPlayer _animationPlayer;

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            _animationPlayer = Entity.Scene.GetOrCreateSceneComponent<SyncedAnimationPlayer>();
        }

        public override void OnEnabled()
        {
            base.OnEnabled();

            if (CurrentAnimation != null)
                _animationPlayer.RegisterAnimation(CurrentAnimationName, CurrentAnimation);
        }

        public override void OnDisabled()
        {
            base.OnDisabled();

            HandleChange();
        }

        public void Play(string animationName)
        {
            if (_animations.TryGetValue(animationName, out var animation))
            {
                _animationPlayer ??= Game1.Scene.GetOrCreateSceneComponent<SyncedAnimationPlayer>();
                _animationPlayer.RegisterAnimation(animationName, animation);

                HandleChange();

                CurrentAnimationName = animationName;
                CurrentAnimation = animation;
            }
        }

        public void Update()
        {
            var frame = _animationPlayer.FrameDictionary[CurrentAnimationName];
            Sprite = CurrentAnimation.Sprites[frame];
        }

        public void AddAnimation(string animationName, Sprite[] sprites, int fps = 10)
        {
            var animation = new SpriteAnimation(sprites, fps);
            if (Sprite == null && animation.Sprites.Length > 0)
                SetSprite(animation.Sprites[0]);
            _animations[animationName] = animation;
        }

        void HandleChange()
        {
            bool shouldDeregister = true;
            var animators = Game1.Scene.FindComponentsOfType<SyncedSpriteAnimator>();
            foreach (var animator in animators)
            {
                if (animator == this || !animator.Enabled)
                    continue;

                if (animator.CurrentAnimationName == CurrentAnimationName)
                {
                    shouldDeregister = false;
                    break;
                }
            }

            if (shouldDeregister)
                _animationPlayer.DeregisterAnimation(CurrentAnimationName);
        }
    }
}
