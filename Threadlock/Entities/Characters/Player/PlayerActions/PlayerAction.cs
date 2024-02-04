using Nez;
using Nez.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Entities.Characters.Player.PlayerActions
{
    public abstract class PlayerAction : Component, IUpdatable
    {
        public PlayerActionState State = PlayerActionState.None;
        public event Action OnPreparationFinished;
        public event Action OnExecutionFinished;

        public virtual void Prepare()
        {
            State = PlayerActionState.Preparing;
        }

        public virtual void Execute()
        {
            State = PlayerActionState.Executing;
        }

        public virtual void Update()
        {

        }

        public virtual void Abort()
        {
            State = PlayerActionState.None;
        }

        public virtual void HandlePreparationFinished()
        {
            State = PlayerActionState.None;
            OnPreparationFinished?.Invoke();
        }

        public virtual void HandleExecutionFinished()
        {
            State = PlayerActionState.None;
            OnExecutionFinished?.Invoke();
        }
    }

    public enum PlayerActionState
    {
        None,
        Preparing,
        Executing
    }

    sealed class PlayerActionInfoAttribute : Attribute
    {
        public string Name { get; }
        public int ApCost { get; }
        public string Description { get; }
        public string IconName { get; }

        public PlayerActionInfoAttribute(string name, int apCost, string description, string iconName)
        {
            Name = name;
            ApCost = apCost;
            Description = description;
            IconName = iconName;
        }
    }

    public static class PlayerActionUtils
    {
        public static string GetName(Type actionType)
        {
            var attribute = (PlayerActionInfoAttribute)Attribute.GetCustomAttribute(actionType, typeof(PlayerActionInfoAttribute));
            return attribute?.Name;
        }

        public static int GetApCost(Type actionType)
        {
            var attribute = (PlayerActionInfoAttribute)Attribute.GetCustomAttribute(actionType, typeof(PlayerActionInfoAttribute));
            return attribute?.ApCost ?? 0;
        }

        //public static PlayerActionCategory GetCategory(Type actionType)
        //{
        //    var attribute = (PlayerActionInfoAttribute)Attribute.GetCustomAttribute(actionType, typeof(PlayerActionInfoAttribute));
        //    return attribute.Category;
        //}

        public static string GetDescription(Type actionType)
        {
            var attribute = (PlayerActionInfoAttribute)Attribute.GetCustomAttribute(actionType, typeof(PlayerActionInfoAttribute));
            return attribute?.Description;
        }

        public static string GetIconName(Type actionType)
        {
            var attribute = (PlayerActionInfoAttribute)Attribute.GetCustomAttribute(actionType, typeof(PlayerActionInfoAttribute));
            return attribute?.IconName;
        }
    }
}
