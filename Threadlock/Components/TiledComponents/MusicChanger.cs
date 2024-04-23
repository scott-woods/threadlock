using Microsoft.Xna.Framework;
using Nez;
using Nez.Tiled;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.StaticData;

namespace Threadlock.Components.TiledComponents
{
    public class MusicChanger : TiledComponent, ITriggerListener
    {
        Collider _collider;

        string _musicName;
        string _musicToReturn;

        public override void Initialize()
        {
            base.Initialize();

            //read properties
            if (TmxObject.Properties != null)
            {
                if (TmxObject.Properties.TryGetValue("MusicName", out var musicName))
                    _musicName = musicName;
            }

            //create collider based on obj type
            switch (TmxObject.ObjectType)
            {
                case TmxObjectType.Basic:
                case TmxObjectType.Tile:
                    _collider = Entity.AddComponent(new BoxCollider(TmxObject.Width, TmxObject.Height));
                    _collider.SetLocalOffset(new Vector2(TmxObject.Width / 2, TmxObject.Height / 2));
                    break;
                case TmxObjectType.Ellipse:
                    _collider = Entity.AddComponent(new CircleCollider(TmxObject.Width));
                    break;
                case TmxObjectType.Polygon:
                    _collider = Entity.AddComponent(new PolygonCollider(TmxObject.Points));
                    var width = TmxObject.Points.Select(p => p.X).Max() - TmxObject.Points.Select(p => p.X).Min();
                    var height = TmxObject.Points.Select(p => p.Y).Max() - TmxObject.Points.Select(p => p.Y).Min();
                    _collider.SetLocalOffset(new Vector2(width / 2, height / 2));
                    break;
            }

            _collider.IsTrigger = true;
            Flags.SetFlagExclusive(ref _collider.PhysicsLayer, PhysicsLayers.AreaTrigger);
            Flags.SetFlagExclusive(ref _collider.CollidesWithLayers, PhysicsLayers.PlayerCollider);
        }

        public void OnTriggerEnter(Collider other, Collider local)
        {
            _musicToReturn = Game1.AudioManager.FadeTo($@"Content\Audio\Music\{_musicName}.ogg", 1.5f, 1.5f);
        }

        public void OnTriggerExit(Collider other, Collider local)
        {
            if (_musicToReturn != null)
            {
                Game1.AudioManager.FadeTo(_musicToReturn, 1.5f, 1.5f);
                _musicToReturn = null;
            }
        }
    }
}
