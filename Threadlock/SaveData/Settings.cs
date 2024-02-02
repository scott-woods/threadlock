using Nez.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Threadlock.SaveData
{
    public class Settings
    {
        private static Settings _instance;

        public float MusicVolume = .7f;
        public float SoundVolume = .7f;

        private Settings()
        {
            Nez.Core.Emitter.AddObserver(Nez.CoreEvents.Exiting, OnExiting);
        }

        public static Settings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = LoadData();
                }
                return _instance;
            }
        }

        public void SaveData()
        {
            var settings = JsonSettings.HandlesReferences;
            settings.TypeNameHandling = TypeNameHandling.All;

            var json = Json.ToJson(this, settings);
            File.WriteAllText("Data/settings.json", json);
        }

        public void UpdateAndSave()
        {
            SaveData();
            Game1.AudioManager.UpdateMusicVolume();
        }

        private static Settings LoadData()
        {
            if (File.Exists("Data/settings.json"))
            {
                var json = File.ReadAllText("Data/settings.json");
                _instance = Json.FromJson<Settings>(json);
            }
            else
            {
                _instance = new Settings();
            }

            return _instance;
        }

        void OnExiting()
        {
            SaveData();
        }
    }
}
