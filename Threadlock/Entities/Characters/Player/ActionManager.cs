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

        public bool EquipAction(PlayerActionType actionType, VirtualButton button)
        {
            var existingAction = AllActionSlots.FirstOrDefault(s => s.Action?.GetType() == actionType.ToType());
            if (existingAction != null)
            {
                if (existingAction.Button == button)
                {
                    Emitter.Emit(ActionManagerEvents.ActionsChanged);
                    return true;
                }
                else
                {
                    var swapSlot = ActionDictionary[button];
                    var swapAction = swapSlot.ReplaceAction(existingAction.Action);
                    existingAction.ReplaceAction(swapAction);

                    Emitter.Emit(ActionManagerEvents.ActionsChanged);
                    return true;
                }
            }

            PlayerAction actionInstance = existingAction?.Action ?? (PlayerAction)Activator.CreateInstance(actionType.ToType(), button);

            if (ActionDictionary.ContainsKey(button))
            {
                if (ActionDictionary[button].Action != null && ActionDictionary[button].Action.State != PlayerActionState.None)
                    return false;

                ActionDictionary[button].EquipAction(actionInstance);
                Emitter.Emit(ActionManagerEvents.ActionsChanged);
                return true;
            }

            return false;
        }

        public void UnequipAction(VirtualButton button)
        {
            if (ActionDictionary.ContainsKey(button))
            {
                if (ActionDictionary[button].Action != null && ActionDictionary[button].Action.State != PlayerActionState.None)
                    return;

                ActionDictionary[button].UnequipAction();
                Emitter.Emit(ActionManagerEvents.ActionsChanged);
            }
        }

        /// <summary>
        /// see if we can perform any equipped actions, and return the necessary slot
        /// </summary>
        /// <param name="actionSlot"></param>
        /// <returns></returns>
        public bool TryAction(bool pressedOnly, out ActionSlot actionSlot)
        {
            foreach (var pair in ActionDictionary)
            {
                if (pair.Value.Action != null && (pressedOnly ? pair.Value.Button.IsPressed : pair.Value.Button.IsDown) && CanAffordAction(pair.Value.Action))
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

        /// <summary>
        /// returns the first empty action slot, or null if full
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public bool TryGetEmptySlot(out ActionSlot slot)
        {
            slot = null;

            foreach (var actionSlot in AllActionSlots)
            {
                if (actionSlot.Action == null)
                {
                    slot = actionSlot;
                    return true;
                }
            }

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
            if (Action != null && Action == action)
                return;

            Action?.RemoveComponent(Action);
            Action = action;
            Player.Instance.AddComponent(Action);
        }

        public PlayerAction ReplaceAction(PlayerAction action)
        {
            var prevAction = Action;

            Action = action;

            return prevAction;
        }

        public void UnequipAction()
        {
            Action?.RemoveComponent(Action);
            Action = null;
        }
    }
}
