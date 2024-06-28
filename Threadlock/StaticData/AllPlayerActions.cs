using Nez.Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components.EnemyActions;
using Threadlock.Entities.Characters.Player.PlayerActions;

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

        public static bool TryGetAction(string name, out PlayerAction2 action)
        {
            if (_playerActionDictionary.Value.TryGetValue(name, out var actionTemplate))
            {
                action = actionTemplate.Clone() as PlayerAction2;
                return true;
            }

            action = null;
            return false;
        }
    }
}
