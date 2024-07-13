using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Entities.Characters.Player;
using Threadlock.SaveData;
using Threadlock.StaticData;

namespace Threadlock
{
    public class PlayerWeapon
    {
        public string Name;

        public PlayerWeaponAttack PrimaryAttack;
        public PlayerWeaponAttack SecondaryAttack;

        PlayerWeaponAttack _queuedAttack;

        public PlayerWeapon(PlayerWeaponData data)
        {
            Name = data.Name;

            if (PlayerWeaponAttacks.TryCreatePlayerWeaponAttack(data.PrimaryAttack, Player.Instance, out PrimaryAttack))
                PrimaryAttack.Button = Controls.Instance.Melee;
            if (PlayerWeaponAttacks.TryCreatePlayerWeaponAttack(data.SecondaryAttack, Player.Instance, out SecondaryAttack))
                SecondaryAttack.Button = Controls.Instance.AltAttack;
        }

        public bool Poll()
        {
            if (PrimaryAttack != null && Controls.Instance.Melee.IsPressed)
            {
                _queuedAttack = PrimaryAttack;
                return true;
            }
            else if (SecondaryAttack != null && Controls.Instance.AltAttack.IsPressed)
            {
                _queuedAttack = SecondaryAttack;
                return true;
            }

            return false;
        }

        public IEnumerator Execute()
        {
            if (_queuedAttack == null)
                yield break;

            yield return _queuedAttack.Execute();
        }
    }

    public class PlayerWeaponData
    {
        public string Name;

        public string PrimaryAttack;
        public string SecondaryAttack;
    }
}
