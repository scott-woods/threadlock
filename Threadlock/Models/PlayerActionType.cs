using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Entities.Characters.Player.PlayerActions;

namespace Threadlock.Models
{
    /// <summary>
    /// wrapper class to save actions as Types
    /// </summary>
    public class PlayerActionType
    {
        public string TypeName;

        public static PlayerActionType FromType<T>() where T : PlayerAction
        {
            return new PlayerActionType() { TypeName = typeof(T).AssemblyQualifiedName };
        }

        public Type ToType()
        {
            return Type.GetType(TypeName);
        }
    }
}
