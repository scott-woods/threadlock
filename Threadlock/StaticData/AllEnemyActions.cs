using Nez;
using Nez.Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Actions;

namespace Threadlock.StaticData
{
    public class AllEnemyActions
    {
        static readonly Lazy<Dictionary<string, EnemyAction>> _enemyActionDictionary = new Lazy<Dictionary<string, EnemyAction>>(() =>
        {
            var dict = new Dictionary<string, EnemyAction>();

            if (File.Exists("Content/Data/EnemyActions.json"))
            {
                var json = File.ReadAllText("Content/Data/EnemyActions.json");
                var enemyActions = Json.FromJson<EnemyAction[]>(json);
                foreach (var action in enemyActions)
                    dict.Add(action.Name, action);
            }

            return dict;
        });

        public static EnemyAction GetBaseEnemyAction(string name)
        {
            return _enemyActionDictionary.Value.GetValueOrDefault(name);
        }

        public static bool TryGetBaseEnemyAction(string name, out EnemyAction action)
        {
            return _enemyActionDictionary.Value.TryGetValue(name, out action);
        }

        public static EnemyAction CreateEnemyAction(string name, Entity context)
        {
            EnemyAction action = null;

            if (_enemyActionDictionary.Value.TryGetValue(name, out action))
            {
                action = action.Clone() as EnemyAction;
                action.Context = context;
            }

            return action;
        }

        /// <summary>
        /// Get a clone of an enemy action by name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static bool TryCreateEnemyAction(string name, Entity context, out EnemyAction action)
        {
            action = null;

            if (_enemyActionDictionary.Value.TryGetValue(name, out action))
            {
                action = action.Clone() as EnemyAction;
                action.Context = context;
                return true;
            }

            return false;
        }
    }
}
