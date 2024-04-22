﻿using Microsoft.Xna.Framework.Audio;
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

        StreamingVoice _musicVoice;

        public AudioDevice AudioDevice { get; }

        public string CurrentSongName;
        public Song CurrentSong;

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
        /// play music via song model
        /// </summary>
        /// <param name="songModel"></param>
        /// <param name="looping"></param>
        //public unsafe void PlayMusic(SongModel songModel, bool looping = true)
        //{
        //    PlayMusic(songModel.Path, looping);
        //}

        /// <summary>
        /// play music by file path
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="looping"></param>
        public unsafe void PlayMusic(string filePath, bool looping = true)
        {
            //StopMusic();

            var audioDataOgg = new AudioDataOgg(AudioDevice, filePath);

            _musicVoice = new StreamingVoice(AudioDevice, audioDataOgg.Format);

            _musicVoice.Load(audioDataOgg);

            _musicVoice.SetVolume(0);

            _musicVoice.Loop = looping;

            _musicVoice.Play();

            Game1.StartCoroutine(FadeIn(1.5f));
        }

        public void SetFilterFrequency(float frequency)
        {
            _musicVoice.SetFilterFrequency(frequency);
        }

        IEnumerator FadeIn(float time)
        {
            if (_musicVoice == null)
                yield break;

            var voice = _musicVoice;

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
        }

        public void PauseMusic()
        {
            _musicVoice?.Pause();
        }

        public void ResumeMusic()
        {
            _musicVoice?.Play();
        }

        public void StopMusic()
        {
            _musicVoice?.Unload();
            //FAudio.FAudioSourceVoice_Stop(_musicVoiceHandle, 0, FAudio.FAUDIO_COMMIT_NOW);
            //FAudio.FAudioSourceVoice_FlushSourceBuffers(_musicVoiceHandle);
            //MediaPlayer.Stop();
        }

        public IEnumerator FadeoutMusic(float time)
        {
            if (_musicVoice == null)
                yield break;

            var voice = _musicVoice;

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

            voice.Unload();
        }

        public void UpdateMusicVolume()
        {
            _musicVoice?.SetVolume(_defaultMusicVolume * Settings.Instance.MusicVolume);
        }

        void OnGameExiting()
        {
            AudioDevice.Dispose();
        }
    }
}
