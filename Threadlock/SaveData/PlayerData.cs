using Nez.Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Entities.Characters.Player.PlayerActions;

namespace Threadlock.SaveData
{
    public class PlayerData
    {
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

        public PlayerActionType OffensiveAction1 = PlayerActionType.FromType(typeof(DashAction));
        public PlayerActionType OffensiveAction2 = PlayerActionType.FromType(typeof(Grip));
        public PlayerActionType SupportAction = PlayerActionType.FromType(typeof(Teleport));

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
}
