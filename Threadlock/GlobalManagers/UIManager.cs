using Nez;
using Nez.Systems;
using Nez.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Models;
using Threadlock.StaticData;
using Threadlock.UI.Canvases;
using Threadlock.UI.Elements;
using Threadlock.UI.Skins;

namespace Threadlock.GlobalManagers
{
    public class UIManager : GlobalManager
    {
        public Emitter<UIEvents> Emitter = new Emitter<UIEvents>();

        public IEnumerator ShowTextboxText(string text)
        {
            var canvas = Game1.Scene.CreateEntity("textbox-ui").AddComponent(new UICanvas());
            canvas.SetRenderLayer(RenderLayers.ScreenSpaceRenderLayer);
            canvas.IsFullScreen = true;
            var skin = Skins.GetDefaultSkin();

            var baseTable = canvas.Stage.AddElement(new Table()).Bottom().SetFillParent(false);
            baseTable.SetWidth(Game1.ResolutionManager.UIResolution.X);
            baseTable.SetHeight(Game1.ResolutionManager.UIResolution.Y);
            //baseTable.SetFillParent(false);d

            var textbox = new Textbox(skin);
            baseTable.Add(textbox).Expand().Bottom().SetPadBottom(Value.PercentHeight(.05f, baseTable)).Width(Value.PercentWidth(1f)).Height(Value.PercentHeight(1f));

            Emitter.Emit(UIEvents.DialogueStarted);

            yield return textbox.DisplayText(text);

            Emitter.Emit(UIEvents.DialogueEnded);

            canvas.Entity.Destroy();
        }

        public IEnumerator ShowTextboxText(List<DialogueLine> dialogueSet)
        {
            var canvas = Game1.Scene.CreateEntity("textbox-ui").AddComponent(new UICanvas());
            canvas.SetRenderLayer(RenderLayers.ScreenSpaceRenderLayer);
            canvas.IsFullScreen = true;
            var skin = Skins.GetDefaultSkin();

            var baseTable = canvas.Stage.AddElement(new Table()).Bottom().SetFillParent(false);
            baseTable.SetWidth(Game1.ResolutionManager.UIResolution.X);
            baseTable.SetHeight(Game1.ResolutionManager.UIResolution.Y);
            //baseTable.SetFillParent(false);d

            var textbox = new Textbox(skin);
            baseTable.Add(textbox).Expand().Bottom().SetPadBottom(Value.PercentHeight(.05f, baseTable)).Width(Value.PercentWidth(1f)).Height(Value.PercentHeight(1f));

            Emitter.Emit(UIEvents.DialogueStarted);

            yield return textbox.DisplayText(dialogueSet);

            Emitter.Emit(UIEvents.DialogueEnded);

            canvas.Entity.Destroy();
        }

        public IEnumerator ShowMenu(Menu menu)
        {
            var entity = Game1.Scene.CreateEntity("ui-menu");
            entity.AddComponent(menu);

            Emitter.Emit(UIEvents.MenuOpened);

            //wait one frame so we don't instantly close the menu
            yield return null;

            yield return Game1.StartCoroutine(menu.OpenMenu());

            //again wait one frame so menu doesn't instantly open again
            yield return null;

            Emitter.Emit(UIEvents.MenuClosed);

            entity.Destroy();
        }
    }
    
    public enum UIEvents
    {
        DialogueStarted,
        DialogueEnded,
        MenuOpened,
        MenuClosed,
    }
}
