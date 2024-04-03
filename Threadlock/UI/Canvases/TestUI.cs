using Nez;
using Nez.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Entities.Characters.Player;
using Threadlock.StaticData;
using Threadlock.UI.Elements;

namespace Threadlock.UI.Canvases
{
    public class TestUI : UICanvas
    {
        Skin _skin;

        Table _baseTable;
        PlayerHealthbar _healthBar;

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
            topLeftTable.DebugAll();
            _baseTable.Add(topLeftTable).Top().Left();

            _healthBar = new PlayerHealthbar(_skin, "playerHealthBar");
            //topLeftTable.Add(_healthBar).Width(Value.PercentWidth(.15f, _baseTable));
            topLeftTable.Add(_healthBar);
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
