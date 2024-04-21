using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Entities.Characters.Player.PlayerActions;
using Threadlock.Models;

namespace Threadlock.Entities
{
    public class ActionPickup : Entity
    {
        SpriteRenderer _renderer;
        ButtonPrompt _prompt;

        Bobber _bobber;

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            var action = PlayerActionType.FromType<ChainLightning>();
            var iconId = PlayerActionUtils.GetIconName(action.ToType());
            var path = @$"Content\Textures\UI\Icons\Style3\Style 3 Icon {iconId}.png";
            var texture = Scene.Content.LoadTexture(path);
            _renderer = AddComponent(new SpriteRenderer(texture));

            var shadow = AddComponent(new Shadow(_renderer));
            shadow.SetLocalOffset(new Vector2(0, _renderer.Height * .7f));

            _prompt = AddComponent(new ButtonPrompt(32, _renderer));

            _bobber = new Bobber(Position);
        }

        public override void Update()
        {
            base.Update();

            var nextPos = _bobber.GetNextPosition();
            var movement = nextPos - Position;
            _renderer.SetLocalOffset(movement);
        }
    }
}
