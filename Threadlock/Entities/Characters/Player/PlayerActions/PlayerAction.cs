using Microsoft.Xna.Framework.Graphics;
using Nez;
using Nez.Persistence;
using Nez.Systems;
using Nez.Textures;
using Nez.Tweens;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.DebugTools;

namespace Threadlock.Entities.Characters.Player.PlayerActions
{
    public abstract class PlayerAction : Component, IUpdatable
    {
        public PlayerActionState State = PlayerActionState.None;

        ICoroutine _prepareCoroutine;
        ICoroutine _executeCoroutine;

        #region LIFECYCLE

        public virtual void Update()
        {

        }

        #endregion

        public IEnumerator Prepare()
        {
            State = PlayerActionState.Preparing;
            _prepareCoroutine = Game1.StartCoroutine(PreparationCoroutine());
            yield return _prepareCoroutine;
            _prepareCoroutine = null;
            State = PlayerActionState.None;
        }

        public IEnumerator Execute()
        {
            //update state
            State = PlayerActionState.Executing;

            //remove ap points
            if (!DebugSettings.FreeActions)
            {
                if (Entity.TryGetComponent<ApComponent>(out var apComponent))
                {
                    var cost = PlayerActionUtils.GetApCost(this.GetType());
                    apComponent.ActionPoints -= cost;
                }
            }

            _executeCoroutine = Game1.StartCoroutine(ExecutionCoroutine());
            yield return _executeCoroutine;
            _executeCoroutine = null;

            State = PlayerActionState.None;
        }

        public Texture2D GetIconTexture()
        {
            var iconId = PlayerActionUtils.GetIconName(GetType());
            var path = @$"Content\Textures\UI\Icons\Style3\Style 3 Icon {iconId}.png";
            var texture = Entity.Scene.Content.LoadTexture(path);
            return texture;
        }

        public abstract IEnumerator ExecutionCoroutine();

        public abstract IEnumerator PreparationCoroutine();

        /// <summary>
        /// called when the action ends, successfully or not. do any cleanup here
        /// </summary>
        public virtual void Reset()
        {
            State = PlayerActionState.None;

            _prepareCoroutine?.Stop();
            _prepareCoroutine = null;

            _executeCoroutine?.Stop();
            _executeCoroutine = null;
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
