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
            var borderDrawable = new MaskedNinePatchDrawable(borderSprite);
            borderDrawable.LeftWidth = 0;
            borderDrawable.RightWidth = 0;
            var barSprite = new NinePatchSprite(playerHealthTexture, new Rectangle(48, 0, 48, 16), 16, 16, 0, 0);
            var barDrawable = new MaskedNinePatchDrawable(barSprite);
            barDrawable.MinWidth = 0;
            skin.Add("playerHealthBar", new ProgressBarStyle()
            {
                Background = borderDrawable,
                KnobBefore = barDrawable,
            });

            //ap bar
            var apBarTexture = Game1.Content.LoadTexture(Nez.Content.Textures.UI.PixelUIpack3._04);
            var apBarBgSprite = new NinePatchSprite(apBarTexture, new Rectangle(0, 48, 48, 16), 16, 16, 0, 0);
            var apBarBgDrawable = new MaskedNinePatchDrawable(apBarBgSprite);
            apBarBgDrawable.LeftWidth = 0;
            apBarBgDrawable.RightWidth = 0;
            var apBarKnobBeforeSprite = new NinePatchSprite(apBarTexture, new Rectangle(48, 32, 48, 16), 16, 16, 0, 0);
            var apBarKnobBeforeDrawable = new MaskedNinePatchDrawable(apBarKnobBeforeSprite);
            apBarKnobBeforeDrawable.MinWidth = 0;
            skin.Add("apBar", new ProgressBarStyle()
            {
                Background = apBarBgDrawable,
                KnobBefore = apBarKnobBeforeDrawable,
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

            //keyboard key symbols
            var keysTexture = Game1.Content.LoadTexture(Nez.Content.Textures.UI.KeyboardSymbols.KeyboardLettersandSymbols);
            var keySprites = Sprite.SpritesFromAtlas(keysTexture, 16, 16);
            for (int i = 0; i < keySprites.Count; i++)
            {
                skin.Add($"image_keys_{i}", keySprites[i]);
            }

            //ammo
            var ammoTexture = Game1.Content.LoadTexture(Nez.Content.Textures.UI.UIAmmo.UIAmmo16x16);
            var ammoSprites = Sprite.SpritesFromAtlas(ammoTexture, 16, 16);
            var index = 17;
            for (int i = 0; i < 6; i++)
            {
                skin.Add($"image_ammo_{i}", ammoSprites[index]);
                index--;
            }

            //coin
            var coinTexture = Game1.Content.LoadTexture(Nez.Content.Textures.Drops.CollectablesSheet);
            var coinSprite = new Sprite(coinTexture, 0, 0, 16, 16);
            skin.Add($"image_coin", coinSprite);

            //dust
            var dustTexture = Game1.Content.LoadTexture(Nez.Content.Textures.Drops.CollectablesSheet);
            var dustSprite = new Sprite(dustTexture, 176, 32, 16, 16);
            skin.Add($"image_dust", dustSprite);

            //slider
            var sliderTexture = Game1.Content.LoadTexture(Nez.Content.Textures.UI.UISliders.Simple_slider);
            var knobSprite = new Sprite(sliderTexture, new Rectangle(0, 0, 3, 3));
            var sliderSprite = new Sprite(sliderTexture, new Rectangle(3, 0, 35, 16));
            var sliderDrawable = new SpriteDrawable(sliderSprite);
            sliderDrawable.LeftWidth = 3;
            sliderDrawable.RightWidth = 1;
            skin.Add("slider_simple", new ProgressBarStyle()
            {
                Knob = new SpriteDrawable(knobSprite),
                Background = sliderDrawable
            });

            skin.Add("inventory_button_empty", new ButtonStyle()
            {
                Up = skin.GetDrawable("Inventory_01"),
                Over = skin.GetDrawable("Inventory_02"),
            });

            for (int i = 0; i <= 335; i++)
            {
                var num = i.ToString();
                while (num.Length < 3)
                    num = num.Insert(0, "0");

                skin.Add($"inventory_button_{num}", new ButtonStyle()
                {
                    Up = skin.GetDrawable($"Style 4 Icon {num}"),
                    Down = skin.GetDrawable($"Style 3 Icon {num}"),
                    Over = skin.GetDrawable($"Style 3 Icon {num}")
                });
            }

            return skin;
        }
    }
}
