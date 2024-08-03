using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Entities.Characters.Player;
using Threadlock.SaveData;

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
                    return PlayerData.Instance.CurrentWeapon == parameters[0];
                default:
                    return true;
            }
        }
    }
}
