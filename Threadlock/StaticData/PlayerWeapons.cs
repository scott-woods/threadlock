using Nez.Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Entities.Characters.Player;

namespace Threadlock.StaticData
{
    public class PlayerWeapons
    {
        static readonly Lazy<Dictionary<string, PlayerWeaponData>> _playerWeaponDictionary = new Lazy<Dictionary<string, PlayerWeaponData>>(() =>
        {
            var dict = new Dictionary<string, PlayerWeaponData>();

            if (File.Exists("Content/Data/PlayerWeapons.json"))
            {
                var json = File.ReadAllText("Content/Data/PlayerWeapons.json");
                var playerWeapons = Json.FromJson<PlayerWeaponData[]>(json);
                foreach (var weapon in playerWeapons)
                    dict.Add(weapon.Name, weapon);
            }

            return dict;
        });

        public static bool TryGetWeapon(string name, out PlayerWeapon weapon)
        {
            weapon = null;

            if (_playerWeaponDictionary.Value.TryGetValue(name, out var data))
            {
                weapon = new PlayerWeapon(data);
                return true;
            }

            return false;
        }

        public static PlayerWeapon GetWeapon(string name)
        {
            if (_playerWeaponDictionary.Value.TryGetValue(name, out var data))
                return new PlayerWeapon(data);

            return null;
        }
    }
}
