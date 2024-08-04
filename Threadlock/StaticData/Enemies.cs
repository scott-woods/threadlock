using Microsoft.Xna.Framework;
using Nez.Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using Threadlock.Models;

namespace Threadlock.StaticData
{
    public class Enemies
    {
        static readonly Lazy<Dictionary<string, EnemyConfig>> _enemyConfigDictionary = new Lazy<Dictionary<string, EnemyConfig>>(() =>
        {
            var dict = new Dictionary<string, EnemyConfig>();

            if (File.Exists("Content/Data/Enemies.json"))
            {
                var json = File.ReadAllText("Content/Data/Enemies.json");
                var settings = new JsonSettings();
                var enemyConfigs = Json.FromJson<EnemyConfig[]>(json, settings);
                foreach (var config in enemyConfigs)
                    dict.Add(config.Name, config);
            }

            return dict;
        });

        public static Dictionary<string, EnemyConfig> EnemyConfigDictionary => _enemyConfigDictionary.Value;
    }
}
