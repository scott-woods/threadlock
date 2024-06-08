using Nez.Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components.EnemyActions;

namespace Threadlock.StaticData
{
    public class AllEnemyActions
    {
        static readonly Lazy<Dictionary<string, EnemyAction3>> _enemyActionDictionary = new Lazy<Dictionary<string, EnemyAction3>>(() =>
        {
            var dict = new Dictionary<string, EnemyAction3>();

            if (File.Exists("Content/Data/EnemyActions.json"))
            {
                var json = File.ReadAllText("Content/Data/EnemyActions.json");
                var enemyActions = Json.FromJson<EnemyAction3[]>(json);
                foreach (var action in enemyActions)
                    dict.Add(action.Name, action);
            }

            return dict;
        });

        public static bool TryGetAction(string name, out EnemyAction3 action)
        {
            return _enemyActionDictionary.Value.TryGetValue(name, out action);
        }
    }
}
