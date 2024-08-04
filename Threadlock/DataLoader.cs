using Nez.Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Models;

namespace Threadlock
{
    public class DataLoader
    {
        public static readonly Dictionary<string, EnemyData> EnemyDataDictionary = LoadEnemyData();

        static Dictionary<string, EnemyData> LoadEnemyData()
        {
            if (File.Exists("Content/Data/EnemyData.json"))
            {
                var json = File.ReadAllText("Content/Data/EnemyData.json");
                var settings = new JsonSettings();
                var enemyConfigs = Json.FromJson<Dictionary<string, EnemyData>>(json, settings);
                return enemyConfigs;
            }
            else
                throw new Exception("Could not find EnemyData.json");
        }
    }
}
