using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Entities.Characters.Player;
using Threadlock.Entities.Characters.Player.PlayerActions;
using Threadlock.Models;
using Threadlock.UI.Canvases;
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

            _bobber = new Bobber(Position);
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
