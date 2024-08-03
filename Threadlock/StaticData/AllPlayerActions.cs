using Nez;
using Nez.Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using Threadlock.Actions;

namespace Threadlock.StaticData
{
    public class AllPlayerActions
    {
        static readonly Lazy<Dictionary<string, PlayerAction2>> _playerActionDictionary = new Lazy<Dictionary<string, PlayerAction2>>(() =>
        {
            var dict = new Dictionary<string, PlayerAction2>();

            if (File.Exists("Content/Data/PlayerActions.json"))
            {
                var json = File.ReadAllText("Content/Data/PlayerActions.json");
                var playerActions = Json.FromJson<PlayerAction2[]>(json);
                foreach (var action in playerActions)
                    dict.Add(action.Name, action);
            }

            return dict;
        });

        public static PlayerAction2 GetBasePlayerAction(string name)
        {
            return _playerActionDictionary.Value.GetValueOrDefault(name);
        }

        public static bool TryGetBasePlayerAction(string name, out PlayerAction2 action)
        {
            return _playerActionDictionary.Value.TryGetValue(name, out action);
        }

        public static PlayerAction2 CreatePlayerAction(string name, Entity context)
        {
            PlayerAction2 action = null;

            if (_playerActionDictionary.Value.TryGetValue(name, out action))
            {
                action = action.Clone() as PlayerAction2;
                action.Context = context;
            }

            return action;
        }

        /// <summary>
        /// Get a clone of a player action by name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static bool TryCreatePlayerAction(string name, Entity context, out PlayerAction2 action)
        {
            action = null;

            if (_playerActionDictionary.Value.TryGetValue(name, out action))
            {
                action = action.Clone() as PlayerAction2;
                action.Context = context;
                return true;
            }

            return false;
        }
    }
}
