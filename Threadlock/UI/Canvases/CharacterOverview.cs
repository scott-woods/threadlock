//using Microsoft.Xna.Framework;
//using Nez;
//using Nez.Textures;
//using Nez.UI;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Threadlock.Components;
//using Threadlock.Entities;
//using Threadlock.Entities.Characters.Player;
//using Threadlock.Entities.Characters.Player.PlayerActions;
//using Threadlock.Models;
//using Threadlock.SaveData;
//using Threadlock.StaticData;
//using Threadlock.UI.Elements;
//using Stack = Nez.UI.Stack;

//namespace Threadlock.UI.Canvases
//{
//    public class CharacterOverview : Menu
//    {
//        const string _headerFont = "abaddon_24";
//        const string _subHeaderFont = "abaddon_18";
//        const string _bodyFont = "abaddon_12";
//        const string _defaultTipLabel = "Click to Change Slot";

//        Skin _skin;

//        Table _baseTable;
//        Table _windowTable;
//        Label _hpValueLabel;
//        Label _apValueLabel;
//        Label _headValueLabel, _bodyValueLabel, _legsValueLabel, _charmValueLabel, _infoValueLabel, _actionTipLabel;
//        List<CustomButton> _actionButtons = new List<CustomButton>();

//        Dictionary<CustomButton, ActionSlot> _actionButtonDictionary = new Dictionary<CustomButton, ActionSlot>();

//        CursorAttach<ActionSlot> _cursorAttach;

//        #region LIFECYCLE

//        public override void Initialize()
//        {
//            base.Initialize();

//            Stage.IsFullScreen = true;

//            SetRenderLayer(RenderLayers.ScreenSpaceRenderLayer);

//            _skin = Skins.Skins.GetDefaultSkin();

//            _baseTable = Stage.AddElement(new Table());
//            _baseTable.SetWidth(Game1.ResolutionManager.UIResolution.X);
//            _baseTable.SetHeight(Game1.ResolutionManager.UIResolution.Y);

//            _windowTable = new Table();
//            _windowTable.SetBackground(_skin.GetDrawable("window_blue"));
//            _windowTable.Defaults().Pad(5f);
//            _baseTable.Add(_windowTable).SetMaxWidth(Value.PercentWidth(.7f, _baseTable)).SetMaxHeight(Value.PercentHeight(.8f, _baseTable));

//            SetupStatsSection();

//            SetupBodySection();

//            _windowTable.Row();

//            SetupInventorySection();

//            SetupInfoSection();
//        }

//        public override void OnAddedToEntity()
//        {
//            base.OnAddedToEntity();

//            Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds.Open_stats_menu);

//            if (Player.Instance.TryGetComponent<HealthComponent>(out var hc))
//            {
//                _hpValueLabel.SetText($"{hc.Health}/{hc.MaxHealth}");
//            }

//            if (Player.Instance.TryGetComponent<ApComponent>(out var ap))
//                _apValueLabel.SetText(ap.MaxActionPoints.ToString());

//            if (Player.Instance.TryGetComponent<ActionManager>(out var actionManager))
//            {
//                actionManager.Emitter.AddObserver(ActionManagerEvents.ActionsChanged, OnActionsChanged);
//                OnActionsChanged();
//            }
//        }

//        public override void OnRemovedFromEntity()
//        {
//            base.OnRemovedFromEntity();

//            Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds.Close_stats_menu);

//            if (Player.Instance.TryGetComponent<ActionManager>(out var actionManager))
//                actionManager.Emitter.RemoveObserver(ActionManagerEvents.ActionsChanged, OnActionsChanged);
//        }

//        #endregion

//        #region MENU OVERRIDES

//        public override IEnumerator OpenMenu()
//        {
//            while (!Controls.Instance.ShowStats.IsPressed || _cursorAttach != null)
//                yield return null;
//        }

//        #endregion

//        #region OBSERVERS

//        void OnActionsChanged()
//        {
//            if (Player.Instance.TryGetComponent<ActionManager>(out var actionManager))
//            {
//                for (int i = 0; i < actionManager.AllActionSlots.Count; i++)
//                {
//                    var slot = actionManager.AllActionSlots[i];
//                    var button = _actionButtons[i];
//                    _actionButtonDictionary[button] = slot;

//                    if (slot.Action != null)
//                    {
//                        var id = PlayerActionUtils.GetIconName(slot.Action.GetType());
//                        button.FocusedSoundPath = Nez.Content.Audio.Sounds._002_Hover_02;
//                        button.SetStyle(_skin.Get<ButtonStyle>($"inventory_button_{id}"));
//                    }
//                    else
//                    {
//                        button.FocusedSoundPath = null;
//                        button.SetStyle(_skin.Get<ButtonStyle>("inventory_button_slot"));
//                    }
                        

//                    button.InvalidateHierarchy();
//                }
//            }
//        }

//        void OnButtonFocused(CustomButton button)
//        {
//            var slot = _actionButtonDictionary[button];

//            if (slot.Action == null)
//                return;

//            var description = PlayerActionUtils.GetDescription(slot.Action.GetType());
//            _infoValueLabel.SetText(description);
//            _infoValueLabel.Layout();
//        }

//        void OnButtonUnfocused(CustomButton button)
//        {
//            _infoValueLabel.SetText("");
//        }

//        void OnActionButtonClicked(Button button)
//        {
//            var slot = _actionButtonDictionary[button as CustomButton];

//            //if cursor attach is null, we're selecting a new action to move
//            if (_cursorAttach == null)
//            {
//                if (slot != null && slot.Action != null)
//                {
//                    Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds.Socket_Remove);
//                    _cursorAttach = Entity.Scene.AddEntity(new CursorAttach<ActionSlot>(new Sprite(slot.Action.GetIconTexture()), slot));
//                    //if (Player.Instance.TryGetComponent<ActionManager>(out var actionManager))
//                    //    actionManager.UnequipAction(slot.Button);
//                    button.SetStyle(_skin.Get<ButtonStyle>($"inventory_button_slot"));
//                    button.InvalidateHierarchy();

//                    _actionTipLabel.SetText("Replacing...");
//                }
//            }
//            else
//            {
//                if (slot == _cursorAttach.Data || slot.Action == null) //if picked the original slot or an empty slot
//                {
//                    if (Player.Instance.TryGetComponent<ActionManager>(out var actionManager))
//                        actionManager.EquipAction(PlayerActionType.FromType(_cursorAttach.Data.Action.GetType()), slot.Button);
//                }
//                else //picked a slot occupied by another action
//                {
//                    if (Player.Instance.TryGetComponent<ActionManager>(out var actionManager))
//                    {
//                        var prevActionType = PlayerActionType.FromType(slot.Action.GetType());
//                        actionManager.EquipAction(PlayerActionType.FromType(_cursorAttach.Data.Action.GetType()), slot.Button);
//                        actionManager.EquipAction(prevActionType, _cursorAttach.Data.Button);
//                    }
//                }

//                Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds.Socket_Equip);

//                _cursorAttach?.Destroy();
//                _cursorAttach = null;

//                _actionTipLabel.SetText(_defaultTipLabel);
//            }
//        }

//        #endregion

//        #region UI Setup

//        Table SetupStatsSection()
//        {
//            var statsSection = new Table();
//            statsSection.Top().Left();
//            _windowTable.Add(statsSection).Top().Left().Grow().SetUniformX();

//            //header
//            var headerLabel = new Label("Stats", _skin, _headerFont);
//            statsSection.Add(headerLabel).Left();

//            statsSection.Row();

//            var statsTable = new Table();
//            statsSection.Add(statsTable);

//            var hpLabel = new Label("HP: ", _skin, _bodyFont);
//            _hpValueLabel = new Label("", _skin, _bodyFont);
//            statsTable.Add(hpLabel).Left();
//            statsTable.Add(_hpValueLabel).SetExpandX().Right();

//            statsTable.Row();

//            var apLabel = new Label("Max AP: ", _skin, _bodyFont);
//            _apValueLabel = new Label("", _skin, _bodyFont);
//            statsTable.Add(apLabel).Left();
//            statsTable.Add(_apValueLabel).SetExpandX().Right();

//            return statsSection;
//        }

//        Table SetupBodySection()
//        {
//            var bodySection = new Table();
//            _windowTable.Add(bodySection).Grow();

//            var valuesSection = new Table();
//            bodySection.Add(valuesSection);

//            var headTable = new Table();
//            valuesSection.Add(headTable).Left();
//            var headLabel = new Label("Head", _skin, _subHeaderFont);
//            headTable.Add(headLabel).Left();
//            headTable.Row();
//            _headValueLabel = new Label("None", _skin, _bodyFont);
//            headTable.Add(_headValueLabel).Left();

//            valuesSection.Row();

//            var bodyTable = new Table();
//            valuesSection.Add(bodyTable).Left();
//            var bodyLabel = new Label("Body", _skin, _subHeaderFont);
//            bodyTable.Add(bodyLabel).Left();
//            bodyTable.Row();
//            _bodyValueLabel = new Label("None", _skin, _bodyFont);
//            bodyTable.Add(_bodyValueLabel).Left();

//            valuesSection.Row();

//            var legsTable = new Table();
//            valuesSection.Add(legsTable).Left();
//            var legsLabel = new Label("Legs", _skin, _subHeaderFont);
//            legsTable.Add(legsLabel).Left();
//            legsTable.Row();
//            _legsValueLabel = new Label("None", _skin, _bodyFont);
//            legsTable.Add(_legsValueLabel).Left();

//            valuesSection.Row();

//            var charmTable = new Table();
//            valuesSection.Add(charmTable).Left();
//            var charmLabel = new Label("Charm", _skin, _subHeaderFont);
//            charmTable.Add(charmLabel).Left();
//            charmTable.Row();
//            _charmValueLabel = new Label("None", _skin, _bodyFont);
//            charmTable.Add(_charmValueLabel).Left();

//            var characterImage = new Image(Entity.Scene.Content.LoadTexture(Nez.Content.Textures.UI.Cursedtylersmall));
//            characterImage.SetScale(.15f);
//            bodySection.Add(characterImage).SetSpaceLeft(10f);

//            return bodySection;
//        }

//        Table SetupInventorySection()
//        {
//            var inventorySection = new Table();

//            _windowTable.Add(inventorySection).Grow().SetUniformX();

//            var actionsTable = new Table();
//            inventorySection.Add(actionsTable).Grow();

//            var actionHeader = new Label("Actions", _skin, _headerFont);
//            actionsTable.Add(actionHeader).SetColspan(1).Left();

//            actionsTable.Row();

//            _actionTipLabel = new Label(_defaultTipLabel, _skin, _subHeaderFont);
//            actionsTable.Add(_actionTipLabel).SetColspan(1).Left();

//            actionsTable.Row();

//            var equippedActionsTable = new Table();
//            actionsTable.Add(equippedActionsTable).Grow().Left();

//            for (int i = 0; i < 3; i++)
//            {
//                var slot = new Table();
//                var slotDrawable = _skin.GetDrawable("Inventory_01");
//                slot.SetBackground(slotDrawable);
//                var button = new CustomButton(_skin, "inventory_button_slot");
//                button.FocusedSoundPath = null;
//                button.OnClicked += OnActionButtonClicked;
//                button.OnButtonFocused += OnButtonFocused;
//                button.OnButtonUnfocused += OnButtonUnfocused;
//                //button.SetBackground(new SpriteDrawable(Entity.Scene.Content.LoadTexture(Nez.Content.Textures.UI.Icons.Plus_icon_up)));
//                _actionButtonDictionary.Add(button, null);
//                _actionButtons.Add(button);

//                equippedActionsTable.Add(slot).Left().Width(slotDrawable.MinWidth).Height(slotDrawable.MinHeight);
//                slot.Add(button);
//            }

//            //var reserveSlot = new Table();
//            //var reserveSlotDrawable = _skin.GetDrawable("Inventory_01");
//            //reserveSlot.SetBackground(reserveSlotDrawable);

//            //inventorySection.Row();

//            //var modsTable = new Table();
//            //inventorySection.Add(modsTable);

//            //var modsHeader = new Label("Mods", _skin, _subHeaderFont);
//            //modsTable.Add(modsHeader).Expand().Left();

//            return inventorySection;
//        }

//        Table SetupInfoSection()
//        {
//            var infoSection = new Table();

//            _windowTable.Add(infoSection).Grow();

//            var headerLabel = new Label("Info", _skin, _headerFont);
//            infoSection.Add(headerLabel).Top().Left();

//            infoSection.Row();

//            _infoValueLabel = new Label("", _skin, _bodyFont);
//            _infoValueLabel.SetWidth(5f);
//            _infoValueLabel.SetWrap(true);
//            infoSection.Add(_infoValueLabel).Grow().Top().Left();

//            return infoSection;
//        }

//        #endregion
//    }
//}
