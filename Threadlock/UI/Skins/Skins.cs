using Microsoft.Xna.Framework;
using Nez.Textures;
using Nez.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.UI.Drawables;

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

            var menuTexture = Game1.Content.LoadTexture(Nez.Content.Textures.UI.BorderAll10);
            var menuTextureSprite = new Sprite(menuTexture, 64, 64, 64, 64);
            var menuNp = new NinePatchDrawable(menuTextureSprite, 24, 24, 24, 24);
            skin.Add("window_blue", menuNp);

            //player health bar
            var playerHealthTexture = Game1.Content.LoadTexture(Nez.Content.Textures.UI.PixelUIpack3._04);
            var borderSprite = new NinePatchSprite(playerHealthTexture, new Rectangle(0, 0, 48, 16), 16, 16, 0, 0);
            //var borderDrawable = new NinePatchDrawable(borderSprite);
            var borderDrawable = new MaskedNinePatchDrawable(borderSprite);
            //var borderSprite = new Sprite(playerHealthTexture, new Rectangle(0, 0, 48, 16));
            //var borderDrawable = new SpriteDrawable(borderSprite);
            borderDrawable.LeftWidth = 0;
            borderDrawable.RightWidth = 0;
            var barSprite = new NinePatchSprite(playerHealthTexture, new Rectangle(48, 0, 48, 16), 16, 16, 0, 0);
            //var barDrawable = new NinePatchDrawable(barSprite);
            var barDrawable = new MaskedNinePatchDrawable(barSprite);
            //var barSprite = new Sprite(playerHealthTexture, new Rectangle(48, 0, 48, 16));
            //var barDrawable = new SpriteDrawable(barSprite);
            barDrawable.MinWidth = 0;
            //var knobAfterSprite = new Sprite(playerHealthTexture, new Rectangle(0, 48, 48, 16));
            //var knobAfterDrawable = new SpriteDrawable(knobAfterSprite);
            var knobAfterSprite = new NinePatchSprite(playerHealthTexture, new Rectangle(0, 48, 48, 16), 16, 16, 0, 0);
            var knobAfterDrawable = new NinePatchDrawable(knobAfterSprite);
            knobAfterDrawable.MinWidth = 0;
            skin.Add("playerHealthBar", new ProgressBarStyle()
            {
                Background = borderDrawable,
                KnobBefore = barDrawable,
                //KnobAfter = knobAfterDrawable,
            });

            //var playerHealthBgSprite = new Sprite(playerHealthTexture, new Rectangle(0, 0, 48, 16));
            //var playerHealthKnobBeforeSprite = new Sprite(playerHealthTexture, new Rectangle(0, 16, 64, 16));
            //var playerHealthBg = new SpriteDrawable(playerHealthBgSprite);
            //playerHealthBg.MinHeight = 48;
            //playerHealthBg.LeftWidth = 1;
            //playerHealthBg.RightWidth = 1;
            //var playerHealthKnobBefore = new SpriteDrawable(playerHealthKnobBeforeSprite);
            //playerHealthKnobBefore.MinHeight = 48;
            //playerHealthKnobBefore.MinWidth = 0;
            //skin.Add("playerHealthBar", new ProgressBarStyle()
            //{
            //    Background = playerHealthBg,
            //    KnobBefore = playerHealthKnobBefore,
            //});

            //ap bar
            var apBarTexture = Game1.Content.LoadTexture(Nez.Content.Textures.UI.ProgressBar);
            var apBarBgSprite = new Sprite(apBarTexture, new Rectangle(0, 0, 64, 16));
            var apBarKnobBeforeSprite = new Sprite(apBarTexture, new Rectangle(0, 16, 64, 16));
            var apBarBg = new SpriteDrawable(apBarBgSprite);
            apBarBg.MinHeight = 48;
            apBarBg.LeftWidth = 1;
            apBarBg.RightWidth = 1;
            var apBarKnobBefore = new SpriteDrawable(apBarKnobBeforeSprite);
            apBarKnobBefore.MinHeight = 48;
            apBarKnobBefore.MinWidth = 0;
            skin.Add("apBar", new ProgressBarStyle()
            {
                Background = apBarBg,
                KnobBefore = apBarKnobBefore,
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

                skin.Add($"btn_default_{size}", new TextButtonStyle()
                {
                    Font = skin.GetFont($"font_abaddon_light_{size}"),
                    FontColor = Color.White,
                    Up = new PrimitiveDrawable(Color.Transparent),
                    Down = new PrimitiveDrawable(Color.Orange),
                    DownFontColor = Color.Black,
                    Over = new PrimitiveDrawable(Color.White),
                    OverFontColor = Color.Black
                });
            }

            return skin;
        }
    }
}
