using Nez;
using Nez.Sprites;
using Nez.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.DebugTools;
using Threadlock.Entities.Characters.Player.PlayerActions;
using Threadlock.Helpers;
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
            [Controls.Instance.Action3] = new ActionSlot(Controls.Instance.Action3),
        };

        public List<ActionSlot> AllActionSlots { get => ActionDictionary.Values.ToList(); }

        //components
        ApComponent _apComponent;

        #region LIFECYCLE

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            if (Entity.TryGetComponent<ApComponent>(out var apComponent))
                _apComponent = apComponent;

            if (PlayerData.Instance.Action1 != null)
                EquipAction(PlayerData.Instance.Action1, Controls.Instance.Action1);
            if (PlayerData.Instance.Action2 != null)
                EquipAction(PlayerData.Instance.Action2, Controls.Instance.Action2);
            if (PlayerData.Instance.Action3 != null)
                EquipAction(PlayerData.Instance.Action3, Controls.Instance.Action3);
        }

        #endregion

        public bool EquipAction(string actionName, VirtualButton button)
        {
            //see if this is already equipped
            var existingActionSlot = AllActionSlots.FirstOrDefault(s => s.Action?.Name == actionName);
            if (existingActionSlot != null)
            {
                //see if it's on the same button
                if (existingActionSlot.Button == button)
                {
                    Emitter.Emit(ActionManagerEvents.ActionsChanged);
                    return true;
                }
                else
                {
                    //get the slot we want to take
                    var swapSlot = ActionDictionary[button];

                    //replace the action in that slot with this one, returning the action that was previously there
                    var swapAction = swapSlot.ReplaceAction(existingActionSlot.Action);

                    //replace the action that was in the existing slot with the one we just replaced
                    existingActionSlot.ReplaceAction(swapAction);

                    Emitter.Emit(ActionManagerEvents.ActionsChanged);
                    return true;
                }
            }

            //if not already equipped, get action
            if (AllPlayerActions.TryGetAction(actionName, out var action))
            {
                if (ActionDictionary.ContainsKey(button))
                {
                    ActionDictionary[button].EquipAction(action);
                    Emitter.Emit(ActionManagerEvents.ActionsChanged);

                    var animator = Player.Instance.GetComponent<SpriteAnimator>();

                    //load animations
                    action.LoadAnimations(ref animator);

                    return true;
                }
            }

            return false;
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

        bool CanAffordAction(PlayerAction2 action)
        {
            if (DebugSettings.FreeActions)
                return true;
            return action.ApCost <= _apComponent.ActionPoints;
        }
    }

    public enum ActionManagerEvents
    {
        ActionsChanged
    }

    public class ActionSlot
    {
        public PlayerAction2 Action { get; private set; }
        public VirtualButton Button { get; private set; }

        public ActionSlot(VirtualButton button)
        {
            Button = button;
        }

        public void EquipAction(PlayerAction2 action)
        {
            if (Action != null && Action == action)
                return;

            Action = action;
        }

        public PlayerAction2 ReplaceAction(PlayerAction2 action)
        {
            var prevAction = Action;

            Action = action;

            return prevAction;
        }

        public void UnequipAction()
        {
            Action = null;
        }
    }
}
