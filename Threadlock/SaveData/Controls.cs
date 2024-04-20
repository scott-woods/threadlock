using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Input;
using Nez.Persistence;
using System.IO;
using static Nez.VirtualButton;

namespace Threadlock.SaveData
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

        public static readonly Dictionary<Keys, string> KeyIconDictionary = new Dictionary<Keys, string>()
        {
            [Keys.Q] = "image_keys_32",
            [Keys.E] = "image_keys_20",
            [Keys.F] = "image_keys_21"
        };

        public VirtualButton Confirm = new VirtualButton(
            new VirtualButton.GamePadButton(0, Buttons.A),
            new VirtualButton.MouseLeftButton()
            );

        public VirtualButton Check = new VirtualButton(
            new VirtualButton.KeyboardKey(Settings.Instance.CheckKey),
            new VirtualButton.GamePadButton(0, Buttons.A)
            );

        public VirtualButton Cancel = new VirtualButton(
            new VirtualButton.KeyboardKey(Settings.Instance.CancelKey),
            new VirtualButton.GamePadButton(0, Buttons.B)
            );

        public VirtualButton ShowStats = new VirtualButton(
            new VirtualButton.KeyboardKey(Settings.Instance.ShowStatsKey),
            new VirtualButton.GamePadButton(0, Buttons.Back)
            );

        public VirtualButton Pause = new VirtualButton(
            new VirtualButton.KeyboardKey(Settings.Instance.PauseKey),
            new VirtualButton.GamePadButton(0, Buttons.Start)
            );

        public VirtualButton Melee = new VirtualButton(
            new VirtualButton.MouseLeftButton(),
            new VirtualButton.GamePadButton(0, Buttons.X)
            );

        public VirtualButton AltAttack = new VirtualButton(
            new VirtualButton.MouseRightButton()
            );

        public VirtualButton Dodge = new VirtualButton(
            new VirtualButton.KeyboardKey(Settings.Instance.DodgeKey),
            new VirtualButton.GamePadButton(0, Buttons.B)
            );

        public VirtualButton Action1 = new VirtualButton(
            new VirtualButton.KeyboardKey(Settings.Instance.Action1Key)
            );

        public VirtualButton Action2 = new VirtualButton(
            new VirtualButton.KeyboardKey(Settings.Instance.Action2Key)
            );

        public VirtualButton SupportAction = new VirtualButton(
            new VirtualButton.KeyboardKey(Settings.Instance.SupportActionKey)
            );

        public VirtualButton Reload = new VirtualButton(
            new VirtualButton.KeyboardKey(Settings.Instance.Reload)
            );

        public VirtualIntegerAxis XAxisIntegerInput = new VirtualIntegerAxis(
            new VirtualAxis.GamePadDpadLeftRight(),
            new VirtualAxis.GamePadLeftStickX(),
            new VirtualAxis.KeyboardKeys(VirtualInput.OverlapBehavior.TakeNewer, Settings.Instance.LeftKey, Settings.Instance.RightKey)
            );

        public VirtualIntegerAxis YAxisIntegerInput = new VirtualIntegerAxis(
            new VirtualAxis.GamePadDpadUpDown(),
            new VirtualAxis.GamePadLeftStickY(),
            new VirtualAxis.KeyboardKeys(VirtualInput.OverlapBehavior.TakeNewer, Settings.Instance.UpKey, Settings.Instance.DownKey)
            );

        public VirtualJoystick DirectionalInput = new VirtualJoystick(
            false,
            new VirtualJoystick.GamePadDpad(),
            new VirtualJoystick.GamePadLeftStick(),
            new VirtualJoystick.KeyboardKeys(VirtualInput.OverlapBehavior.TakeNewer, Settings.Instance.LeftKey, Settings.Instance.RightKey, Settings.Instance.UpKey, Settings.Instance.DownKey)
            );

        public static string GetIconString(VirtualButton button)
        {
            foreach (var node in button.Nodes)
            {
                if (node is KeyboardKey keyboardKey)
                {
                    if (KeyIconDictionary.TryGetValue(keyboardKey.Key, out var iconString))
                        return iconString;
                }
            }

            return null;
        }
    }
}
