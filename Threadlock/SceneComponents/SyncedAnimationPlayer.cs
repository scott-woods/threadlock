using Nez;
using Nez.Sprites;
using Nez.Textures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Nez.Sprites.SpriteAnimator;

namespace Threadlock.SceneComponents
{
    public class SyncedAnimationPlayer : SceneComponent
    {
        /// <summary>
		/// animation playback speed
		/// </summary>
		public float Speed = 1;

        Dictionary<string, SpriteAnimation> _managedAnimations = new Dictionary<string, SpriteAnimation>();

        public Dictionary<string, int> FrameDictionary { get => _frameDictionary; }
        Dictionary<string, int> _frameDictionary = new Dictionary<string, int>();

        Dictionary<string, float> _timerDictionary = new Dictionary<string, float>();

        public override void Update()
        {
            base.Update();

            foreach (var kvp in _managedAnimations)
            {
                var animationName = kvp.Key;
                var animation = kvp.Value;

                var secondsPerFrame = 1 / (animation.FrameRates[_frameDictionary[animationName]] * Speed);

                _timerDictionary[animationName] += Time.DeltaTime;
                var time = Math.Abs(_timerDictionary[animationName]);

                // figure out which frame we are on
                int i = Mathf.FloorToInt(time / secondsPerFrame);
                int n = animation.Sprites.Length;
                var currentFrame = i % n;

                _frameDictionary[animationName] = currentFrame;
            }
        }

        public void RegisterAnimation(string name, SpriteAnimation animation)
        {
            if (!_managedAnimations.ContainsKey(name))
            {
                _managedAnimations.Add(name, animation);
                _frameDictionary[name] = 0;
                _timerDictionary[name] = 0;
            }
        }

        public void DeregisterAnimation(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                _managedAnimations.Remove(name);
                _frameDictionary.Remove(name);
                _timerDictionary.Remove(name);
            }
        }
    }
}
