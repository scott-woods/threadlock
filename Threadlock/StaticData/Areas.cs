using Nez.Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using Threadlock.Models;

namespace Threadlock.StaticData
{
    public class Areas
    {
        static readonly Lazy<Dictionary<string, Area>> _areaDictionary = new Lazy<Dictionary<string, Area>>(() =>
        {
            var dict = new Dictionary<string, Area>();

            if (File.Exists("Content/Data/Areas.json"))
            {
                var json = File.ReadAllText("Content/Data/Areas.json");
                var areas = Json.FromJson<Area[]>(json);
                foreach (var area in areas)
                    dict.Add(area.Name, area);
            }

            return dict;
        });

        public static Dictionary<string, Area> AreaDictionary => _areaDictionary.Value;

        //// Lazy initialization
        //private static readonly Lazy<DungeonArea> _forge = new Lazy<DungeonArea>(() => new DungeonArea
        //{
        //    Name = "Forge",
        //    EnemyTypes = new List<Type> { typeof(ChainBot), typeof(Spitter), typeof(Ghoul), typeof(OrbMage) }
        //});
        //public static DungeonArea Forge => _forge.Value;

        //// Private dictionary for area lookup
        //private static readonly Dictionary<string, DungeonArea> _areaDictionary = new Dictionary<string, DungeonArea>
        //{
        //    { "Forge", Forge }
        //};

        //// Method to get area by string
        //public static bool TryGetArea(string areaName, out DungeonArea area)
        //{
        //    return _areaDictionary.TryGetValue(areaName, out area);
        //}
    }
}
