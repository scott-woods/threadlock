using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nez;
using Nez.Persistence;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components.EnemyActions;
using Threadlock.Entities;
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

                    var method = typeof(JsonConvert).GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m => m.Name == "DeserializeObject" &&
                    m.IsGenericMethod &&
                    m.GetParameters().Length == 2 &&
                    m.GetParameters()[0].ParameterType == typeof(string) &&
                    m.GetParameters()[1].ParameterType == typeof(JsonSerializerSettings));

                    if (method == null)
                        throw new InvalidOperationException();

                    var genericMethod = method.MakeGenericMethod(projectileType);

                    var settings = new JsonSerializerSettings();
                    settings.Converters.Add(new HitEffectConverter());
                    settings.Converters.Add(new Vector2Converter());

                    var config = genericMethod.Invoke(null, new object[] { jObject.ToString(), settings }) as ProjectileConfig2;
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

    public class Vector2Converter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Vector2);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jo = JObject.Load(reader);
            var x = jo["X"].Value<float>();
            var y = jo["Y"].Value<float>();
            return new Vector2(x, y);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var vector = (Vector2)value;
            writer.WriteStartObject();
            writer.WritePropertyName("X");
            writer.WriteValue(vector.X);
            writer.WritePropertyName("Y");
            writer.WriteValue(vector.Y);
            writer.WriteEndObject();
        }
    }

    public class HitEffectConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(IHitEffect2).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);
            var typeString = jObject["Type"]?.Value<string>();

            if (typeString == null)
                throw new ArgumentException("Type property is missing in the JSON object");

            typeString += "HitEffect";

            var hitEffectType = Assembly.GetExecutingAssembly().GetTypes()
                .FirstOrDefault(t => t.Name == typeString && typeof(IHitEffect2).IsAssignableFrom(t));

            if (hitEffectType == null)
                return null;

            var method = typeof(JsonConvert).GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m => m.Name == "DeserializeObject" &&
                    m.IsGenericMethod &&
                    m.GetParameters().Length == 2 &&
                    m.GetParameters()[0].ParameterType == typeof(string) &&
                    m.GetParameters()[1].ParameterType == typeof(JsonSerializerSettings));

            if (method == null)
                throw new InvalidOperationException();

            var genericMethod = method.MakeGenericMethod(hitEffectType);

            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new Vector2Converter());

            var effect = genericMethod.Invoke(null, new object[] { jObject.ToString(), settings }) as IHitEffect2;

            serializer.Populate(jObject.CreateReader(), effect);
            return effect;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
