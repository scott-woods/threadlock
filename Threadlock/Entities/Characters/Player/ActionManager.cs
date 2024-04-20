using Nez;
using Nez.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.DebugTools;
using Threadlock.Entities.Characters.Player.PlayerActions;
using Threadlock.Models;
using Threadlock.SaveData;
using Threadlock.StaticData;

namespace Threadlock.Entities.Characters.Player
{
    public class ActionManager : Component
    {
        public Emitter<ActionManagerEvents> Emitter = new Emitter<ActionManagerEvents>();

        public Dictionary<VirtualButton, ActionSlot> ActionDictionary = new Dictionary<VirtualButton, ActionSlot>()
        {
            [Controls.Instance.Action1] = new ActionSlot(Controls.Instance.Action1),
            [Controls.Instance.Action2] = new ActionSlot(Controls.Instance.Action2),
            [Controls.Instance.SupportAction] = new ActionSlot(Controls.Instance.SupportAction),
        };

        public List<ActionSlot> AllActionSlots { get => ActionDictionary.Values.ToList(); }

        //components
        ApComponent _apComponent;

        #region LIFECYCLE

        public override void Initialize()
        {
            base.Initialize();

            if (PlayerData.Instance.OffensiveAction1 != null)
                EquipAction(PlayerData.Instance.OffensiveAction1, Controls.Instance.Action1);
            if (PlayerData.Instance.OffensiveAction2 != null)
                EquipAction(PlayerData.Instance.OffensiveAction2, Controls.Instance.Action2);
            if (PlayerData.Instance.SupportAction != null)
                EquipAction(PlayerData.Instance.SupportAction, Controls.Instance.SupportAction);
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            if (Entity.TryGetComponent<ApComponent>(out var apComponent))
                _apComponent = apComponent;
        }

        #endregion

        public void EquipAction(PlayerActionType actionType, VirtualButton button)
        {
            PlayerAction actionInstance = (PlayerAction)Activator.CreateInstance(actionType.ToType(), button);

            if (ActionDictionary.ContainsKey(button))
            {
                if (ActionDictionary[button].Action != null && ActionDictionary[button].Action.State != PlayerActionState.None)
                    return;

                ActionDictionary[button].EquipAction(actionInstance);
                Emitter.Emit(ActionManagerEvents.ActionsChanged);
            }
        }

        /// <summary>
        /// see if we can perform any equipped actions, and return the necessary slot
        /// </summary>
        /// <param name="actionSlot"></param>
        /// <returns></returns>
        public bool TryAction(out ActionSlot actionSlot)
        {
            foreach (var pair in ActionDictionary)
            {
                if (pair.Value.Action != null && pair.Key.IsDown && CanAffordAction(pair.Value.Action))
                {
                    actionSlot = pair.Value;
                    return true;
                }
                //if (pair.Key.IsDown && CanAffordAction(pair.Value))
                //{
                //    CurrentAction = pair;
                //    return true;
                //}
            }

            actionSlot = null;
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

    public enum ActionManagerEvents
    {
        ActionsChanged
    }

    public class ActionSlot
    {
        public PlayerAction Action { get; private set; }
        public VirtualButton Button { get; private set; }

        public ActionSlot(VirtualButton button)
        {
            Button = button;
        }

        public void EquipAction(PlayerAction action)
        {
            Action?.RemoveComponent(Action);
            Action = action;
            Player.Instance.AddComponent(Action);
        }
    }
}
