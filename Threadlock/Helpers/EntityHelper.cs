using Microsoft.Xna.Framework;
using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;

namespace Threadlock.Helpers
{
    public static class EntityHelper
    {
        public static float DistanceToEntity(Entity from, Entity to, bool useOrigin = true)
        {
            Vector2 fromPos = from.Position;
            Vector2 toPos = to.Position;
            if (useOrigin)
            {
                if (from.TryGetComponent<OriginComponent>(out var fromOrigin))
                    fromPos = fromOrigin.Origin;
                if (to.TryGetComponent<OriginComponent>(out var toOrigin))
                    toPos = toOrigin.Origin;
            }

            return Vector2.Distance(fromPos, toPos);
        }

        public static Vector2 DirectionToEntity(Entity from, Entity to, bool shouldNormalize = true, bool useOrigin = true)
        {
            Vector2 fromPos = from.Position;
            Vector2 toPos = to.Position;
            if (useOrigin)
            {
                if (from.TryGetComponent<OriginComponent>(out var fromOrigin))
                    fromPos = fromOrigin.Origin;
                if (to.TryGetComponent<OriginComponent>(out var toOrigin))
                    toPos = toOrigin.Origin;
            }

            var dir = toPos - fromPos;
            if (shouldNormalize)
                dir.Normalize();

            return dir;
        }
    }
}
