using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nez.Persistence;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components.EnemyActions;
using Threadlock.Helpers;

namespace Threadlock.StaticData
{
    public class Projectiles2
    {
        static readonly Lazy<Dictionary<string, ProjectileConfig2>> _projectileDictionary = new Lazy<Dictionary<string, ProjectileConfig2>>(() =>
        {
            var dict = new Dictionary<string, ProjectileConfig2>();

            if (File.Exists("Content/Data/Projectiles.json"))
            {
                var json = File.ReadAllText("Content/Data/Projectiles.json");
                var jArray = JArray.Parse(json);

                foreach (var jToken in jArray)
                {
                    var jObject = (JObject)jToken;
                    var typeString = jObject["Type"]?.ToString();
                    if (typeString == null)
                        throw new ArgumentException("Type property is missing in the JSON object");

                    typeString += "ProjectileConfig";

                    var projectileType = Assembly.GetExecutingAssembly().GetTypes()
                        .FirstOrDefault(t => t.Name == typeString && typeof(ProjectileConfig2).IsAssignableFrom(t));

                    if (projectileType == null)
                        return null;

                    var method = typeof(Json).GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m => m.Name == "FromJson" && m.IsGenericMethod);

                    if (method == null)
                        throw new InvalidOperationException();

                    var genericMethod = method.MakeGenericMethod(projectileType);

                    var config = genericMethod.Invoke(null, new object[] { jObject.ToString(), null }) as ProjectileConfig2;
                    dict.Add(config.Name, config);
                }
            }

            return dict;
        });

        public static bool TryGetProjectile(string name, out ProjectileConfig2 projectile)
        {
            return _projectileDictionary.Value.TryGetValue(name, out projectile);
        }
    }
}
