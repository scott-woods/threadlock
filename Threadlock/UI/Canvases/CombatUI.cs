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
using Threadlock.StaticData;
using Threadlock.UI.Elements;

namespace Threadlock.UI.Canvases
{
    public class CombatUI : UICanvas
    {
        //skin
        Skin _skin;

        //elements
        Table _baseTable;
        ProgressBar _healthBar;
        ProgressBar _apBar;
        Table _iconsTable;

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

            var topLeftTable = new Table();
            _baseTable.Add(topLeftTable).Top().Left();

            _healthBar = new PlayerHealthbar(_skin, "playerHealthBar");
            topLeftTable.Add(_healthBar).Width(Value.PercentWidth(.15f, _baseTable));

            _baseTable.Row();

            var bottomTable = new Table();
            _baseTable.Add(bottomTable).Grow();

            _iconsTable = new Table();
            _iconsTable.Defaults().SetSpaceRight(Value.PercentWidth(.01f, _baseTable));
            bottomTable.Add(_iconsTable).Expand().Bottom().Left();
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            if (Player.Instance.TryGetComponent<HealthComponent>(out var hc))
            {
                _healthBar.SetMinMax(0, hc.MaxHealth);
                _healthBar.Value = hc.Health;
                hc.OnHealthChanged += OnHealthChanged;
            }

            if (Player.Instance.OffensiveAction1 != null)
            {
                var table = new Table();
                _iconsTable.Add(table);
                var icon = new ActionIcon(_skin, PlayerActionUtils.GetIconName(Player.Instance.OffensiveAction1.GetType()));
                table.Add(icon);

                table.Row();

                var label = new Label("Q", _skin, "abaddon_24");
                table.Add(label);
            }
            if (Player.Instance.SupportAction != null)
            {
                var table = new Table();
                _iconsTable.Add(table);
                var icon = new ActionIcon(_skin, PlayerActionUtils.GetIconName(Player.Instance.SupportAction.GetType()));
                table.Add(icon);

                table.Row();

                var label = new Label("F", _skin, "abaddon_24");
                table.Add(label);
            }
        }

        public override void OnRemovedFromEntity()
        {
            base.OnRemovedFromEntity();

            if (Player.Instance.TryGetComponent<HealthComponent>(out var hc))
            {
                hc.OnHealthChanged -= OnHealthChanged;
            }
        }

        void OnHealthChanged(int oldValue, int newValue)
        {
            _healthBar.Value = newValue;
        }
    }
}
