using Microsoft.Xna.Framework;
using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.StaticData;

namespace Threadlock.Helpers
{
    public static class EntityHelper
    {
        public static float DistanceToEntity(Entity from, Entity to, bool useOrigin = true)
        {
            var fromPos = GetEntityPosition(from, useOrigin);
            var toPos = GetEntityPosition(to, useOrigin);

            return Vector2.Distance(fromPos, toPos);
        }

        public static Vector2 DirectionToEntity(Entity from, Entity to, bool shouldNormalize = true, bool useOrigin = true)
        {
            var fromPos = GetEntityPosition(from, useOrigin);
            var toPos = GetEntityPosition(to, useOrigin);

            var dir = toPos - fromPos;

            if (shouldNormalize)
            {
                var normalizedDir = dir;
                normalizedDir.Normalize();

                if (float.IsNaN(normalizedDir.X) || float.IsNaN(normalizedDir.Y))
                    return dir;
                else return normalizedDir;
            }

            return dir;
        }

        public static bool HasLineOfSight(Entity from, Entity to, bool useOrigin = true)
        {
            var fromPos = GetEntityPosition(from, useOrigin);
            var toPos = GetEntityPosition(to, useOrigin);

            var cast = Physics.Linecast(fromPos, toPos, 1 << PhysicsLayers.Environment);
            return cast.Collider == null;
        }

        public static TiledMapRenderer GetCurrentMap(Entity entity, bool useOrigin = true)
        {
            var pos = GetEntityPosition(entity, useOrigin);
            var renderer = entity.Scene.FindComponentsOfType<TiledMapRenderer>().FirstOrDefault(r => r.Bounds.Contains(pos));
            return renderer;
        }

        static Vector2 GetEntityPosition(Entity entity, bool useOrigin)
        {
            Vector2 pos = entity.Position;
            if (useOrigin)
                if (entity.TryGetComponent<OriginComponent>(out var origin))
                    pos = origin.Origin;

            return pos;
        }
    }
}
