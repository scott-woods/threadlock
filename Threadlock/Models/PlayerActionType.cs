﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        public static PlayerActionType FromType(Type type)
        {
            return new PlayerActionType() { TypeName = type.AssemblyQualifiedName };
        }

        public Type ToType()
        {
            return Type.GetType(TypeName);
        }

        public static List<PlayerActionType> GetAllActionTypes()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            Type[] actionTypes = assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(PlayerAction)) && !t.IsAbstract)
                .ToArray();

            return actionTypes.Select(t => FromType(t)).ToList();
        }
    }
}
