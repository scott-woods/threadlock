using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Entities.Characters.Player;

namespace Threadlock.Helpers
{
    public class DialogueHelper
    {
        /// <summary>
        /// parse a requirement string and determine if that requirement is met
        /// </summary>
        /// <param name="requirement"></param>
        /// <returns></returns>
        public static bool IsRequirementMet(string requirement)
        {
            var parts = requirement.Split(':');
            var requirementName = parts[0];
            var parameters = parts.Skip(1).ToList();

            switch (requirementName)
            {
                case "HasWeaponEquipped":
                    var currentWeapon = Player.Instance.GetCurrentWeapon();
                    if (currentWeapon == null)
                        return false;
                    var weaponType = Type.GetType($"Threadlock.Entities.Characters.Player.BasicWeapons.{parameters[0]}");
                    if (weaponType == null)
                        return false;

                    return weaponType.IsAssignableFrom(currentWeapon.GetType());
                default:
                    return true;
            }
        }
    }
}
