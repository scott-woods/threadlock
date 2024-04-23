using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using Nez.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Entities.Characters.Player;
using Threadlock.Entities.Characters.Player.PlayerActions;
using Threadlock.Helpers;
using Threadlock.Models;
using Threadlock.StaticData;
using Threadlock.UI.Canvases;
using Threadlock.UI.Elements;
using Threadlock.UI.Skins;
using static Nez.Content.Textures.UI;

namespace Threadlock.Entities
{
    public class ActionPickup : Entity
    {
        //components
        SpriteRenderer _renderer;
        ButtonPrompt _prompt;

        PlayerActionType _actionType;
        Bobber _bobber;

        InfoModal _modal;

        public ActionPickup(PlayerActionType actionType)
        {
            _actionType = actionType;
        }

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            var iconId = PlayerActionUtils.GetIconName(_actionType.ToType());
            var path = @$"Content\Textures\UI\Icons\Style3\Style 3 Icon {iconId}.png";
            var texture = Scene.Content.LoadTexture(path);
            _renderer = AddComponent(new SpriteRenderer(texture));

            var shadow = AddComponent(new Shadow(_renderer));
            shadow.SetLocalOffset(new Vector2(0, _renderer.Height * .7f));

            _prompt = AddComponent(new ButtonPrompt(32, _renderer));
            _prompt.OnClicked += OnClicked;
            _prompt.OnEntered += OnEntered;
            _prompt.OnExited += OnExited;

            _bobber = new Bobber(Position);

            var modalHeader = PlayerActionUtils.GetName(_actionType.ToType());
            var modalBody = PlayerActionUtils.GetDescription(_actionType.ToType());
            var modalAnchor = Position + new Vector2(0, ((_renderer.Height / 2) + (_renderer.Height * .2f) + 10) * -1);
            _modal = Scene.CreateEntity("modal").AddComponent(new InfoModal(modalAnchor, modalHeader, modalBody));
            _modal.SetEnabled(false);
        }

        public override void OnRemovedFromScene()
        {
            base.OnRemovedFromScene();

            _modal?.Entity?.Destroy();
            _modal = null;
        }

        public override void Update()
        {
            base.Update();

            var nextPos = _bobber.GetNextPosition();
            var movement = nextPos - Position;
            _renderer.SetLocalOffset(movement);
        }

        void OnClicked()
        {
            if (Player.Instance.TryGetComponent<ActionManager>(out var actionManager))
            {
                if (actionManager.TryGetEmptySlot(out var slot))
                {
                    Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds.Menu_select);
                    actionManager.EquipAction(_actionType, slot.Button);
                    Destroy();
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
                var success = actionManager.EquipAction(_actionType, lastSlot.Button);

                if (success)
                {
                    Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds.Menu_select);

                    //wait one frame before making new action pickup
                    yield return null;

                    var pickup = Scene.AddEntity(new ActionPickup(actionType));
                    pickup.SetPosition(Position);

                    Destroy();
                }
                else
                {
                    Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds._021_Decline_01);
                }
            }
        }
    }
}
