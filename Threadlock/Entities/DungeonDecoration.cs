using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using Nez.Textures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Models;

namespace Threadlock.Entities
{
    public class DungeonDecoration : Entity
    {
        public DungeonDecoration(Vector2 position, Vector2 size, Sprite sprite, bool rotate = false)
        {
            Position = position;

            var renderer = AddComponent(new SpriteRenderer(sprite));
            var yOffset = 16 - sprite.SourceRect.Height / 2;
            renderer.SetLocalOffset(new Vector2(sprite.SourceRect.Width / 2, yOffset));
            //renderer.SetLocalOffset(new Vector2(sprite.SourceRect.Width / 2, sprite.SourceRect.Height / 2));
        }
    }
}
