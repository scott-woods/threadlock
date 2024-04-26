using Nez;
using Nez.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Entities.Characters.Player;
using Threadlock.Entities.Characters.Player.BasicWeapons;
using Threadlock.Entities.Characters.Player.PlayerActions;
using Threadlock.SaveData;
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
        Table _apTable;
        Table _bottomTable;
        Table _weaponInfoTable;
        List<ProgressBar> _apBars = new List<ProgressBar>();
        List<ActionIcon> _actionIcons = new List<ActionIcon>();
        Label _coinLabel;
        Label _dustLabel;

        ActionManager _actionManager;

        public override void Initialize()
        {
            base.Initialize();

            Stage.IsFullScreen = true;

            SetRenderLayer(RenderLayers.ScreenSpaceRenderLayer);

            _skin = Skins.Skins.GetDefaultSkin();

            _baseTable = Stage.AddElement(new Table());
            _baseTable.SetWidth(Game1.ResolutionManager.UIResolution.X);
            _baseTable.SetHeight(Game1.ResolutionManager.UIResolution.Y);
            _baseTable.SetFillParent(false).Pad(Value.PercentWidth(.02f));

            var topLeftTable = new Table();
            _baseTable.Add(topLeftTable).Top().Left();

            _healthBar = new PlayerHealthbar(_skin, "playerHealthBar");
            topLeftTable.Add(_healthBar).Width(Value.PercentWidth(.15f, _baseTable));

            _baseTable.Row();

            _apTable = new Table();
            //_apTable.Defaults().SetSpaceRight(Value.PercentWidth(.005f, _baseTable));
            _baseTable.Add(_apTable).Width(Value.PercentWidth(.3f, _baseTable)).Top().Left();

            _baseTable.Row();

            var currencyTable = new Table();
            _baseTable.Add(currencyTable).Left();

            currencyTable.Add(new Image(_skin.GetDrawable("image_coin")));
            _coinLabel = new Label("", _skin, "abaddon_18");
            currencyTable.Add(_coinLabel).SetPadLeft(3f).SetPadTop(5f);

            currencyTable.Row();

            currencyTable.Add(new Image(_skin.GetDrawable("image_dust")));
            _dustLabel = new Label("", _skin, "abaddon_18");
            currencyTable.Add(_dustLabel).SetPadLeft(3f).SetPadTop(5f);

            currencyTable.Row();

            var menuTable = new Table();
            currencyTable.Add(menuTable).SetPadLeft(3f).SetPadTop(5f);
            menuTable.Add(new Image(_skin.GetDrawable("image_extra_keys_0")));
            menuTable.Row();
            var menuLabel = new Label("Menu", _skin, "abaddon_12");
            menuTable.Add(menuLabel);

            _baseTable.Row();

            _bottomTable = new Table();
            _bottomTable.Bottom().Left();
            _baseTable.Add(_bottomTable).Grow();

            _weaponInfoTable = new Table();
            _weaponInfoTable.Left();
            _bottomTable.Add(_weaponInfoTable).Left().SetSpaceBottom(2f);

            _bottomTable.Row();

            _iconsTable = new Table();
            _iconsTable.Defaults().SetSpaceRight(Value.PercentWidth(.01f, _baseTable));
            _bottomTable.Add(_iconsTable).Left();
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

            //setup actions
            if (Player.Instance.TryGetComponent<ActionManager>(out var actionManager))
            {
                _actionManager = actionManager;
                _actionManager.Emitter.AddObserver(ActionManagerEvents.ActionsChanged, OnActionsChanged);

                OnActionsChanged();
            }

            if (Player.Instance.TryGetComponent<ApComponent>(out var ac))
            {
                ac.OnApChanged += OnApChanged;

                var tableCell = _baseTable.GetCell(_apTable);
                var width = tableCell.GetMaxWidth() / ac.MaxActionPoints;
                for (int i = 0; i < ac.MaxActionPoints; i++)
                {
                    var bar = new ProgressBar(_skin, "apBar");
                    bar.SetMinMax(0, 1);
                    _apBars.Add(bar);
                    _apTable.Add(bar).Width(width);
                }
            }

            if (Player.Instance.TryGetComponent<BasicWeapon>(out var weapon))
                OnWeaponChanged(weapon);

            Player.Instance.OnWeaponChanged += OnWeaponChanged;

            Game1.UIManager.Emitter.AddObserver(GlobalManagers.UIEvents.DialogueStarted, OnDialogueStarted);
            Game1.UIManager.Emitter.AddObserver(GlobalManagers.UIEvents.DialogueEnded, OnDialogueEnded);

            PlayerData.Instance.Emitter.AddObserver(PlayerDataEvents.DollahsChanged, OnDollahsChanged);
            OnDollahsChanged();
            PlayerData.Instance.Emitter.AddObserver(PlayerDataEvents.DustChanged, OnDustChanged);
            OnDustChanged();
        }

        void OnActionsChanged()
        {
            _iconsTable.Clear();
            _actionIcons.Clear();

            foreach (var slot in _actionManager.AllActionSlots)
            {
                if (slot.Action == null)
                    continue;

                var type = slot.Action.GetType();
                var table = new Table();
                _iconsTable.Add(table);
                var icon = new ActionIcon(_skin, PlayerActionUtils.GetIconName(type), PlayerActionUtils.GetApCost(type));
                _actionIcons.Add(icon);
                table.Add(icon);

                table.Row();

                var key = new Image(_skin.GetDrawable(Controls.GetIconString(slot.Button)));
                table.Add(key);
            }
        }

        void OnDollahsChanged()
        {
            var value = PlayerData.Instance.Dollahs.ToString();
            _coinLabel.SetText(value == "" ? "0" : value);
        }

        void OnDustChanged()
        {
            var value = PlayerData.Instance.Dust.ToString();
            _dustLabel.SetText(value == "" ? "0" : value);
        }

        void OnDialogueStarted()
        {
            SetEnabled(false);
        }

        void OnDialogueEnded()
        {
            SetEnabled(true);
        }

        void OnWeaponChanged(BasicWeapon weapon)
        {
            _weaponInfoTable?.Clear();

            if (weapon is Gun gun)
            {
                var ammoTable = new AmmoTable(_skin, gun);
                _weaponInfoTable.Add(ammoTable).Left();
            }
        }

        public override void OnRemovedFromEntity()
        {
            base.OnRemovedFromEntity();

            if (Player.Instance.TryGetComponent<HealthComponent>(out var hc))
            {
                hc.OnHealthChanged -= OnHealthChanged;
            }

            if (Player.Instance.TryGetComponent<ApComponent>(out var ac))
            {
                ac.OnApChanged -= OnApChanged;
            }

            Game1.UIManager.Emitter.RemoveObserver(GlobalManagers.UIEvents.DialogueStarted, OnDialogueStarted);
            Game1.UIManager.Emitter.RemoveObserver(GlobalManagers.UIEvents.DialogueEnded, OnDialogueEnded);

            PlayerData.Instance.Emitter.RemoveObserver(PlayerDataEvents.DollahsChanged, OnDollahsChanged);
            PlayerData.Instance.Emitter.RemoveObserver(PlayerDataEvents.DustChanged, OnDustChanged);

            _actionManager.Emitter.RemoveObserver(ActionManagerEvents.ActionsChanged, OnActionsChanged);
        }

        void OnHealthChanged(int oldValue, int newValue)
        {
            _healthBar.Value = newValue;
        }

        void OnApChanged(int totalAp, float progress)
        {
            //update ap bars
            for (int i = 0; i < _apBars.Count; i++)
            {
                if (i < totalAp)
                {
                    _apBars[i].SetValue(1f);
                }
                else if (i == totalAp)
                {
                    _apBars[i].SetValue(progress);
                }
                else
                {
                    _apBars[i].SetValue(0);
                }
            }

            //update icons
            foreach (var icon in _actionIcons)
            {
                icon.UpdateDisplay(totalAp);
            }
        }
    }
}
