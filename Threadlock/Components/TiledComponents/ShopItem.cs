using Nez.Sprites;
using Nez;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Entities.Characters.Player.PlayerActions;
using Threadlock.Entities.Characters.Player;
using Threadlock.Entities;
using Threadlock.Models;
using Threadlock.UI.Elements;
using Microsoft.Xna.Framework;
using Threadlock.SaveData;

namespace Threadlock.Components.TiledComponents
{
    public class ShopItem : TiledComponent, IUpdatable
    {
        const int _minCost = 100;
        const int _maxCost = 225;
        const int _increment = 5;

        public PlayerActionType ActionType;

        int _cost;

        //components
        SpriteRenderer _renderer;
        ButtonPrompt _prompt;

        Bobber _bobber;

        InfoModal _modal;

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            //get all action types
            var actionTypes = PlayerActionType.GetAllActionTypes();

            //get others
            var otherItems = Entity.Scene.FindComponentsOfType<ShopItem>()
                .Where(i => i != this && i.ActionType != null)
                .Select(i => i.ActionType)
                .ToList();

            var possibleActions = actionTypes.Where(a => !otherItems.Any(i => i.TypeName == a.TypeName)).ToList();

            //pick an action type
            ActionType = possibleActions.RandomItem();

            //decide on cost
            var numberOfSteps = (_maxCost - _minCost) / _increment + 1;
            var randomIndex = Nez.Random.NextInt(numberOfSteps);
            _cost = _minCost + (randomIndex * _increment);

            //renderer
            var iconId = PlayerActionUtils.GetIconName(ActionType.ToType());
            var path = @$"Content\Textures\UI\Icons\Style3\Style 3 Icon {iconId}.png";
            var texture = Entity.Scene.Content.LoadTexture(path);
            _renderer = Entity.AddComponent(new SpriteRenderer(texture));

            _bobber = new Bobber(Entity.Position);

            var cost = Entity.AddComponent(new TextComponent());
            cost.SetText(_cost.ToString());
            cost.SetLocalOffset(new Vector2(cost.Width / -2, (_renderer.Height / 2) + 5));

            //shadow
            var shadow = Entity.AddComponent(new Shadow(_renderer));
            shadow.SetLocalOffset(new Vector2(0, _renderer.Height * .7f));

            //button prompt
            _prompt = Entity.AddComponent(new ButtonPrompt(20, _renderer));
            _prompt.OnClicked += OnClicked;
            _prompt.OnEntered += OnEntered;
            _prompt.OnExited += OnExited;

            var modalHeader = PlayerActionUtils.GetName(ActionType.ToType());
            var modalBody = PlayerActionUtils.GetDescription(ActionType.ToType());
            var modalAnchor = Entity.Position + new Vector2(0, ((_renderer.Height / 2) + (_renderer.Height * .2f) + 10) * -1);
            _modal = Entity.Scene.CreateEntity("modal").AddComponent(new InfoModal(modalAnchor, modalHeader, modalBody));
            _modal.SetEnabled(false);
        }

        public override void OnRemovedFromEntity()
        {
            base.OnRemovedFromEntity();

            _modal?.Entity?.Destroy();
            _modal = null;
        }

        public void Update()
        {
            var nextPos = _bobber.GetNextPosition();
            var movement = nextPos - Entity.Position;
            _renderer.SetLocalOffset(movement);
        }

        void OnClicked()
        {
            if (PlayerData.Instance.Dollahs < _cost)
            {
                Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds._021_Decline_01);
                return;
            }

            if (Player.Instance.TryGetComponent<ActionManager>(out var actionManager))
            {
                if (actionManager.AllActionSlots.Any(s => s.Action != null && s.Action.GetType() == ActionType.ToType()))
                {
                    Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds._021_Decline_01);
                    return;
                }
                if (actionManager.TryGetEmptySlot(out var slot))
                {
                    Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds.Purchase);
                    PlayerData.Instance.Dollahs -= _cost;
                    actionManager.EquipAction(ActionType, slot.Button);
                    Entity.Destroy();
                    return;
                }
                else
                    Game1.StartCoroutine(Replace());
            }
        }

        void OnEntered()
        {
            _modal.SetEnabled(true);
        }

        void OnExited()
        {
            _modal.SetEnabled(false);
        }

        IEnumerator Replace()
        {
            if (Player.Instance.TryGetComponent<ActionManager>(out var actionManager))
            {
                //get data from existing action in slot
                var lastSlot = actionManager.AllActionSlots.Last();
                var actionType = PlayerActionType.FromType(lastSlot.Action.GetType());

                //equip new action, overwriting what was there
                var success = actionManager.EquipAction(ActionType, lastSlot.Button);

                if (success)
                {
                    Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds.Purchase);
                    PlayerData.Instance.Dollahs -= _cost;

                    //wait one frame before making new action pickup
                    yield return null;

                    var pickup = Entity.Scene.AddEntity(new ActionPickup(actionType));
                    pickup.SetPosition(Entity.Position);

                    Entity.Destroy();
                }
                else
                {
                    Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds._021_Decline_01);
                }
            }
        }
    }
}
