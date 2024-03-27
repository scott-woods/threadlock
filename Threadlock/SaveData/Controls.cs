using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Input;
using Nez.Persistence;
using System.IO;

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
                    _instance = LoadData();
                }
                return _instance;
            }
        }

        private Controls()
        {
            Game1.Emitter.AddObserver(CoreEvents.Exiting, OnExiting);
        }

        public void SaveData()
        {
            var settings = JsonSettings.HandlesReferences;
            settings.TypeNameHandling = TypeNameHandling.All;

            var json = Json.ToJson(this, settings);
            File.WriteAllText("Data/controls.json", json);
        }

        private static Controls LoadData()
        {
            if (File.Exists("Data/controls.json"))
            {
                var json = File.ReadAllText("Data/controls.json");
                _instance = Json.FromJson<Controls>(json);
            }
            else
            {
                _instance = new Controls();
            }

            return _instance;
        }

        void OnExiting()
        {
            SaveData();
        }

        public Keys UIActionKey = Keys.E;
        public Buttons UIActionButton = Buttons.A;

        public VirtualButton Confirm = new VirtualButton(
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

        public VirtualButton Action1 = new VirtualButton(
            new VirtualButton.KeyboardKey(Keys.Q)
            );

        public VirtualButton Action2 = new VirtualButton(
            new VirtualButton.KeyboardKey(Keys.E)
            );

        public VirtualButton SupportAction = new VirtualButton(
            new VirtualButton.KeyboardKey(Keys.F)
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
