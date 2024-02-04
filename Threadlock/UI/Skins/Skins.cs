using Microsoft.Xna.Framework;
using Nez.Textures;
using Nez.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.UI.Skins
{
    public static class Skins
    {
        public static Skin GetDefaultSkin()
        {
            var skin = new Skin();

            //load sprite atlas assets
            skin.AddSprites(Game1.Content.LoadSpriteAtlas("Content/Textures/UI/Icons/Style3/Atlas/icons_style_3.atlas"));
            skin.AddSprites(Game1.Content.LoadSpriteAtlas("Content/Textures/UI/Icons/Style4/Atlas/icons_style_4.atlas"));
            skin.AddSprites(Game1.Content.LoadSpriteAtlas("Content/Textures/UI/Menu/Style3/Atlas/menu_style_3.atlas"));

            //load fonts
            skin.Add("font_abaddon_light_12", Game1.Content.LoadBitmapFont(Nez.Content.Fonts.Abaddon_light_12));
            skin.Add("font_abaddon_light_18", Game1.Content.LoadBitmapFont(Nez.Content.Fonts.Abaddon_light_18));
            skin.Add("font_abaddon_light_24", Game1.Content.LoadBitmapFont(Nez.Content.Fonts.Abaddon_light_24));
            skin.Add("font_abaddon_light_36", Game1.Content.LoadBitmapFont(Nez.Content.Fonts.Abaddon_light_36));
            skin.Add("font_abaddon_light_48", Game1.Content.LoadBitmapFont(Nez.Content.Fonts.Font_abaddon_light_48));
            skin.Add("font_abaddon_light_60", Game1.Content.LoadBitmapFont(Nez.Content.Fonts.Abaddon_light_60));
            skin.Add("font_abaddon_light_72", Game1.Content.LoadBitmapFont(Nez.Content.Fonts.Abaddon_light_72));

            //player health bar
            var playerHealthTexture = Game1.Content.LoadTexture(Nez.Content.Textures.UI.PlayerHealthBar);
            var playerHealthBgSprite = new Sprite(playerHealthTexture, new Rectangle(0, 0, 64, 16));
            var playerHealthKnobBeforeSprite = new Sprite(playerHealthTexture, new Rectangle(0, 16, 64, 16));
            var playerHealthBg = new SpriteDrawable(playerHealthBgSprite);
            playerHealthBg.MinHeight = 48;
            playerHealthBg.LeftWidth = 1;
            playerHealthBg.RightWidth = 1;
            var playerHealthKnobBefore = new SpriteDrawable(playerHealthKnobBeforeSprite);
            playerHealthKnobBefore.MinHeight = 48;
            playerHealthKnobBefore.MinWidth = 0;
            skin.Add("playerHealthBar", new ProgressBarStyle()
            {
                Background = playerHealthBg,
                KnobBefore = playerHealthKnobBefore,
            });

            //labels
            List<int> fontSizes = new List<int> { 12, 18, 24, 36, 48, 60, 72 };
            foreach (var size in fontSizes)
            {
                skin.Add($"abaddon_{size}", new LabelStyle()
                {
                    Font = skin.GetFont($"font_abaddon_light_{size}"),
                    FontColor = Color.White
                });
            }

            return skin;
        }
    }
}
