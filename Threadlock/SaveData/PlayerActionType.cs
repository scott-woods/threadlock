using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.SaveData
{
    /// <summary>
    /// wrapper class to save actions as Types
    /// </summary>
    public class PlayerActionType
    {
        public string TypeName;

        public static PlayerActionType FromType(Type type)
        {
            return new PlayerActionType() { TypeName = type.AssemblyQualifiedName };
        }

        public Type ToType()
        {
            return Type.GetType(TypeName);
        }
    }
}
