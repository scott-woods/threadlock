﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.StaticData
{
    public static class PhysicsLayers
    {
        public const int None = 0;
        public const int Environment = 1;
        public const int PlayerHitbox = 2;
        public const int EnemyHurtbox = 3;
        public const int PlayerCollider = 4;
        public const int EnemyCollider = 5;
        public const int EnemyHitbox = 6;
        public const int PlayerHurtbox = 7;
        public const int Trigger = 8;
        public const int CombatArea = 9;
        public const int ProjectilePassableWall = 10;
        public const int Selectable = 11;
        public const int PromptTrigger = 12;
        public const int AreaTrigger = 13;
        public const int LuteNoteExplosion = 14;
        public const int Selector = 15;

        public static int GetLayerByName(string name)
        {
            Type type = typeof(PhysicsLayers);
            FieldInfo field = type.GetField(name, BindingFlags.Public | BindingFlags.Static);
            if (field != null)
                return (int)field.GetValue(null);

            throw new ArgumentException($"Physics Layer with name '{name}' does not exist.");
        }
    }
}
