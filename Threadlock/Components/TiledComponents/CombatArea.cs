using Microsoft.Xna.Framework;
using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.StaticData;

namespace Threadlock.Components.TiledComponents
{
    public class CombatArea : TiledComponent
    {
        PolygonCollider _collider;

        public override void Initialize()
        {
            base.Initialize();

            var points = new List<Vector2>(TmxObject.Points);
            _collider = Entity.AddComponent(new PolygonCollider(points.ToArray()));
            if (_collider.LocalOffset == Vector2.Zero)
            {
                _collider.SetLocalOffset(new Vector2(TmxObject.Width / 2, TmxObject.Height / 2));
            }
            _collider.IsTrigger = true;
            Flags.SetFlagExclusive(ref _collider.PhysicsLayer, PhysicsLayers.CombatArea);
        }

        public static bool IsPointInCombatArea(Vector2 position)
        {
            var combatAreaCollider = Physics.OverlapCircle(position, .5f, 1 << PhysicsLayers.CombatArea);
            if (combatAreaCollider == null) return false;
            else return true;
        }
    }
}
