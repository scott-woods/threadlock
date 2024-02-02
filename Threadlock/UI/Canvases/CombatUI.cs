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

        public override void Initialize()
        {
            base.Initialize();

            Stage.IsFullScreen = true;

            SetRenderLayer(RenderLayers.ScreenSpaceRenderLayer);

            _skin = Skin.CreateDefaultSkin();

            _baseTable = Stage.AddElement(new Table());
            _baseTable.SetWidth(Game1.ResolutionManager.UIResolution.X);
            _baseTable.SetHeight(Game1.ResolutionManager.UIResolution.Y);
            _baseTable.Pad(Value.PercentWidth(.025f));

            var topLeftTable = new Table();
            _baseTable.Add(topLeftTable).Expand().Top().Left();

            _healthBar = new ProgressBar(_skin);
            topLeftTable.Add(_healthBar).SetSpaceBottom(Value.PercentHeight(.025f, _baseTable));

            topLeftTable.Row();

            _apBar = new ProgressBar(_skin);
            topLeftTable.Add(_apBar);
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
