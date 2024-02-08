using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.DebugTools;
using Threadlock.Entities.Characters.Player.PlayerActions;
using Threadlock.StaticData;

namespace Threadlock.Entities.Characters.Player
{
    public class ActionManager : Component
    {
        public KeyValuePair<VirtualButton, PlayerAction>? CurrentAction { get; private set; }

        Dictionary<VirtualButton, PlayerAction> _actionDictionary = new Dictionary<VirtualButton, PlayerAction>();

        //components
        ApComponent _apComponent;

        public ActionManager(Dictionary<VirtualButton, PlayerAction> actionDictionary)
        {
            _actionDictionary = actionDictionary;
        }

        #region LIFECYCLE

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            if (Entity.TryGetComponent<ApComponent>(out var apComponent))
                _apComponent = apComponent;
        }

        #endregion

        public bool CanPerformAction()
        {
            foreach (var pair in _actionDictionary)
            {
                if (pair.Key.IsPressed && CanAffordAction(pair.Value))
                {
                    CurrentAction = pair;
                    return true;
                }
            }

            CurrentAction = null;
            return false;
        }

        bool CanAffordAction(PlayerAction action)
        {
            if (DebugSettings.FreeActions)
                return true;
            var cost = PlayerActionUtils.GetApCost(action.GetType());
            return cost <= _apComponent.ActionPoints;
        }
    }
}
