using Nez;
using Nez.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.SaveData;
using Threadlock.StaticData;
using Threadlock.UI.Elements;

namespace Threadlock.UI.Canvases
{
    public class PauseMenu : UICanvas
    {
        Skin _skin;

        //elements
        Table _baseTable;

        Action _unpauseHandler;

        public PauseMenu(Action unpauseHandler)
        {
            _unpauseHandler = unpauseHandler;
        }

        public override void Initialize()
        {
            base.Initialize();

            Stage.IsFullScreen = true;

            SetRenderLayer(RenderLayers.ScreenSpaceRenderLayer);

            _skin = Skins.Skins.GetDefaultSkin();

            _baseTable = Stage.AddElement(new Table());
            _baseTable.SetWidth(Game1.ResolutionManager.UIResolution.X);
            _baseTable.SetHeight(Game1.ResolutionManager.UIResolution.Y);
            _baseTable.SetFillParent(false).Pad(Value.PercentWidth(.025f));

            var table = new Table();
            table.SetBackground(_skin.GetNinePatchDrawable("window_blue"));
            _baseTable.Add(table).Width(Value.PercentWidth(.4f, _baseTable));

            var innerTable = new Table();
            table.Add(innerTable).Grow();

            var resumeButton = new TextButton("Resume", _skin, "btn_default_24");
            resumeButton.OnClicked += OnResumeClicked;
            innerTable.Add(resumeButton);

            innerTable.Row();

            var exitButton = new TextButton("Quit", _skin, "btn_default_24");
            innerTable.Add(exitButton);
            exitButton.OnClicked += OnExitClicked;
        }

        public override void OnEnabled()
        {
            base.OnEnabled();

            Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds._094_Pause_06);
        }

        public override void Update()
        {
            base.Update();

            if (Controls.Instance.Pause.IsPressed)
            {
                _unpauseHandler?.Invoke();
            }
        }

        void OnResumeClicked(Button obj)
        {
            Entity.Destroy();
            _unpauseHandler?.Invoke();
        }

        void OnExitClicked(Button obj)
        {
            Game1.Exit();
        }
    }
}
