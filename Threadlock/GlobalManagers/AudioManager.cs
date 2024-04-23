using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using Nez;
using Nez.Tweens;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Audio;
using Threadlock.SaveData;

namespace Threadlock.GlobalManagers
{
    public class AudioManager : GlobalManager
    {
        const float _defaultMusicVolume = .2f;
        const float _defaultSoundVolume = .28f;
        const float _volumeReductionFactor = .07f;

        Dictionary<string, StreamingVoice> _musicDictionary = new Dictionary<string, StreamingVoice>();
        StreamingVoice _activeVoice;

        public AudioDevice AudioDevice { get; }

        public string CurrentSongName;
        public Song CurrentSong;

        ICoroutine _fadeInCoroutine, _fadeOutCoroutine;

        Dictionary<SoundEffect, List<SoundEffectInstance>> _soundInstances = new Dictionary<SoundEffect, List<SoundEffectInstance>>();

        public AudioManager(GameStateManager gameStateManager)
        {
            AudioDevice = new AudioDevice();

            Game1.Emitter.AddObserver(CoreEvents.Exiting, OnGameExiting);
            gameStateManager.Emitter.AddObserver(GameStateEvents.Paused, () => SetFilterFrequency(.1f));
            gameStateManager.Emitter.AddObserver(GameStateEvents.Unpaused, () => SetFilterFrequency(1f));
        }

        public override void Update()
        {
            base.Update();

            AudioDevice.WakeThread();
        }

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
                _soundInstances[sound] = new List<SoundEffectInstance>();

            //clean up sound instances that have stopped
            _soundInstances[sound].RemoveAll(instance => instance.State == Microsoft.Xna.Framework.Audio.SoundState.Stopped);

            //create instance
            var soundInstance = sound.CreateInstance();
            _soundInstances[sound].Insert(0, soundInstance);
            int instanceCount = _soundInstances[sound].Count;

            //reduce volume relative to how many sound instances there are currently
            for (int i = _soundInstances[sound].Count - 1; i >= 0; i--)
            {
                var currentInstance = _soundInstances[sound][i];
                float volume = Math.Max(0, (_defaultSoundVolume * Settings.Instance.SoundVolume) * (1 - (i * _volumeReductionFactor)));
                currentInstance.Volume = volume;
            }

            //play sound
            soundInstance.Play();

            //yield while playing
            while (soundInstance.State == Microsoft.Xna.Framework.Audio.SoundState.Playing)
                yield return null;
        }

        /// <summary>
        /// play music by file path
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="looping"></param>
        public unsafe void PlayMusic(string filePath, bool looping = true, float timeToFadeIn = 0f, float volume = 1f, bool replace = true)
        {
            //StopMusic();

            var audioDataOgg = new AudioDataOgg(AudioDevice, filePath);

            if (replace && _musicDictionary.Any())
                StopMusic();

            var voice = new StreamingVoice(AudioDevice, audioDataOgg.Format);
            voice.Loop = looping;
            _musicDictionary.Add(filePath, voice);

            voice.Load(audioDataOgg);

            if (volume > 0f)
                _activeVoice = voice;

            voice.Play();

            if (timeToFadeIn > 0f)
            {
                _fadeInCoroutine?.Stop();
                _fadeInCoroutine = Game1.StartCoroutine(FadeIn(voice, timeToFadeIn));
            }
            else
                voice.SetVolume(_defaultMusicVolume * Settings.Instance.MusicVolume * volume);
        }

        /// <summary>
        /// requires there to be an active voice different from the one passed in. slowly replaces the main voice with a new one
        /// </summary>
        /// <param name="musicName"></param>
        /// <param name="fadeInTime"></param>
        /// <param name="fadeOutTime"></param>
        public unsafe string FadeTo(string musicName, float fadeInTime, float fadeOutTime)
        {
            if (_activeVoice == null)
                return null;
            if (string.IsNullOrWhiteSpace(musicName))
                return null;

            if (_musicDictionary.TryGetValue(musicName, out var voice))
            {
                if (voice == _activeVoice)
                    return null;

                _fadeInCoroutine?.Stop();
                _fadeOutCoroutine?.Stop();
                _fadeInCoroutine = Game1.StartCoroutine(FadeIn(voice, fadeInTime));
                _fadeOutCoroutine = Game1.StartCoroutine(FadeOut(_activeVoice, fadeOutTime));

                var activeVoiceName = _musicDictionary.Keys.FirstOrDefault(s => _musicDictionary[s] == _activeVoice);

                _activeVoice = voice;

                return activeVoiceName;
            }

            return null;
        }

        public void SetFilterFrequency(float frequency, string musicName = null)
        {
            var targetVoice = _musicDictionary.Values.First();
            if (!string.IsNullOrWhiteSpace(musicName) && _musicDictionary.TryGetValue(musicName, out var voice))
                targetVoice = voice;
            targetVoice.SetFilterFrequency(frequency);
        }

        IEnumerator FadeIn(StreamingVoice voice, float time)
        {
            if (voice == null)
                yield break;

            voice.SetVolume(0);

            var timer = time;
            var initialVolume = 0;
            while (timer > 0)
            {
                timer -= Time.DeltaTime;

                var progress = (time - timer) / time;
                var lerpVolume = Lerps.Lerp(0, _defaultMusicVolume * Settings.Instance.MusicVolume, progress);
                voice.SetVolume(lerpVolume);

                yield return null;
            }

            _fadeInCoroutine = null;
        }

        IEnumerator FadeOut(StreamingVoice voice, float time)
        {
            if (voice == null)
                yield break;

            var timer = time;
            var initialVolume = voice.Volume;
            while (timer > 0)
            {
                timer -= Time.DeltaTime;

                var progress = (time - timer) / time;
                var lerpVolume = Lerps.Lerp(initialVolume, 0, progress);
                voice.SetVolume(lerpVolume);

                yield return null;
            }

            _fadeOutCoroutine = null;
        }

        public void StopMusic(string musicName = null, float fadeTime = 0f)
        {
            if (!_musicDictionary.Any())
                return;

            var voice = _musicDictionary.Values.Last();
            if (!string.IsNullOrWhiteSpace(musicName))
            {
                voice = _musicDictionary[musicName];
                _musicDictionary.Remove(musicName);
            }
            else
                _musicDictionary.Remove(_musicDictionary.Last().Key);

            voice.Unload();
        }

        public void UpdateMusicVolume()
        {
            _activeVoice?.SetVolume(_defaultMusicVolume * Settings.Instance.MusicVolume);
        }

        void OnGameExiting()
        {
            AudioDevice.Dispose();
        }
    }
}
