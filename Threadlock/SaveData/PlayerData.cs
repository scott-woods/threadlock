using Nez.Persistence;
using Nez.Systems;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Entities.Characters.Player.PlayerActions;
using Threadlock.Models;

namespace Threadlock.SaveData
{
    public class PlayerData
    {
        [JsonExclude]
        public Emitter<PlayerDataEvents> Emitter = new Emitter<PlayerDataEvents>();

        private static PlayerData _instance;
        public static PlayerData Instance
        {
            get
            {
                if (_instance == null)
                    _instance = LoadData();
                return _instance;
            }
        }

        public PlayerActionType OffensiveAction1 = PlayerActionType.FromType<DashAction>();
        //public PlayerActionType OffensiveAction2 = PlayerActionType.FromType<Grip>();
        public PlayerActionType OffensiveAction2 = null;
        public PlayerActionType SupportAction = PlayerActionType.FromType<Teleport>();

        [JsonInclude]
        int _dollahs = 0;
        public int Dollahs
        {
            get => _dollahs;
            set
            {
                _dollahs = value;
                Emitter.Emit(PlayerDataEvents.DollahsChanged);
            }
        }
        [JsonInclude]
        int _dust = 0;
        public int Dust
        {
            get => _dust;
            set
            {
                _dust = value;
                Emitter.Emit(PlayerDataEvents.DustChanged);
            }
        }

        private PlayerData()
        {
            Game1.Emitter.AddObserver(Nez.CoreEvents.Exiting, OnExiting);
        }

        public void SaveData()
        {
            var settings = JsonSettings.HandlesReferences;
            settings.TypeNameHandling = TypeNameHandling.All;

            var json = Json.ToJson(this, settings);
            File.WriteAllText("Data/playerData.json", json);
        }

        public void UpdateAndSave()
        {
            SaveData();
        }

        private static PlayerData LoadData()
        {
            if (File.Exists("Data/playerData.json"))
            {
                var json = File.ReadAllText("Data/playerData.json");
                _instance = Json.FromJson<PlayerData>(json);
            }
            else
                _instance = new PlayerData();

            return _instance;
        }

        void OnExiting()
        {
            SaveData();
        }
    }

    public enum PlayerDataEvents
    {
        DollahsChanged,
        DustChanged
    }
}
