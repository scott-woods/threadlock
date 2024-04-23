using Nez.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.UI.Elements
{
    public class CustomButton : Button, IInputListener
    {
        public event Action<CustomButton> OnButtonFocused;
        public event Action<CustomButton> OnButtonUnfocused;

        public string FocusedSoundPath = Nez.Content.Audio.Sounds._002_Hover_02;

        #region CONSTRUCTORS

        public CustomButton(ButtonStyle style) : base(style)
        {
        }

        public CustomButton(IDrawable up) : base(up)
        {
        }

        public CustomButton(Skin skin, string styleName = null) : base(skin, styleName)
        {
        }

        public CustomButton(IDrawable up, IDrawable down) : base(up, down)
        {
        }

        public CustomButton(IDrawable up, IDrawable down, IDrawable checked_) : base(up, down, checked_)
        {
        }

        #endregion

        void IInputListener.OnMouseEnter()
        {
            _mouseOver = true;

            if (!string.IsNullOrWhiteSpace(FocusedSoundPath))
                Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds._002_Hover_02);
            OnButtonFocused?.Invoke(this);
        }

        void IInputListener.OnMouseExit()
        {
            _mouseOver = _mouseDown = false;

            OnButtonUnfocused?.Invoke(this);
        }
    }
}
