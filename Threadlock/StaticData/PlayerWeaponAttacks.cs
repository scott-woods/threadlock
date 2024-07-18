using Nez;
using Nez.Persistence;
using Nez.Sprites;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.StaticData
{
    public class PlayerWeaponAttacks
    {
        static readonly Lazy<Dictionary<string, PlayerWeaponAttack>> _playerWeaponAttackDictionary = new Lazy<Dictionary<string, PlayerWeaponAttack>>(() =>
        {
            var dict = new Dictionary<string, PlayerWeaponAttack>();

            if (File.Exists("Content/Data/PlayerWeaponAttacks.json"))
            {
                var json = File.ReadAllText("Content/Data/PlayerWeaponAttacks.json");
                var playerWeaponAttacks = Json.FromJson<PlayerWeaponAttack[]>(json);
                foreach (var attack in playerWeaponAttacks)
                    dict.Add(attack.Name, attack);
            }

            return dict;
        });

        public static PlayerWeaponAttack GetBasePlayerWeaponAttack(string name)
        {
            return _playerWeaponAttackDictionary.Value.GetValueOrDefault(name);
        }

        public static bool TryGetBasePlayerWeaponAttack(string name, out PlayerWeaponAttack attack)
        {
            return _playerWeaponAttackDictionary.Value.TryGetValue(name, out attack);
        }

        public static bool TryCreatePlayerWeaponAttack(string name, Entity context, out PlayerWeaponAttack attack)
        {
            attack = null;

            if (_playerWeaponAttackDictionary.Value.TryGetValue(name, out attack))
            {
                attack = attack.Clone() as PlayerWeaponAttack;
                attack.Context = context;

                if (context.TryGetComponent<SpriteAnimator>(out var animator))
                    attack.LoadAnimations(ref animator);

                return true;
            }

            return false;
        }

        public static PlayerWeaponAttack CreatePlayerWeaponAttack(string name, Entity context)
        {
            PlayerWeaponAttack attack = null;

            if (_playerWeaponAttackDictionary.Value.TryGetValue(name, out attack))
            {
                attack = attack.Clone() as PlayerWeaponAttack;
                attack.Context = context;

                if (context.TryGetComponent<SpriteAnimator>(out var animator))
                    attack.LoadAnimations(ref animator);
            }

            return attack;
        }
    }
}
