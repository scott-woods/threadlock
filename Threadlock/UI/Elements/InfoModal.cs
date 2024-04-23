using Microsoft.Xna.Framework;
using Nez;
using Nez.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Helpers;
using Threadlock.StaticData;

namespace Threadlock.UI.Elements
{
    public class InfoModal : UICanvas
    {
        Vector2 _anchor;

        string _titleText;
        string _bodyText;

        Skin _skin;
        Table _table;
        Label _title, _body;

        public InfoModal(Vector2 anchor, string title, string body)
        {
            _anchor = anchor;
            _titleText = title;
            _bodyText = body;
        }

        public override void Initialize()
        {
            base.Initialize();

            SetRenderLayer(RenderLayers.ScreenSpaceRenderLayer);

            _skin = Skins.Skins.GetDefaultSkin();

            _table = new Table();
            _table.Pad(10);
            _table.SetBackground(_skin.GetDrawable("window_blue"));

            Stage.AddElement(_table);

            _title = new Label(_titleText, _skin, "abaddon_24");
            _table.Add(_title).Left();

            _table.Row();

            _body = new Label(_bodyText, _skin, "abaddon_18");
            _table.Add(_body).Grow().Left();

            _table.DebugAll();

            _table.Pack();
        }

        public override void Update()
        {
            base.Update();

            //UIHelper.MoveToWorldSpace(_anchor - new Vector2(_table.width / 2, _table.height), _table);
            var xOffset = (_table.width / 2);
            var finalPos = UIHelper.GetScreenPoint(_anchor);
            _table.SetPosition(finalPos.X - xOffset, finalPos.Y - _table.height);
            //UIHelper.MoveToWorldSpace(_anchor - new Vector2(xOffset, _table.height), _table);
        }
    }
}
