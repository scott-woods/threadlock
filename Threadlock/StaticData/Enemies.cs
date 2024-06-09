using Microsoft.Xna.Framework;
using Nez.Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components.EnemyActions;
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

    class EnemyConfigTypeConverter : JsonTypeConverter<EnemyConfig>
    {
        public override bool CanWrite => false;

        public override void OnFoundCustomData(EnemyConfig instance, string key, object value)
        {
            switch (key)
            {
                case "hurtboxSize":
                    instance.HurtboxSize = GetVectorFromString(value);
                    break;
                case "colliderSize":
                    instance.ColliderSize = GetVectorFromString(value);
                    break;
                case "colliderOffset":
                    instance.ColliderOffset = GetVectorFromString(value);
                    break;
                case "animatorOffset":
                    instance.AnimatorOffset = GetVectorFromString(value);
                    break;
            }
        }

        public override void WriteJson(IJsonEncoder encoder, EnemyConfig value)
        {
            throw new NotImplementedException();
        }

        Vector2 GetVectorFromString(object obj)
        {
            var str = obj as string;
            if (string.IsNullOrWhiteSpace(str))
                return Vector2.Zero;

            var split = str.Split(' ');
            if (split.Length != 2)
                return Vector2.Zero;

            var x = Convert.ToInt32(split[0]);
            var y = Convert.ToInt32(split[1]);
            return new Vector2(x, y);
        }
    }
}
