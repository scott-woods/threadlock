using Microsoft.Xna.Framework.Audio;
using Nez;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.SaveData;

namespace Threadlock.GlobalManagers
{
    public class AudioManager : GlobalManager
    {
        const float _defaultMusicVolume = .28f;
        const float _defaultSoundVolume = .28f;
        const float _volumeReductionFactor = .01f;

        Dictionary<SoundEffect, List<SoundEffectInstance>> _soundInstances = new Dictionary<SoundEffect, List<SoundEffectInstance>>();

        public void PlaySound(string soundName)
        {
            Game1.StartCoroutine(PlaySoundCoroutine(soundName));
        }

        public IEnumerator PlaySoundCoroutine(string soundName)
        {
            //load sound
            var sound = Game1.Scene.Content.LoadSoundEffect(soundName);

            //if an instance of this sound isn't already in the list, add it
            if (!_soundInstances.ContainsKey(sound))
            {
                _soundInstances[sound] = new List<SoundEffectInstance>();
            }

            //clean up sound instances that have stopped
            _soundInstances[sound].RemoveAll(instance => instance.State == Microsoft.Xna.Framework.Audio.SoundState.Stopped);

            //create instance
            var soundInstance = sound.CreateInstance();
            int instanceCount = _soundInstances[sound].Count;

            //reduce volume relative to how many sound instances there are currently
            float volume = Math.Max(0, (_defaultSoundVolume * Settings.Instance.SoundVolume) * (1 - (instanceCount * _volumeReductionFactor)));
            soundInstance.Volume = volume;

            //play sound
            soundInstance.Play();
            _soundInstances[sound].Add(soundInstance);

            //yield while playing
            while (soundInstance.State == Microsoft.Xna.Framework.Audio.SoundState.Playing)
            {
                yield return null;
            }
        }

        public void UpdateMusicVolume()
        {
            //_musicVoice?.SetVolume(_defaultMusicVolume * Settings.Instance.MusicVolume);
        }
    }
}
