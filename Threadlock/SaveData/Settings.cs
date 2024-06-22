using Nez.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Xna.Framework.Input;

namespace Threadlock.SaveData
{
    public class Settings
    {
        private static Settings _instance;

        public float MusicVolume = .7f;
        public float SoundVolume = .7f;

        public Keys UIActionKey = Keys.E;
        public Keys Reload = Keys.R;
        public Buttons UIActionButton = Buttons.A;
        public Keys CheckKey = Keys.E;
        public Keys CancelKey = Keys.X;
        public Keys ShowStatsKey = Keys.Tab;
        public Keys PauseKey = Keys.Escape;
        public Keys DodgeKey = Keys.Space;
        public Keys Action1Key = Keys.Q;
        public Keys Action2Key = Keys.E;
        public Keys Action3Key = Keys.F;
        public Keys UpKey = Keys.W;
        public Keys DownKey = Keys.S;
        public Keys LeftKey = Keys.A;
        public Keys RightKey = Keys.D;

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
