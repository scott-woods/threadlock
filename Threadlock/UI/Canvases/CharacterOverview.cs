using Microsoft.Xna.Framework;
using Nez;
using Nez.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Entities.Characters.Player;
using Threadlock.Entities.Characters.Player.PlayerActions;
using Threadlock.SaveData;
using Threadlock.StaticData;

namespace Threadlock.UI.Canvases
{
    public class CharacterOverview : UICanvas
    {
        const string _headerFont = "abaddon_24";
        const string _subHeaderFont = "abaddon_18";
        const string _bodyFont = "abaddon_12";

        Action _closeHandler;

        Skin _skin;

        Table _baseTable;
        Table _windowTable;
        Label _hpValueLabel;
        Label _apValueLabel;
        Label _headValueLabel, _bodyValueLabel, _legsValueLabel, _charmValueLabel;
        List<Button> _actionButtons = new List<Button>();

        public CharacterOverview(Action closeHandler)
        {
            _closeHandler = closeHandler;
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

            _windowTable = new Table();
            _windowTable.SetBackground(_skin.GetDrawable("window_blue"));
            _windowTable.Defaults().Pad(10f);
            _baseTable.Add(_windowTable).SetMaxWidth(Value.PercentWidth(.5f, _baseTable)).SetMaxHeight(Value.PercentHeight(.8f, _baseTable));

            SetupStatsSection();

            SetupBodySection();

            _windowTable.Row();

            SetupInventorySection();

            SetupInfoSection();
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            if (Player.Instance.TryGetComponent<HealthComponent>(out var hc))
            {
                _hpValueLabel.SetText($"{hc.Health}/{hc.MaxHealth}");
            }

            if (Player.Instance.TryGetComponent<ApComponent>(out var ap))
                _apValueLabel.SetText(ap.MaxActionPoints.ToString());

            if (Player.Instance.TryGetComponent<ActionManager>(out var actionManager))
            {
                for (int i = 0; i < actionManager.AllActionSlots.Count; i++)
                {
                    var slot = actionManager.AllActionSlots[i];
                    if (slot.Action == null)
                        continue;

                    var id = PlayerActionUtils.GetIconName(slot.Action.GetType());

                    var button = _actionButtons[i];
                    button.SetStyle(_skin.Get<ButtonStyle>($"inventory_button_{id}"));
                }
            }
        }

        public override void Update()
        {
            base.Update();

            if (Controls.Instance.ShowStats.IsPressed)
            {
                _closeHandler?.Invoke();
                return;
            }
        }

        Table SetupStatsSection()
        {
            var statsSection = new Table();
            statsSection.Top().Left();
            statsSection.DebugAll();
            _windowTable.Add(statsSection).Grow();

            //header
            var headerLabel = new Label("Stats", _skin, _headerFont);
            statsSection.Add(headerLabel).Left();

            statsSection.Row();

            var statsTable = new Table();
            statsSection.Add(statsTable);

            var hpLabel = new Label("HP: ", _skin, _bodyFont);
            _hpValueLabel = new Label("", _skin, _bodyFont);
            statsTable.Add(hpLabel).Left();
            statsTable.Add(_hpValueLabel).SetExpandX().Right();

            statsTable.Row();

            var apLabel = new Label("Max AP: ", _skin, _bodyFont);
            _apValueLabel = new Label("", _skin, _bodyFont);
            statsTable.Add(apLabel).Left();
            statsTable.Add(_apValueLabel).SetExpandX().Right();

            return statsSection;
        }

        Table SetupBodySection()
        {
            var bodySection = new Table();
            _windowTable.Add(bodySection).Grow();

            var valuesSection = new Table();
            bodySection.Add(valuesSection);

            var headTable = new Table();
            valuesSection.Add(headTable).Left();
            var headLabel = new Label("Head", _skin, _subHeaderFont);
            headTable.Add(headLabel).Left();
            headTable.Row();
            _headValueLabel = new Label("None", _skin, _bodyFont);
            headTable.Add(_headValueLabel).Left();

            valuesSection.Row();

            var bodyTable = new Table();
            valuesSection.Add(bodyTable).Left();
            var bodyLabel = new Label("Body", _skin, _subHeaderFont);
            bodyTable.Add(bodyLabel).Left();
            bodyTable.Row();
            _bodyValueLabel = new Label("None", _skin, _bodyFont);
            bodyTable.Add(_bodyValueLabel).Left();

            valuesSection.Row();

            var legsTable = new Table();
            valuesSection.Add(legsTable).Left();
            var legsLabel = new Label("Legs", _skin, _subHeaderFont);
            legsTable.Add(legsLabel).Left();
            legsTable.Row();
            _legsValueLabel = new Label("None", _skin, _bodyFont);
            legsTable.Add(_legsValueLabel).Left();

            valuesSection.Row();

            var charmTable = new Table();
            valuesSection.Add(charmTable).Left();
            var charmLabel = new Label("Charm", _skin, _subHeaderFont);
            charmTable.Add(charmLabel).Left();
            charmTable.Row();
            _charmValueLabel = new Label("None", _skin, _bodyFont);
            charmTable.Add(_charmValueLabel).Left();

            var characterImage = new Image(Entity.Scene.Content.LoadTexture(Nez.Content.Textures.UI.Cursedtylersmall));
            characterImage.SetScale(.15f);
            bodySection.Add(characterImage).SetSpaceLeft(10f);

            return bodySection;
        }

        Table SetupInventorySection()
        {
            var inventorySection = new Table();

            _windowTable.Add(inventorySection).Grow();

            var actionsTable = new Table();
            inventorySection.Add(actionsTable);

            var actionHeader = new Label("Actions", _skin, _subHeaderFont);
            actionsTable.Add(actionHeader).SetColspan(3).Left();

            actionsTable.Row();

            for (int i = 0; i < 3; i++)
            {
                var button = new Button(_skin, "inventory_button_empty");
                //button.SetBackground(new SpriteDrawable(Entity.Scene.Content.LoadTexture(Nez.Content.Textures.UI.Icons.Plus_icon_up)));
                actionsTable.Add(button);
                _actionButtons.Add(button);
            }

            inventorySection.Row();

            var modsTable = new Table();
            inventorySection.Add(modsTable);

            var modsHeader = new Label("Mods", _skin, _subHeaderFont);
            modsTable.Add(modsHeader).Expand().Left();

            return inventorySection;
        }

        Table SetupInfoSection()
        {
            var infoSection = new Table();

            _windowTable.Add(infoSection).Grow();

            var headerLabel = new Label("Info", _skin, _headerFont);
            infoSection.Add(headerLabel).Expand().Top().Left();

            return infoSection;
        }
    }
}
