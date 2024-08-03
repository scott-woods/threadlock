using Nez;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.SaveData;
using Threadlock.StaticData;

namespace Threadlock.Entities.Characters.Player
{
    /// <summary>
    /// keeps track of the player's currently equipped weapon
    /// </summary>
    public class WeaponManager : Component
    {
        PlayerWeapon _currentWeapon;

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            if (PlayerWeapons.TryGetWeapon(PlayerData.Instance.CurrentWeapon, out var weapon))
            {
                _currentWeapon = weapon;
                Entity.AddComponent(weapon);
            }
        }

        public IEnumerator Execute()
        {
            yield return _currentWeapon.Execute();
        }
    }
}
