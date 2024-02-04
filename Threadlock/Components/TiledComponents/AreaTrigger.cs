using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nez.Tiled;
using Threadlock.StaticData;

namespace Threadlock.Components.TiledComponents
{
    public abstract class AreaTrigger : TiledComponent, IUpdatable
    {
        //components
        Collider _collider;

        public override void Initialize()
        {
            base.Initialize();

            switch (TmxObject.ObjectType)
            {
                case TmxObjectType.Basic:
                case TmxObjectType.Tile:
                    _collider = Entity.AddComponent(new BoxCollider(TmxObject.Width, TmxObject.Height));
                    break;
                case TmxObjectType.Ellipse:
                    _collider = Entity.AddComponent(new CircleCollider(TmxObject.Width));
                    break;
                case TmxObjectType.Polygon:
                    _collider = Entity.AddComponent(new PolygonCollider(TmxObject.Points));
                    break;
            }

            _collider.IsTrigger = true;
            Flags.SetFlagExclusive(ref _collider.PhysicsLayer, PhysicsLayers.Trigger);
            Flags.SetFlagExclusive(ref _collider.CollidesWithLayers, PhysicsLayers.PlayerCollider);
        }

        public virtual void Update()
        {
            if (_collider.CollidesWithAny(out CollisionResult result))
            {
                OnTriggered();
            }
        }

        public abstract void OnTriggered();
    }
}
