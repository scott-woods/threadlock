using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Input;

namespace Threadlock.StaticData
{
    public class Controls
    {
        private static Controls _instance;
        public static Controls Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Controls();
                }
                return _instance;
            }
        }

        public Keys UIActionKey = Keys.E;
        public Buttons UIActionButton = Buttons.A;

        public VirtualButton Confirm = new VirtualButton(
            new VirtualButton.KeyboardKey(Keys.E),
            new VirtualButton.GamePadButton(0, Buttons.A),
            new VirtualButton.MouseLeftButton()
            );

        public VirtualButton Check = new VirtualButton(
            new VirtualButton.KeyboardKey(Keys.E),
            new VirtualButton.GamePadButton(0, Buttons.A)
            );

        public VirtualButton TriggerTurn = new VirtualButton(
            new VirtualButton.KeyboardKey(Keys.E),
            new VirtualButton.GamePadButton(0, Buttons.A)
            );

        public VirtualButton Cancel = new VirtualButton(
            new VirtualButton.KeyboardKey(Keys.X),
            new VirtualButton.GamePadButton(0, Buttons.B)
            );

        public VirtualButton ShowStats = new VirtualButton(
            new VirtualButton.KeyboardKey(Keys.Tab),
            new VirtualButton.GamePadButton(0, Buttons.Back)
            );

        public VirtualButton Pause = new VirtualButton(
            new VirtualButton.KeyboardKey(Keys.Escape),
            new VirtualButton.GamePadButton(0, Buttons.Start)
            );

        public VirtualButton Melee = new VirtualButton(
            new VirtualButton.MouseLeftButton(),
            new VirtualButton.GamePadButton(0, Buttons.X)
            );

        public VirtualButton Dodge = new VirtualButton(
            new VirtualButton.KeyboardKey(Keys.Space),
            new VirtualButton.GamePadButton(0, Buttons.B)
            );

        public VirtualIntegerAxis XAxisIntegerInput = new VirtualIntegerAxis(
            new VirtualAxis.GamePadDpadLeftRight(),
            new VirtualAxis.GamePadLeftStickX(),
            new VirtualAxis.KeyboardKeys(VirtualInput.OverlapBehavior.TakeNewer, Keys.A, Keys.D)
            );

        public VirtualIntegerAxis YAxisIntegerInput = new VirtualIntegerAxis(
            new VirtualAxis.GamePadDpadUpDown(),
            new VirtualAxis.GamePadLeftStickY(),
            new VirtualAxis.KeyboardKeys(VirtualInput.OverlapBehavior.TakeNewer, Keys.W, Keys.S)
            );

        public VirtualJoystick DirectionalInput = new VirtualJoystick(
            false,
            new VirtualJoystick.GamePadDpad(),
            new VirtualJoystick.GamePadLeftStick(),
            new VirtualJoystick.KeyboardKeys(VirtualInput.OverlapBehavior.TakeNewer, Keys.A, Keys.D, Keys.W, Keys.S)
            );
    }
}
