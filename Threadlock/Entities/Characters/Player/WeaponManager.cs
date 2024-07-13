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
    public class WeaponManager : Component
    {
        PlayerWeapon _currentWeapon;

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            if (PlayerWeapons.TryGetWeapon(PlayerData.Instance.CurrentWeapon, out var weapon))
            {
                _currentWeapon = weapon;
            }
        }

        public bool Poll()
        {
            if (_currentWeapon == null)
                return false;

            return _currentWeapon.Poll();
        }

        public IEnumerator Execute()
        {
            yield return _currentWeapon.Execute();
        }
    }
}
