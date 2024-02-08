using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Entities.Characters.Enemies.ChainBot;
using Threadlock.Entities.Characters.Enemies.Ghoul;
using Threadlock.Entities.Characters.Enemies.OrbMage;
using Threadlock.Entities.Characters.Enemies.Spitter;
using Threadlock.Models;

namespace Threadlock.StaticData
{
    public class Areas
    {
        // Lazy initialization
        private static readonly Lazy<DungeonArea> _forge = new Lazy<DungeonArea>(() => new DungeonArea
        {
            Name = "Forge",
            EnemyTypes = new List<Type> { typeof(Spitter), typeof(Spitter) }
        });
        public static DungeonArea Forge => _forge.Value;

        // Private dictionary for area lookup
        private static readonly Dictionary<string, DungeonArea> _areaDictionary = new Dictionary<string, DungeonArea>
        {
            { "Forge", Forge }
        };

        // Method to get area by string
        public static bool TryGetArea(string areaName, out DungeonArea area)
        {
            return _areaDictionary.TryGetValue(areaName, out area);
        }
    }
}
